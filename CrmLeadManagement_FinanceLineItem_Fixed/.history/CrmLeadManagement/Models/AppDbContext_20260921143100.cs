using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Models;

/// <summary>
/// EF Core database context for SQL Server. Replaces the old in-memory
/// LeadStore/SettingsStore lists — everything below now lives in real
/// tables (Leads, CallHistoryEntries, FollowUpEntries, etc.).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<UserRoleEntry> Users => Set<UserRoleEntry>();
    public DbSet<LeadStatusOption> StatusOptions => Set<LeadStatusOption>();
    public DbSet<LeadSourceOption> SourceOptions => Set<LeadSourceOption>();
    public DbSet<FollowUpSettings> FollowUpSettings => Set<FollowUpSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var lead = modelBuilder.Entity<Lead>();
        lead.HasKey(l => l.Id);
        // Business-facing id ("LD-1001") stays unique so LeadStore.GetById(string) keeps working.
        lead.HasIndex(l => l.LeadId).IsUnique();

        // Every collection below "belongs to" a Lead — owned entities are the simplest way
        // to map these 1-to-many child tables without creating separate repository classes.
        lead.OwnsMany(l => l.CallHistory, b => b.WithOwner().HasForeignKey("LeadId_FK"));
        lead.OwnsMany(l => l.FollowUps, b => b.WithOwner().HasForeignKey("LeadId_FK"));
        lead.OwnsMany(l => l.Requirements, b => b.WithOwner().HasForeignKey("LeadId_FK"));
        lead.OwnsMany(l => l.Visits, b => b.WithOwner().HasForeignKey("LeadId_FK"));
        lead.OwnsMany(l => l.AssignmentHistory, b => b.WithOwner().HasForeignKey("LeadId_FK"));

        lead.OwnsMany(l => l.Estimations, est =>
        {
            est.ToTable("Estimations");
            est.WithOwner().HasForeignKey("LeadId_FK");

            // FinanceLineItem is reused (same CLR type) by both EstimationEntry.Items and
            // QuotationEntry.Items below. EF Core treats each as a distinct owned entity type
            // (identified by its "defining navigation" — Estimations.Items vs Quotations.Items),
            // but WITHOUT an explicit ToTable() both would fall back to the same default table
            // name derived from the shared CLR type name — "FinanceLineItem" — which collides
            // and is exactly why SQL Server never ended up with a usable table by that name
            // (every insert then failed with "Invalid object name 'FinanceLineItem'"). Giving
            // each nested collection its own explicit table name removes that ambiguity.
            //
            // No HasKey() call here (same as the sibling collections above): leaving the
            // composite key on convention is what makes EF auto-generate a unique "Id" value
            // for each new row scoped to its owner (FinanceLineItem.Id / EstimationEntry.Id
            // already exist as real int properties, so calling HasKey explicitly here would
            // switch that key to "value never generated", and every new item/estimation for
            // the same parent would try to insert with Id = 0 and collide).
            est.OwnsMany(e => e.Items, item =>
            {
                item.ToTable("EstimationLineItems");
                item.WithOwner(nameof(FinanceLineItem.Estimation))
                    .HasForeignKey(nameof(FinanceLineItem.EstimationId_FK))
                    .HasPrincipalKey(nameof(EstimationEntry.Id));
            });
        });
        lead.OwnsMany(l => l.Quotations, quo =>
        {
            quo.ToTable("Quotations");
            quo.WithOwner().HasForeignKey("LeadId_FK");

            quo.OwnsMany(q => q.Items, item =>
            {
                item.ToTable("QuotationLineItems");
                item.Ignore(nameof(FinanceLineItem.Estimation));
                item.Ignore(nameof(FinanceLineItem.EstimationId_FK));
                item.WithOwner()
                    .HasForeignKey("QuotationId_FK")
                    .HasPrincipalKey(nameof(QuotationEntry.Id));
            });
        });
        lead.OwnsMany(l => l.Proposals, b =>
        {
            b.ToTable("Proposals");
            b.WithOwner().HasForeignKey("LeadId_FK");
        });

        modelBuilder.Entity<UserRoleEntry>().HasKey(u => u.Id);
        modelBuilder.Entity<UserRoleEntry>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<LeadStatusOption>().HasKey(s => s.Id);
        modelBuilder.Entity<LeadStatusOption>().HasIndex(s => s.Value).IsUnique();

        modelBuilder.Entity<LeadSourceOption>().HasKey(s => s.Id);
        modelBuilder.Entity<LeadSourceOption>().HasIndex(s => s.Value).IsUnique();

        // Single settings row (id is always 1) — see SettingsStore.FollowUp.
        
    }
}

/// <summary>One selectable Lead Status value (Settings > Lead Status).</summary>
public class LeadStatusOption
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}

/// <summary>One selectable Lead Source value (Settings > Lead Sources).</summary>
public class LeadSourceOption
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}
