using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DAMS.Infrastructure.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Seed Roles
            await SeedRolesAsync(roleManager);

            // Seed Users
            await SeedUsersAsync(userManager);

            // Seed Ministry
            await SeedMinistryAsync(context);

            // Seed Department
            //await SeedDepartmentsAsync(context);

            //// Seed CommonLookup
            await SeedCommonLookupAsync(context);

            //// Seed KpisLov
            await SeedKpisLovAsync(context);

            //// Seed MetricWeights
            await SeedMetricWeightsAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            string[] roles = { "PDA Analyst", "PMO Executive" };

            foreach (var roleName in roles)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName,
                        Description = $"{roleName} role"
                    });
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Seed PDA Analyst User
            var pdaUser = await userManager.FindByNameAsync("pda");
            if (pdaUser == null)
            {
                pdaUser = new ApplicationUser
                {
                    UserName = "pda",
                    Email = "pda@dams.com",
                    FirstName = "PDA",
                    LastName = "USER",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(pdaUser, "Pda@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pdaUser, "PDA Analyst");
                }
            }

            // Seed PMO Executive User
            var pmoUser = await userManager.FindByNameAsync("pmo");
            if (pmoUser == null)
            {
                pmoUser = new ApplicationUser
                {
                    UserName = "pmo",
                    Email = "pmo@dams.com",
                    FirstName = "PMO",
                    LastName = "USER",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(pmoUser, "Pmo@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pmoUser, "PMO Executive");
                }
            }
        }

        private static async Task SeedMinistryAsync(ApplicationDbContext context)
        {
            if (await context.Ministries.AnyAsync())
            {
                return; // Already seeded
            }

            // Get unique ministry names (case-insensitive, normalized)
            var ministryNames = new List<string>
            {
                "Ministry of Water Resources",
                "Ministry of Climate Change and Environmental Coordination",
                "Ministry of Commerce",
                "Ministry of Defence",
                "Ministry of Economic affairs",
                "Ministry Of Energy",
                "Ministry of Defence Production",
                "Ministry of Federal Education and Professional Training",
                "Ministry of Foreign Affairs",
                "Ministry of Housing & Works",
                "Ministry of Human Rights",
                "Ministry of Industries & Production",
                "Ministry of Information & Telecommuncation",
                "Ministry of Information and Broadcasting",
                "Ministry of Interior and narcotics Control",
                "Ministry of Kashmir Affairs , Gilgit-Baltistan and states and Frontier regions",
                "Ministry of Law & Justice",
                "Ministry of Maritime Affairs",
                "Ministry of National Food Security & Research",
                "Ministry of National Health Services Regulations and Coordination",
                "Ministry of Overseas Pakistanis and Human Resources Development",
                "Ministry of Parliamentary Affairs",
                "Ministry of Planning, Development & Special Initiatives",
                "Ministry of Poverty Alleviation and social safety",
                "Ministry of Privatisation",
                "Ministry of Railways",
                "Ministry of Religious Affairs and Interfaith Harmony",
                "Ministry of Science and Technology",
                "Demo Ministry"
            };

            // Remove duplicates (case-insensitive)
            var uniqueMinistries = ministryNames
                .Select(name => name.Trim())
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var ministries = uniqueMinistries.Select(name => new Ministry
            {
                MinistryName = name,
                ContactName = string.Empty,
                ContactEmail = string.Empty,
                ContactPhone = string.Empty,
                CreatedAt = DateTime.Now,
                CreatedBy = "System"
            }).ToList();

            await context.Ministries.AddRangeAsync(ministries);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCommonLookupAsync(ApplicationDbContext context)
        {
            if (await context.CommonLookups.AnyAsync())
            {
                return; // Already seeded
            }

            var commonLookups = new List<CommonLookup>
            {
                new CommonLookup
                {
                    Name = "LOW - Supporting Services",
                    Type = "CitizenImpactLevel",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "MEDIUM - Important Services",
                    Type = "CitizenImpactLevel",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "HIGH - Critical Public Services",
                    Type = "CitizenImpactLevel",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },

                new CommonLookup
                {
                    Name = "P1",
                    Type = "SeverityLevel",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "P2",
                    Type = "SeverityLevel",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "P3",
                    Type = "SeverityLevel",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "P4",
                    Type = "SeverityLevel",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },

                new CommonLookup
                {
                    Name = "Open",
                    Type = "Status",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "Investigating",
                    Type = "Status",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "Fixing",
                    Type = "Status",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "Monitoring",
                    Type = "Status",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "Resolved",
                    Type = "Status",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                },
                new CommonLookup
                {
                    Name = "3",
                    Type = "IncidentCreationFrequency",
                    CreatedAt = DateTime.Now,
                    CreatedBy = "NA"
                }
            };

            await context.CommonLookups.AddRangeAsync(commonLookups);
            await context.SaveChangesAsync();
        }

        private static async Task SeedKpisLovAsync(ApplicationDbContext context)
        {
            if (await context.KpisLovs.AnyAsync())
            {
                return; // Already seeded
            }

            // Get all SeverityLevels for KPIs
            var severityP1 = await context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Type == "SeverityLevel" && cl.Name == "P1" && cl.DeletedAt == null);
            var severityP2 = await context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Type == "SeverityLevel" && cl.Name == "P2" && cl.DeletedAt == null);
            var severityP3 = await context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Type == "SeverityLevel" && cl.Name == "P3" && cl.DeletedAt == null);
            var severityP4 = await context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Type == "SeverityLevel" && cl.Name == "P4" && cl.DeletedAt == null);

            if (severityP1 == null || severityP2 == null || severityP3 == null || severityP4 == null)
            {
                throw new InvalidOperationException("SeverityLevels (P1, P2, P3, P4) not found. Please seed CommonLookup first.");
            }

            var kpis = new List<KpisLov>
            {
                // Availability & Reliability KPIs
                new KpisLov
                {
                    KpiName = "Website completely down (no response)",
                    KpiGroup = "Availability & Reliability",
                    Manual = "Auto",
                    Frequency = "1 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "Uptime",
                    TargetHigh = "99.90%",
                    TargetMedium = "99.50%",
                    TargetLow = "99.00%",
                    KpiType = "http",
                    SeverityId = severityP1.Id,
                    Weight = 5,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "DNS resolution failure",
                    KpiGroup = "Availability & Reliability",
                    Manual = "Auto",
                    Frequency = "5 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "DNS resolved",
                    TargetHigh = "99.90%",
                    TargetMedium = "99.50%",
                    TargetLow = "99.00%",
                    KpiType = "dns",
                    SeverityId = severityP1.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Hosting/network outage",
                    KpiGroup = "Availability & Reliability",
                    Manual = "Auto",
                    Frequency = "5 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "3",
                    TargetLow = "5",
                    KpiType = "http",
                    SeverityId = severityP1.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Partial outage (homepage loads, inner pages fail)",
                    KpiGroup = "Availability & Reliability",
                    Manual = "Auto",
                    Frequency = "5 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "3",
                    TargetLow = "5",
                    KpiType = "browser",
                    SeverityId = severityP2.Id,
                    Weight = 3,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Intermittent availability (flapping)",
                    KpiGroup = "Availability & Reliability",
                    Manual = "Auto",
                    Frequency = "5 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "3",
                    TargetLow = "5",
                    KpiType = "http",
                    SeverityId = severityP2.Id,
                    Weight = 2,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                // Performance & Efficiency KPIs
                new KpisLov
                {
                    KpiName = "Slow page load",
                    KpiGroup = "Performance & Efficiency",
                    Manual = "Auto",
                    Frequency = "15 min",
                    Outcome = "Sec",
                    PagesToCheck = string.Empty,
                    TargetType = "Average",
                    TargetHigh = "3",
                    TargetMedium = "5",
                    TargetLow = "10",
                    KpiType = "browser",
                    SeverityId = severityP2.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Backend response time",
                    KpiGroup = "Performance & Efficiency",
                    Manual = "Auto",
                    Frequency = "15 min",
                    Outcome = "Sec",
                    PagesToCheck = string.Empty,
                    TargetType = "Average",
                    TargetHigh = "0.5",
                    TargetMedium = "1",
                    TargetLow = "2",
                    KpiType = "http",
                    SeverityId = severityP2.Id,
                    Weight = 3,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Heavy pages consuming excessive data",
                    KpiGroup = "Performance & Efficiency",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "MB",
                    PagesToCheck = string.Empty,
                    TargetType = "Average",
                    TargetHigh = "2",
                    TargetMedium = "3",
                    TargetLow = "5",
                    KpiType = "browser",
                    SeverityId = severityP2.Id,
                    Weight = 2,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                // Security, Trust & Privacy KPIs
                new KpisLov
                {
                    KpiName = "Website not using HTTPS",
                    KpiGroup = "Security, Trust & Privacy",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "1",
                    TargetLow = "1",
                    KpiType = "http",
                    SeverityId = severityP2.Id,
                    Weight = 5,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "SSL certificate expired",
                    KpiGroup = "Security, Trust & Privacy",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "0",
                    TargetLow = "0",
                    KpiType = "ssl",
                    SeverityId = severityP2.Id,
                    Weight = 5,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Browser security warning",
                    KpiGroup = "Security, Trust & Privacy",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "0",
                    TargetLow = "0",
                    KpiType = "browser",
                    SeverityId = severityP3.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Mixed content warnings",
                    KpiGroup = "Security, Trust & Privacy",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "0",
                    TargetLow = "0",
                    KpiType = "browser",
                    SeverityId = severityP3.Id,
                    Weight = 3,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Suspicious redirects",
                    KpiGroup = "Security, Trust & Privacy",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "0",
                    TargetLow = "0",
                    KpiType = "browser",
                    SeverityId = severityP3.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Privacy policy availability",
                    KpiGroup = "Security, Trust & Privacy",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "0",
                    TargetLow = "0",
                    KpiType = "browser",
                    SeverityId = severityP3.Id,
                    Weight = 2,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                // Accessibility & Inclusivity KPIs
                new KpisLov
                {
                    KpiName = "WCAG compliance score",
                    KpiGroup = "Accessibility & Inclusivity",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "%",
                    PagesToCheck = string.Empty,
                    TargetType = "Percentage",
                    TargetHigh = "95%",
                    TargetMedium = "90%",
                    TargetLow = "80%",
                    KpiType = "accessibility",
                    SeverityId = severityP4.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Missing form label",
                    KpiGroup = "Accessibility & Inclusivity",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "%",
                    PagesToCheck = string.Empty,
                    TargetType = "Percentage",
                    TargetHigh = "1%",
                    TargetMedium = "3%",
                    TargetLow = "5%",
                    KpiType = "accessibility",
                    SeverityId = severityP4.Id,
                    Weight = 3,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Images missing alt text",
                    KpiGroup = "Accessibility & Inclusivity",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "%",
                    PagesToCheck = string.Empty,
                    TargetType = "Percentage",
                    TargetHigh = "2%",
                    TargetMedium = "5%",
                    TargetLow = "10%",
                    KpiType = "accessibility",
                    SeverityId = severityP4.Id,
                    Weight = 2,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Poor color contrast",
                    KpiGroup = "Accessibility & Inclusivity",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "%",
                    PagesToCheck = string.Empty,
                    TargetType = "Percentage",
                    TargetHigh = "1%",
                    TargetMedium = "3%",
                    TargetLow = "5%",
                    KpiType = "accessibility",
                    SeverityId = severityP4.Id,
                    Weight = 2,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                // User Experience & Journey Quality KPIs
                new KpisLov
                {
                    KpiName = "Download success rate",
                    KpiGroup = "User Experience & Journey Quality",
                    Manual = "Auto",
                    Frequency = "15 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "3",
                    TargetLow = "5",
                    KpiType = "browser",
                    SeverityId = severityP3.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Download links broken",
                    KpiGroup = "User Experience & Journey Quality",
                    Manual = "Auto",
                    Frequency = "15 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "3",
                    TargetLow = "5",
                    KpiType = "browser",
                    SeverityId = severityP3.Id,
                    Weight = 3,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Page loads but assets don't (broken CSS/JS)",
                    KpiGroup = "User Experience & Journey Quality",
                    Manual = "Auto",
                    Frequency = "15 min",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "3",
                    TargetLow = "5",
                    KpiType = "browser",
                    SeverityId = severityP3.Id,
                    Weight = 3,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                // Navigation & Discoverability KPIs
                new KpisLov
                {
                    KpiName = "Search not available",
                    KpiGroup = "Navigation & Discoverability",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "1",
                    TargetLow = "3",
                    KpiType = "browser",
                    SeverityId = severityP4.Id,
                    Weight = 4,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Broken internal links",
                    KpiGroup = "Navigation & Discoverability",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "%",
                    PagesToCheck = string.Empty,
                    TargetType = "Percentage",
                    TargetHigh = "0%",
                    TargetMedium = "5%",
                    TargetLow = "10%",
                    KpiType = "browser",
                    SeverityId = severityP4.Id,
                    Weight = 3,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Circular navigation",
                    KpiGroup = "Navigation & Discoverability",
                    Manual = "Auto",
                    Frequency = "Daily",
                    Outcome = "Flag",
                    PagesToCheck = string.Empty,
                    TargetType = "# of incidents",
                    TargetHigh = "0",
                    TargetMedium = "1",
                    TargetLow = "3",
                    KpiType = "browser",
                    SeverityId = severityP4.Id,
                    Weight = 2,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                // Traffic, Usage KPIs (Manual - No target calculations)
                new KpisLov
                {
                    KpiName = "Total visits",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default, but not used for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Unique visitors",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Page views",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Top accessed pages",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Entry page distribution",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Exit page distribution",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Average session duration",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Bounce rate",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new KpisLov
                {
                    KpiName = "Peak usage windows",
                    KpiGroup = "Traffic, Usage",
                    Manual = "Manual",
                    Frequency = string.Empty,
                    Outcome = string.Empty,
                    PagesToCheck = string.Empty,
                    TargetType = string.Empty,
                    TargetHigh = string.Empty,
                    TargetMedium = string.Empty,
                    TargetLow = string.Empty,
                    KpiType = string.Empty,
                    SeverityId = severityP1.Id, // Default for manual KPIs
                    Weight = 0,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                }
            };

            await context.KpisLovs.AddRangeAsync(kpis);
            await context.SaveChangesAsync();
        }

        private static async Task SeedDepartmentsAsync(ApplicationDbContext context)
        {
            if (await context.Departments.AnyAsync())
            {
                return; // Already seeded
            }

            // Get or create "Undefined Ministry" to link departments to
            var undefinedMinistry = await context.Ministries
                .FirstOrDefaultAsync(m => m.MinistryName == "Undefined Ministry" && m.DeletedAt == null);

            if (undefinedMinistry == null)
            {
                undefinedMinistry = new Ministry
                {
                    MinistryName = "Undefined Ministry",
                    ContactName = string.Empty,
                    ContactEmail = string.Empty,
                    ContactPhone = string.Empty,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                };
                context.Ministries.Add(undefinedMinistry);
                await context.SaveChangesAsync();
            }

            // Get unique department names (case-insensitive, normalized)
            var departmentNames = new List<string>
            {
                "Pakistan Atomic Energy Commission",
                "National Fertilizer Development Center",
                "Federal Government Employees Housing Authority",
                "Heavy Mechanical Complex",
                "National Highway & Motorway Police",
                "Council For Works and Housing Research",
                "navalanchorage housing socity",
                "National Telecomm Corporation",
                "Federal Employees Benevolent & Group Insurance Funds",
                "Ministry of Information And Broadcasting",
                "National institute of eclectronis",
                "National Institute of Electronics",
                "Pakistan Academy for Rural Development",
                "National School of Public Policy",
                "Pakistan Museum of Natural History",
                "Forum of Pakistan Ombudsman",
                "National Industrial relations Commission",
                "Pakistan Institute of Medical Sciences",
                "Directorate General Defence Purchase",
                "Federal Directorate of immunization",
                "National Accountability Beurue",
                "Nayapakistan Hosuing Development Authority",
                "National inistiute of public Administration",
                "National Transport resarch center",
                "Pakistan seurity printing corporation",
                "Pakistan International Maritime Expo&Confernce",
                "Islamabad Museum",
                "National Languge Programme",
                "Airport Security Force",
                "Federal tax obudsman",
                "Defence Export Promotion Organization",
                "INMOL Atomic Energy Cancer Hospital",
                "AEMC Atomic Energy Cancer Hospital",
                "BINO Atomic Energy Cancer Hospital",
                "Centre for Nuclear Medicine & Radiotherapy Quetta",
                "Centre for Nuclear Medicine (Atomic Energy Cancer Hospital)",
                "Dera Ismail Khan Atomic Energy Cancer Hospital",
                "GINO Atomic Energy Cancer Hospital",
                "Institute of Nuclear Medicine, Oncology & Radiotherapy Abbotabad",
                "Institute of Radiotherapy and Nuclear Medicine",
                "MINAR Atomic Energy Cancer Hospital",
                "National Metrology Institute of Pakistan",
                "NORI Atomic Energy Cancer Hospital",
                "NORIN Atomic Energy Cancer Hospital",
                "Swat Institute of Nuclear Medicine, Oncology & Radiotherapy",
                "Karachi Institute of Radiotherapy & Nuclear Medicine",
                "PAEC General Atomic Energy Cancer Hospital",
                "Pakistan Air War College Institute",
                "Critical Infrastructure Protection Malware&Malware Analysis",
                "Climate Resilent Urban HumanSettlemnt Unit",
                "Exim Bank",
                "Fishery Development Board",
                "Islamabad police Islamabad",
                "National Defence University",
                "Insttitue of Vocacational Trainings",
                "National Languge Programme Department",
                "Pakistan Telecomm Authority",
                "Pakistan Post",
                "Directorate General of Procurment Army",
                "Heavy Industries Taxila",
                "National Center for Non-Destructive Testing",
                "National Documentation Wing, Cabinet Division",
                "Pakistan Aeronatical complex",
                "Pakistan Baitulmal",
                "Pakistan Council of Research in Water Resources",
                "Pakistan Council of Scientific and Inderstial Resach Karachi",
                "Pakistan Welding Institute",
                "Tourism Dept",
                "Zoological Survey of Pakistan",
                "Zari Taraqiati Bank",
                "National Aerospace Science &Technalogy Park",
                "Agricuture Linkage Programme",
                "Board of Investment Pakistan",
                "Pakistan Maritime Security Agency",
                "Director General Passport &Immigration",
                "Electronic Certification Accreditation Council",
                "ENAR Petorluem Refining Facility",
                "Insttitue of Industrial Electronics Engineering",
                "Karachi Intitute of Power engineering(PAEC)",
                "National Academy for Prison Administration",
                "National Skills University",
                "Pakistan Media Regularatory Authority",
                "Pakistan Institute of Fashion and Design",
                "Common Criteria Pakistan Lab (CCPL)",
                "Federal government polyclinic hospital",
                "Fedearl Investigation Authority",
                "Islamabd high court",
                "National Counter Terirorism Authority",
                "National Cybercrime Investigation Agency",
                "PAF College Sargodha",
                "Pakistan Military Depratment Authority",
                "Pakistan medical dental council",
                "Senate of Pakistan",
                "Security Divission",
                "Topians cadet coolage murree",
                "urdu dictionary board",
                "Pakistan Agriculure Resarch Coucil",
                "National Assembly of Pakistan",
                "Aviation Maintenance Training College",
                "Council of Islamaic Idealogy",
                "Pakistan Air Force",
                "Audit Buearu of circulation",
                "Anti_Dumping Appellate Tribunal",
                "Cadet College Spinkai",
                "Controller general of Accounts",
                "Directorate General Munitions Productions",
                "Ministry Of Communications",
                "Directorate General Ports & Shipping , Minisirtry of Maritime Affairs",
                "Directorate General of Religious Education",
                "Directorate General of Special Education, M/o Federal Education & Proessional Training",
                "Directorate of Workers Education",
                "Pakistan Environmental protection agency",
                "Federal Ombudsperson Secretariat For protection Against Harassment",
                "Federal seed Certification and registration Department",
                "Gandahara",
                "Genco Holding Company Limited",
                "Map of Pakistan",
                "Wafaqi Mohtasib (Ombudsman)'s Secretariat",
                "National Commission on status of Women",
                "Management Services wing",
                "National Energy Efficiency & Conservation Authority",
                "National Education Foundation",
                "National Information Technology Board",
                "National Spatial Data Infrastructure",
                "Pakistan Armed Services Board",
                "Pakistan Public Works Department",
                "Pakistan Public Administration Research Centre",
                "Secretariat Training Institute",
                "Trade Dispute Resolution Commission",
                "Workers Welfare Fund",
                "Federal land Commiission",
                "Auditor General Pakistan",
                "Department of Tourist Services",
                "National Heritage and Culture Division",
                "Inter provincial Coordination Division",
                "National Secuirty Division",
                "National Police Academy",
                "Office of the press registrar",
                "Printing Coporation Of Pakistan",
                "Pakistan Institute of Education",
                "Pakistan ManPower Institute",
                "Pakistan Veterinary Medical Council",
                "Directorate General Research & Developnment Estabishment",
                "States and Frontier Region",
                "Survey of Pakistan",
                "Pakistan Sports Board, Islamabad",
                "Nationa Commission for Human Development",
                "FPG",
                "Law & Justice Commission of Pakistan",
                "Office of the Attorney General for Pakistan",
                "Accountant General Pakistan Revenues",
                "Agriculture Policy Institue",
                "Directorate General of Basic Education Community Schools",
                "Secretariat of the council of common Interest",
                "department Of Libraries",
                "ECO postal Staff College",
                "Iqbal Academy Pakistan,",
                "ISA.EDU",
                "Akhtar Hameed Khan National Center For Rural Development",
                "Pakistan Mint",
                "Pakistan Railways Police",
                "Pakistan Agricultral research council",
                "Plant Breeders Right Registry",
                "Pakistan National Commission For UNESCO",
                "Pakistan Oil Seed Department - National Food Secuirty and Research",
                "Establishment Division - Staff Welfare Organization",
                "National Secuirty Printing Company",
                "Paksitan Hockey Federtaion",
                "Prime Minister Insection Commission",
                "Pakistan Information Commiision",
                "Single National Curriculum",
                "PIC.GOV",
                "Quaid-i-Azam Academy",
                "Directorate General of Trade Organization",
                "Gilgit Baltistan Council",
                "Human Organ Transplant Authority",
                "Benazir Income Support programme",
                "Cabinet Division",
                "Establishment Division",
                "Special Invesment Facilitation Council",
                "PKCERT",
                "Inter Boards Coordination Commission",
                "National Poverty Graduation Programme",
                "planning_commision",
                "Ba-Ikhtiyar Naujawan",
                "Plant Protection",
                "NRKNA",
                "Financial Accountin & Budgeting System",
                "PARC",
                "KRL",
                "Law & Justice Commission",
                "NSPP CR HTTPs",
                "Islamabad Excise",
                "CM Sindh",
                "Pakistan Medical & Dental Council",
                "Pakistan Medical Council",
                "PTA",
                "Pakistan Aeronautical Complex",
                "Establishment",
                "National Security Division",
                "Ten Billion Trees",
                "FPSC",
                "Estate Office",
                "National Skill Univeristy",
                "Planning Commission",
                "CPEC",
                "Pakistan Code",
                "Salam Pakistan",
                "Prime Minister Youth Program",
                "National Assembly",
                "Audit oversight Board",
                "President House",
                "Department of archealogy & Museums",
                "Karachi Port Trust",
                "Pastic",
                "National Police Bureaue",
                "PAK RAIL",
                "Natioanl Savings",
                "Sport Board",
                "Board of Invest",
                "Prime Minister Office",
                "Plants Breeders Rights Registry",
                "Overseas pakistanis",
                "Department of Tourist",
                "OGRA",
                "NIEST",
                "Digital Dialogue",
                "DG Religious Affairs",
                "ANF",
                "Marine Fishries Dept",
                "Cannabis Control Regulatory authority"
            };

            // Remove duplicates (case-insensitive) and trim
            var uniqueDepartments = departmentNames
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            // Get existing departments for this ministry to avoid duplicates
            var existingDepartmentNames = await context.Departments
                .Where(d => d.MinistryId == undefinedMinistry.Id && d.DeletedAt == null)
                .Select(d => d.DepartmentName)
                .ToListAsync();

            // Filter out departments that already exist
            var departmentsToAdd = uniqueDepartments
                .Where(name => !existingDepartmentNames.Any(existing => 
                    string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
                .Select(name => new Department
                {
                    MinistryId = undefinedMinistry.Id,
                    DepartmentName = name,
                    ContactName = string.Empty,
                    ContactEmail = string.Empty,
                    ContactPhone = string.Empty,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                })
                .ToList();

            if (departmentsToAdd.Any())
            {
                await context.Departments.AddRangeAsync(departmentsToAdd);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedMetricWeightsAsync(ApplicationDbContext context)
        {
            if (await context.MetricWeights.AnyAsync())
            {
                return; // Already seeded
            }

            var metricWeights = new List<MetricWeights>
            {
                // CHM (Citizen Happiness Metric) weights
                new MetricWeights
                {
                    Category = "CHM",
                    Name = "Accessibility & Inclusivity",
                    Weight = 15.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 18),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "CHM",
                    Name = "Availability & Reliability",
                    Weight = 30.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 18),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "CHM",
                    Name = "Navigation & Discoverability",
                    Weight = 10.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 18),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "CHM",
                    Name = "Performance & Efficiency",
                    Weight = 20.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 18),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "CHM",
                    Name = "Security, Trust & Privacy",
                    Weight = 5.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 18),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "CHM",
                    Name = "User Experience & Journey Quality",
                    Weight = 20.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 19),
                    CreatedBy = "system"
                },
                // OCM (Overall Compliance Metric) weights
                new MetricWeights
                {
                    Category = "OCM",
                    Name = "Accessibility & Inclusivity",
                    Weight = 20.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 19),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "OCM",
                    Name = "Availability & Reliability",
                    Weight = 20.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 19),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "OCM",
                    Name = "Navigation & Discoverability",
                    Weight = 5.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 19),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "OCM",
                    Name = "Performance & Efficiency",
                    Weight = 15.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 19),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "OCM",
                    Name = "Security, Trust & Privacy",
                    Weight = 30.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 19),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "OCM",
                    Name = "User Experience & Journey Quality",
                    Weight = 10.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 19),
                    CreatedBy = "system"
                },
                // DREI (Digital Risk Exposure Index) weights
                new MetricWeights
                {
                    Category = "DREI",
                    Name = "OpenCritical",
                    Weight = 35.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 20),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "DREI",
                    Name = "OpenHigh",
                    Weight = 25.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 20),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "DREI",
                    Name = "OpenMedium",
                    Weight = 15.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 20),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "DREI",
                    Name = "OpenLow",
                    Weight = 5.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 20),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "DREI",
                    Name = "SLABreach",
                    Weight = 20.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 20),
                    CreatedBy = "system"
                },
                // AssetCriticality multipliers
                new MetricWeights
                {
                    Category = "AssetCriticality",
                    Name = "High",
                    Weight = 100.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 21),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "AssetCriticality",
                    Name = "Medium",
                    Weight = 60.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 21),
                    CreatedBy = "system"
                },
                new MetricWeights
                {
                    Category = "AssetCriticality",
                    Name = "Low",
                    Weight = 30.00m,
                    CreatedAt = new DateTime(2026, 1, 28, 20, 28, 21),
                    CreatedBy = "system"
                }
            };

            await context.MetricWeights.AddRangeAsync(metricWeights);
            await context.SaveChangesAsync();
        }
    }
}
