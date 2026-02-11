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
            var incidentsWithStatusAndKpi = new List<(int AssetId, int KpiId, DateTime CreatedAt, int StatusId, string StatusName, string KpiName)>();

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
                    incidentsWithStatusAndKpi.Add((i.AssetId, i.KpiId, i.CreatedAt, i.StatusId, statusName, kpiName));
                }

                var kpiResults = await _context.KPIsResults
                    .Where(k => assetIds.Contains(k.AssetId))
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
                foreach (var g in kpiResults.GroupBy(k => (k.AssetId, k.KpiId)))
                    latestKpiResultByAssetKpi[g.Key] = g.First().Result ?? string.Empty;
            }

            var assetsOrdered = (ministry.Assets ?? new List<Asset>()).OrderBy(a => a.AssetName).ToList();
            var kpiIdsInIncidents = incidentsWithStatusAndKpi.Select(i => i.KpiId).Distinct().ToList();
            var kpisLovDict = new Dictionary<int, (string KpiName, string TargetDisplay)>();
            if (kpiIdsInIncidents.Count > 0)
            {
                var kpisLov = await _context.KpisLovs
                    .Where(k => kpiIdsInIncidents.Contains(k.Id) && k.DeletedAt == null)
                    .Select(k => new { k.Id, k.KpiName, k.TargetHigh, k.TargetMedium, k.TargetLow })
                    .ToListAsync();
                foreach (var k in kpisLov)
                {
                    var target = !string.IsNullOrWhiteSpace(k.TargetHigh) ? k.TargetHigh
                        : !string.IsNullOrWhiteSpace(k.TargetMedium) ? k.TargetMedium
                        : !string.IsNullOrWhiteSpace(k.TargetLow) ? k.TargetLow : string.Empty;
                    kpisLovDict[k.Id] = (k.KpiName ?? string.Empty, target);
                }
            }

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
                    var currentValue = latestKpiResultByAssetKpi.TryGetValue((asset.Id, inc.KpiId), out var r) ? r : string.Empty;
                    var target = kpisLovDict.TryGetValue(inc.KpiId, out var lov) ? lov.TargetDisplay : string.Empty;
                    var valueTargetDisplay = string.IsNullOrEmpty(currentValue) && string.IsNullOrEmpty(target) ? "-"
                        : string.IsNullOrEmpty(target) ? currentValue
                        : string.IsNullOrEmpty(currentValue) ? $"({target})"
                        : $"{currentValue} ({target})";
                    incidentDetails.Add(new MinistryReportIncidentRowDto
                    {
                        KpiName = inc.KpiName,
                        ValueTargetDisplay = valueTargetDisplay,
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
            var reportTime = dto.ReportGeneratedAt.ToString("dd-MM-yyyy hh:mm tt");
            var cardBg = "#e8eaf6";
            var headerBg = "#e3f2fd";
            var borderColor = "#90caf9";

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#212121"));

                // Header: Ministry name (one line, smaller font, dark blue) | Report generated at (right, gray)
                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem(3).AlignLeft().Text(dto.MinistryName).SemiBold().FontSize(11).FontColor("#1a237e");
                        row.RelativeItem().AlignRight().Text($"Report Generated at {reportTime}").FontSize(9).FontColor("#757575");
                    });

                page.Content()
                    .PaddingVertical(8)
                    .Column(column =>
                    {
                        column.Spacing(12);

                        // Summary: 4 KPI cards in a row (light gray rounded-card style)
                        column.Item().Row(cardsRow =>
                        {
                            cardsRow.Spacing(12);
                            AddSummaryCard(cardsRow, dto.AssetsMonitored.ToString(), "Assets Monitored", cardBg);
                            AddSummaryCard(cardsRow, dto.TotalIncidents.ToString(), "Total Incidents", cardBg);
                            AddSummaryCard(cardsRow, dto.ActiveIncidents.ToString(), "Active Incidents", cardBg);
                            AddSummaryCard(cardsRow, dto.ResolutionPerformance.ToString("F1") + "%", "Resolution Performance", cardBg);
                        });

                        // Per-asset blocks (dashboard card style: white block, gray header row, then incident table)
                        foreach (var asset in dto.Assets)
                        {
                            column.Item()
                                .Border(0.5f).BorderColor(borderColor)
                                .Background(Colors.White)
                                .Padding(10)
                                .Column(assetBlock =>
                                {
                                    assetBlock.Spacing(0);

                                    // Asset summary header row (uppercase, gray background)
                                    assetBlock.Item().Table(headerT =>
                                    {
                                        headerT.ColumnsDefinition(c => { c.RelativeColumn(3); c.ConstantColumn(100); c.ConstantColumn(90); });
                                        headerT.Cell().Background(headerBg).Padding(8).Text("ASSET NAME").SemiBold().FontSize(9).FontColor("#1565c0");
                                        headerT.Cell().Background(headerBg).Padding(8).AlignCenter().Text("COMPLIANCE SCORE").SemiBold().FontSize(9).FontColor("#1565c0");
                                        headerT.Cell().Background(headerBg).Padding(8).AlignRight().Text("ACTIVE INCIDENTS").SemiBold().FontSize(9).FontColor("#1565c0");
                                    });
                                    // Asset data row
                                    assetBlock.Item().Table(dataT =>
                                    {
                                        dataT.ColumnsDefinition(c => { c.RelativeColumn(3); c.ConstantColumn(100); c.ConstantColumn(90); });
                                        dataT.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(10).Text(asset.AssetName).SemiBold().FontSize(11);
                                        dataT.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(10).AlignCenter().Text(asset.ComplianceScore.ToString("F1")).SemiBold().FontSize(11);
                                        dataT.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(10).AlignRight().Text(asset.OpenIncidents.ToString()).SemiBold().FontSize(11);
                                    });

                                    // Incident details table (uppercase headers, status as colored pill)
                                    if (asset.IncidentDetails.Count > 0)
                                    {
                                        assetBlock.Item().PaddingTop(10).Table(incidentTable =>
                                        {
                                            // STATUS column fixed width so pill box is uniform (snug like "Open")
                                            incidentTable.ColumnsDefinition(cols =>
                                            {
                                                cols.RelativeColumn(2);
                                                cols.RelativeColumn(1);
                                                cols.ConstantColumn(95);
                                                cols.ConstantColumn(52);
                                            });
                                            incidentTable.Cell().Background(headerBg).Padding(6).Text("KPI").SemiBold().FontSize(8).FontColor("#1565c0");
                                            incidentTable.Cell().Background(headerBg).Padding(6).AlignCenter().Text("VALUE (TARGET)").SemiBold().FontSize(8).FontColor("#1565c0");
                                            incidentTable.Cell().Background(headerBg).Padding(6).AlignCenter().Text("CREATED AT").SemiBold().FontSize(8).FontColor("#1565c0");
                                            incidentTable.Cell().Background(headerBg).Padding(6).AlignCenter().Text("STATUS").SemiBold().FontSize(8).FontColor("#1565c0");
                                            foreach (var row in asset.IncidentDetails)
                                            {
                                                var (bg, fg) = GetStatusPillColors(row.StatusName);
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).Text(row.KpiName);
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text(row.ValueTargetDisplay);
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text(row.IncidentCreatedAt.ToString("dd/MM/yyyy hh:mm tt"));
                                                incidentTable.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).AlignCenter().Background(bg).PaddingVertical(3).PaddingHorizontal(8).Text(row.StatusName).FontSize(8).SemiBold();
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

        private static void AddSummaryCard(RowDescriptor row, string value, string label, string bg)
        {
            row.RelativeItem().Element(e => e
                .Background(bg)
                .Border(0.5f).BorderColor("#90caf9")
                .Padding(14)
                .Column(c =>
                {
                    c.Item().AlignCenter().Text(value).Bold().FontSize(20).FontColor("#1565c0");
                    c.Item().AlignCenter().PaddingTop(4).Text(label).FontSize(10).FontColor("#37474f");
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
