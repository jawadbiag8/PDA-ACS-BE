using DAMS.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAMS.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ministry> Ministries => Set<Ministry>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<CommonLookup> CommonLookups => Set<CommonLookup>();
        public DbSet<KpisLov> KpisLovs => Set<KpisLov>();
        public DbSet<KPIsResult> KPIsResults => Set<KPIsResult>();
        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<KPIsResultHistory> KPIsResultHistories => Set<KPIsResultHistory>();
        public DbSet<IncidentHistory> IncidentHistories => Set<IncidentHistory>();
        public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
        public DbSet<AssetMetrics> AssetMetrics => Set<AssetMetrics>();
        public DbSet<MetricWeights> MetricWeights => Set<MetricWeights>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
            });

            // Configure ApplicationRole
            builder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // Configure Ministry
            builder.Entity<Ministry>(entity =>
            {
                entity.ToTable("Ministries");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MinistryName).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.MinistryName).IsUnique();
                entity.Property(e => e.ContactName).HasMaxLength(255);
                entity.Property(e => e.ContactEmail).HasMaxLength(255);
                entity.Property(e => e.ContactPhone).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.DeletedBy).HasMaxLength(255);
            });

            // Configure Department
            builder.Entity<Department>(entity =>
            {
                entity.ToTable("Departments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => new { e.DepartmentName, e.MinistryId }).IsUnique();
                entity.Property(e => e.ContactName).HasMaxLength(255);
                entity.Property(e => e.ContactEmail).HasMaxLength(255);
                entity.Property(e => e.ContactPhone).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.DeletedBy).HasMaxLength(255);
                entity.HasOne(e => e.Ministry)
                    .WithMany(m => m.Departments)
                    .HasForeignKey(e => e.MinistryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CommonLookup
            builder.Entity<CommonLookup>(entity =>
            {
                entity.ToTable("CommonLookup");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => new { e.Name, e.Type }).IsUnique();
            });

            // Configure Asset
            builder.Entity<Asset>(entity =>
            {
                entity.ToTable("Assets");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AssetName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AssetUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.PrimaryContactName).HasMaxLength(255);
                entity.Property(e => e.PrimaryContactEmail).HasMaxLength(255);
                entity.Property(e => e.PrimaryContactPhone).HasMaxLength(50);
                entity.Property(e => e.TechnicalContactName).HasMaxLength(255);
                entity.Property(e => e.TechnicalContactEmail).HasMaxLength(255);
                entity.Property(e => e.TechnicalContactPhone).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.DeletedBy).HasMaxLength(255);
                entity.HasOne(e => e.Ministry)
                    .WithMany(m => m.Assets)
                    .HasForeignKey(e => e.MinistryId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Assets)
                    .HasForeignKey(e => e.DepartmentId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.CitizenImpactLevel)
                    .WithMany(cl => cl.Assets)
                    .HasForeignKey(e => e.CitizenImpactLevelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure AssetMetrics
            builder.Entity<AssetMetrics>(entity =>
            {
                entity.ToTable("AssetMetrics");
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Asset)
                    .WithMany(a => a.AssetMetrics)
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.AssetId).HasDatabaseName("UX_AssetMetrics_AssetId").IsUnique();
                entity.HasIndex(e => new { e.AssetId, e.PeriodStartDate, e.PeriodEndDate });
            });

            // Configure MetricWeights
            builder.Entity<MetricWeights>(entity =>
            {
                entity.ToTable("MetricWeights");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Weight).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.DeletedBy).HasMaxLength(255);
            });

            // Configure KpisLov
            builder.Entity<KpisLov>(entity =>
            {
                entity.ToTable("KpisLov");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.KpiName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.KpiGroup).HasMaxLength(255);
                entity.Property(e => e.Manual).HasMaxLength(255);
                entity.Property(e => e.Frequency).HasMaxLength(255);
                entity.Property(e => e.Outcome).HasMaxLength(255);
                entity.Property(e => e.PagesToCheck).HasMaxLength(500);
                entity.Property(e => e.TargetType).HasMaxLength(255);
                entity.Property(e => e.TargetHigh).HasMaxLength(255);
                entity.Property(e => e.TargetMedium).HasMaxLength(255);
                entity.Property(e => e.TargetLow).HasMaxLength(255);
                entity.Property(e => e.KpiType).HasMaxLength(255);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.DeletedBy).HasMaxLength(255);
                entity.HasOne(e => e.Severity)
                    .WithMany()
                    .HasForeignKey(e => e.SeverityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure KPIsResult
            builder.Entity<KPIsResult>(entity =>
            {
                entity.ToTable("kpisResults");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Result).HasMaxLength(500);
                entity.Property(e => e.Details).HasColumnType("text");
                entity.HasOne(e => e.Asset)
                    .WithMany(a => a.KPIsResults)
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.KpisLov)
                    .WithMany(k => k.KPIsResults)
                    .HasForeignKey(e => e.KpiId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.AssetId, e.KpiId }).IsUnique();
            });

            // Configure Incident
            builder.Entity<Incident>(entity =>
            {
                entity.ToTable("Incidents");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IncidentTitle).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Type).HasMaxLength(255);
                entity.Property(e => e.AssignedTo).HasMaxLength(255);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.UpdatedBy).HasMaxLength(255);
                entity.Property(e => e.DeletedBy).HasMaxLength(255);
                entity.HasOne(e => e.Asset)
                    .WithMany(a => a.Incidents)
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.KpisLov)
                    .WithMany()
                    .HasForeignKey(e => e.KpiId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Severity)
                    .WithMany()
                    .HasForeignKey(e => e.SeverityId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Status)
                    .WithMany()
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure KPIsResultHistory
            builder.Entity<KPIsResultHistory>(entity =>
            {
                entity.ToTable("KPIsResultHistories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Result).HasMaxLength(500);
                entity.Property(e => e.Details).HasColumnType("text");
                entity.Property(e => e.Target).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.Asset)
                    .WithMany(a => a.KPIsResultHistories)
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.KpisLov)
                    .WithMany()
                    .HasForeignKey(e => e.KpiId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.KPIsResult)
                    .WithMany()
                    .HasForeignKey(e => e.KPIsResultId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure IncidentHistory
            builder.Entity<IncidentHistory>(entity =>
            {
                entity.ToTable("IncidentHistories");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IncidentTitle).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Type).HasMaxLength(255);
                entity.Property(e => e.AssignedTo).HasMaxLength(255);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.HasOne(e => e.Severity)
                    .WithMany()
                    .HasForeignKey(e => e.SeverityId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Status)
                    .WithMany()
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Incident)
                    .WithMany(i => i.History)
                    .HasForeignKey(e => e.IncidentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.KpisLov)
                    .WithMany()
                    .HasForeignKey(e => e.KpiId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure IncidentComment
            builder.Entity<IncidentComment>(entity =>
            {
                entity.ToTable("IncidentComments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Comment).HasColumnType("text").IsRequired();
                entity.Property(e => e.Status).HasMaxLength(255);
                entity.Property(e => e.CreatedBy).HasMaxLength(255).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.Incident)
                    .WithMany(i => i.Comments)
                    .HasForeignKey(e => e.IncidentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
