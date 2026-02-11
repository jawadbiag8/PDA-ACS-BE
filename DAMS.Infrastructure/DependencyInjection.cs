using DAMS.Application.Interfaces;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using DAMS.Infrastructure.Seeding;
using DAMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Text;

namespace DAMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connectionString ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."), 
                    new MySqlServerVersion(new Version(10, 11, 14)),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        mySqlOptions.EnableStringComparisonTranslations();
                    }));

            // Add Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not found.");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddHttpClient();

            // Register services
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMinistryService, MinistryService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IAssetService, AssetService>();
            services.AddScoped<IIncidentService, IncidentService>();
            services.AddScoped<ICommonLookupService, CommonLookupService>();
            services.AddScoped<IKpisLovService, KpisLovService>();
            services.AddScoped<IPMDashboardService, PMDashboardService>();
            services.AddScoped<IMinistryReportService, MinistryReportService>();

            return services;
        }

        public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
        {
            await DatabaseSeeder.SeedAsync(serviceProvider);
        }
    }
}

