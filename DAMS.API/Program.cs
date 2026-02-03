using DAMS.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DAMS API",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpContextAccessor();

// Add CORS - Allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("*");
    });
});

// Add Infrastructure (includes Identity)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Seed database (with error handling for connection issues)
try
{
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.SeedDatabaseAsync();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database. The application will continue, but some initial data may be missing.");
    // Continue running even if seeding fails
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true)//true for dev
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirection since we're running on HTTP port 5000
// app.UseHttpsRedirection();

// Use CORS - must be before UseAuthentication and UseAuthorization
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//app.Run();
app.Run();

