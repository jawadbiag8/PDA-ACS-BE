using DAMS.Application.DTOs;
using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DAMS.Infrastructure.Services
{
    public class MinistryReportService : IMinistryReportService
    {
        private static bool _licenseSet;
        private readonly ApplicationDbContext _context;

        public MinistryReportService(ApplicationDbContext context)
        {
            _context = context;
            if (!_licenseSet)
            {
                QuestPDF.Settings.License = LicenseType.Community;
                _licenseSet = true;
            }
        }

        public async Task<MinistryReportPdfResult> GenerateReportPdfAsync(int ministryId)
        {
            var ministry = await _context.Ministries
                .Include(m => m.Assets.Where(a => a.DeletedAt == null))
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == ministryId && m.DeletedAt == null);

            if (ministry == null)
            {
                return new MinistryReportPdfResult
                {
                    Success = false,
                    Message = "Ministry not found.",
                    PdfBytes = null,
                    FileName = null
                };
            }

            var assetIds = ministry.Assets?.Select(a => a.Id).ToList() ?? new List<int>();
            var latestMetricsByAsset = new Dictionary<int, double>();
            int totalIncidents = 0, activeIncidents = 0, resolvedCount = 0;
            var latestKpiResultByAssetKpi = new Dictionary<(int AssetId, int KpiId), string>();
            var incidentsWithStatusAndKpi = new List<(int AssetId, int KpiId, DateTime CreatedAt, int StatusId, string StatusName, string KpiName, string KpiGroup)>();

            if (assetIds.Count > 0)
            {
                var metrics = await _context.AssetMetrics
                    .Where(m => assetIds.Contains(m.AssetId))
                    .OrderByDescending(m => m.CalculatedAt)
                    .ToListAsync();
                foreach (var g in metrics.GroupBy(m => m.AssetId))
                    latestMetricsByAsset[g.Key] = g.First().OverallComplianceMetric;

                var incidents = await _context.Incidents
                    .Where(i => assetIds.Contains(i.AssetId) && i.DeletedAt == null)
                    .Include(i => i.Status)
                    .Include(i => i.KpisLov)
                    .ToListAsync();
                totalIncidents = incidents.Count;
                resolvedCount = incidents.Count(i => i.StatusId == 12);
                activeIncidents = totalIncidents - resolvedCount;
                foreach (var i in incidents)
                {
                    var statusName = i.Status?.Name ?? string.Empty;
                    var kpiName = i.KpisLov?.KpiName ?? string.Empty;
                    var kpiGroup = i.KpisLov?.KpiGroup ?? string.Empty;
                    incidentsWithStatusAndKpi.Add((i.AssetId, i.KpiId, i.CreatedAt, i.StatusId, statusName, kpiName, kpiGroup));
                }

                var kpiResults = await _context.KPIsResults
                    .Where(k => assetIds.Contains(k.AssetId))
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
                foreach (var g in kpiResults.GroupBy(k => (k.AssetId, k.KpiId)))
                    latestKpiResultByAssetKpi[g.Key] = g.First().Result ?? string.Empty;
            }

            var assetsOrdered = (ministry.Assets ?? new List<Asset>()).OrderBy(a => a.AssetName).ToList();

            var resolutionPerformance = totalIncidents > 0 ? Math.Round((double)resolvedCount / totalIncidents * 100, 1) : 0;

            var assetDtos = new List<MinistryReportAssetRowDto>();
            foreach (var asset in assetsOrdered)
            {
                var openIncidentsForAsset = incidentsWithStatusAndKpi
                    .Where(i => i.AssetId == asset.Id && i.StatusId != 12)
                    .OrderBy(i => i.KpiId)
                    .ThenBy(i => i.CreatedAt)
                    .ToList();
                var incidentDetails = new List<MinistryReportIncidentRowDto>();
                foreach (var inc in openIncidentsForAsset)
                {
                    var historyResult = latestKpiResultByAssetKpi.TryGetValue((asset.Id, inc.KpiId), out var r) ? r : null;
                    // Open incidents = failure; use same display logic as incident creation reasoning (value only, no target)
                    var valueDisplay = KpiValueDisplayHelper.GetCurrentValueDisplay(inc.KpiId, isFailure: true, historyResult);
                    if (string.IsNullOrWhiteSpace(valueDisplay)) valueDisplay = historyResult ?? "-";
                    incidentDetails.Add(new MinistryReportIncidentRowDto
                    {
                        KpiName = inc.KpiName,
                        KpiGroup = inc.KpiGroup,
                        ValueTargetDisplay = valueDisplay,
                        IncidentCreatedAt = inc.CreatedAt,
                        StatusName = inc.StatusName
                    });
                }

                assetDtos.Add(new MinistryReportAssetRowDto
                {
                    AssetName = asset.AssetName,
                    ComplianceScore = latestMetricsByAsset.TryGetValue(asset.Id, out var score) ? score : 0,
                    OpenIncidents = openIncidentsForAsset.Count,
                    IncidentDetails = incidentDetails
                });
            }

            var dto = new MinistryReportDto
            {
                MinistryName = ministry.MinistryName,
                ReportGeneratedAt = DateTime.Now,
                AssetsMonitored = assetsOrdered.Count,
                TotalIncidents = totalIncidents,
                ActiveIncidents = activeIncidents,
                ResolutionPerformance = resolutionPerformance,
                Assets = assetDtos
            };

            byte[] pdfBytes;
            try
            {
                using (var stream = new MemoryStream())
                {
                    Document.Create(container => BuildReport(container, dto)).GeneratePdf(stream);
                    pdfBytes = stream.ToArray();
                }
                // Run finalizers so any QuestPDF cleanup happens before scope/request teardown
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                return new MinistryReportPdfResult
                {
                    Success = false,
                    Message = "Failed to generate PDF: " + ex.Message,
                    PdfBytes = null,
                    FileName = null
                };
            }

            var fileName = $"Ministry-Report-{SanitizeFileName(dto.MinistryName)}.pdf";
            return new MinistryReportPdfResult
            {
                Success = true,
                PdfBytes = pdfBytes,
                FileName = fileName,
                Message = null
            };
        }

        private static void BuildReport(IDocumentContainer container, MinistryReportDto dto)
        {
            var reportTime = dto.ReportGeneratedAt.ToString("dd/MM/yyyy hh:mm tt");
            var cardBg = "#F8F8F8";
            var headerBg = "#F0F0F0";
            var borderColor = "#E0E0E0";
            var textDark = "#212121";

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#212121"));

                // Header: Ministry name (dark grey) | Report generated at (right, light grey)
                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem(3).AlignLeft().Text(dto.MinistryName).SemiBold().FontSize(11).FontColor(textDark);
                        row.RelativeItem().AlignRight().Text($"Report Generated at {reportTime}").FontSize(9).FontColor("#757575");
                    });

                page.Content()
                    .PaddingVertical(8)
                    .Column(column =>
                    {
                        column.Spacing(12);

                        // Summary: 3 cards in a row (rounded corners, design-aligned)
                        column.Item().Row(cardsRow =>
                        {
                            cardsRow.Spacing(12);
                            AddSummaryCard(cardsRow, dto.AssetsMonitored.ToString(), "Assets Monitored", cardBg, borderColor, textDark);
                            AddSummaryCard(cardsRow, dto.TotalIncidents.ToString(), "Total Incidents", cardBg, borderColor, textDark);
                            AddSummaryCard(cardsRow, dto.ActiveIncidents.ToString(), "Active Incidents", cardBg, borderColor, textDark);
                        });

                        // Per-asset blocks (light gray background, rounded corners, subtle gray borders)
                        foreach (var asset in dto.Assets)
                        {
                            column.Item()
                                .Border(0.5f).BorderColor(borderColor)
                                .CornerRadius(6)
                                .Background(cardBg)
                                .Padding(10)
                                .Column(assetBlock =>
                                {
                                    assetBlock.Spacing(0);

                                    // Asset summary header row (darker gray background, dark grey text, full grid + left/right borders)
                                    assetBlock.Item().Table(headerT =>
                                    {
                                        headerT.ColumnsDefinition(c => { c.RelativeColumn(3); c.ConstantColumn(120); c.ConstantColumn(115); });
                                        headerT.Cell().Background(headerBg).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).BorderColor(borderColor).Padding(8).Text("ASSET NAME").SemiBold().FontSize(9).FontColor(textDark);
                                        headerT.Cell().Background(headerBg).BorderBottom(0.5f).BorderRight(0.5f).BorderColor(borderColor).Padding(8).AlignCenter().Text("COMPLIANCE SCORE").SemiBold().FontSize(9).FontColor(textDark);
                                        headerT.Cell().Background(headerBg).BorderBottom(0.5f).BorderRight(0.5f).BorderColor(borderColor).Padding(8).AlignCenter().Text("ACTIVE INCIDENTS").SemiBold().FontSize(9).FontColor(textDark);
                                    });
                                    // Asset data row (horizontal + vertical internal borders + left/right)
                                    assetBlock.Item().Table(dataT =>
                                    {
                                        dataT.ColumnsDefinition(c => { c.RelativeColumn(3); c.ConstantColumn(120); c.ConstantColumn(115); });
                                        dataT.Cell().BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).BorderColor(borderColor).Padding(10).Text(asset.AssetName).SemiBold().FontSize(11).FontColor(textDark);
                                        dataT.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(borderColor).Padding(10).AlignCenter().Text(asset.ComplianceScore.ToString("F1")).SemiBold().FontSize(11).FontColor(textDark);
                                        dataT.Cell().BorderBottom(0.5f).BorderRight(0.5f).BorderColor(borderColor).Padding(10).AlignCenter().Text(asset.OpenIncidents.ToString()).SemiBold().FontSize(11).FontColor(textDark);
                                    });

                                    // Second block: KPI details table with its own border (same style as first block)
                                    if (asset.IncidentDetails.Count > 0)
                                    {
                                        assetBlock.Item().PaddingTop(10)
                                            .Border(0.5f).BorderColor(borderColor).CornerRadius(4)
                                            .Table(incidentTable =>
                                        {
                                            incidentTable.ColumnsDefinition(cols =>
                                            {
                                                cols.RelativeColumn(1);   // KPI (narrower)
                                                cols.RelativeColumn(1);
                                                cols.ConstantColumn(110); // CREATED AT - wide enough for "dd/MM/yyyy hh:mm tt" on one line
                                                cols.ConstantColumn(72);  // STATUS - wide enough for "Monitoring" on one line
                                            });
                                            var headerGray = "#616161";
                                            incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).Text("KPI").SemiBold().FontSize(8).FontColor(headerGray);
                                            incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text("VALUE").SemiBold().FontSize(8).FontColor(headerGray);
                                            incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text("CREATED AT").SemiBold().FontSize(8).FontColor(headerGray);
                                            incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text("STATUS").SemiBold().FontSize(8).FontColor(headerGray);
                                            foreach (var row in asset.IncidentDetails)
                                            {
                                                var createdAtOneLine = row.IncidentCreatedAt.ToString("dd/MM/yyyy hh:mm tt");
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6)
                                                    .Column(kpiCol =>
                                                    {
                                                        kpiCol.Item().Text(row.KpiName).Bold().FontSize(9).FontColor(textDark);
                                                        if (!string.IsNullOrWhiteSpace(row.KpiGroup))
                                                            kpiCol.Item().PaddingTop(2).Text(row.KpiGroup).FontSize(8).FontColor("#757575");
                                                    });
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text(row.ValueTargetDisplay).FontSize(9).FontColor(textDark);
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text(createdAtOneLine).FontSize(9).FontColor(textDark);
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text(row.StatusName).FontSize(9).FontColor(textDark);
                                            }
                                        });
                                    }
                                });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(9).FontColor("#757575"))
                    .Text(x => { x.Span("Page "); x.CurrentPageNumber(); });
            });
        }

        private static void AddSummaryCard(RowDescriptor row, string value, string label, string bg, string borderCol, string textCol)
        {
            row.RelativeItem().Element(e => e
                .Background(bg)
                .Border(0.5f).BorderColor(borderCol)
                .CornerRadius(8)
                .Padding(14)
                .Column(c =>
                {
                    c.Item().AlignLeft().PaddingLeft(4).Text(value).Bold().FontSize(20).FontColor(textCol);
                    c.Item().AlignLeft().PaddingLeft(4).PaddingTop(4).Text(label).FontSize(10).FontColor("#37474f");
                }));
        }

        private static (string Background, string Text) GetStatusPillColors(string statusName)
        {
            var s = (statusName ?? string.Empty).Trim().ToUpperInvariant();
            // Use hex for reliable color in PDF (RESOLVED=green, OPEN=gray, etc.)
            if (s.Contains("RESOLVED")) return ("#d4edda", "#155724");
            if (s.Contains("OPEN")) return ("#6c757d", "#ffffff");
            if (s.Contains("MONITORING")) return ("#fd7e14", "#ffffff");
            if (s.Contains("INVESTIGAT") || s.Contains("INVISTAGIT")) return ("#f8d7da", "#721c24");
            if (s.Contains("FIXING")) return ("#fff3cd", "#856404");
            return ("#e2e3e5", "#383d41");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
            return string.IsNullOrEmpty(sanitized) ? "Report" : sanitized;
        }
    }
}
