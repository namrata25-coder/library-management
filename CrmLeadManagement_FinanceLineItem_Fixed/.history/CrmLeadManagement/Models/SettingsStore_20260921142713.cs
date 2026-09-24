namespace CrmLeadManagement.Models;

public class UserRoleEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "Active"; // Active / Inactive
}

public class FollowUpSettings
{
    // Always row Id = 1 — this is a single settings record, not a list.
    public int Id { get; set; }

    public int DefaultReminderDaysBefore { get; set; } 
    public bool EnableEmailReminders { get; set; } = true;
    public bool EnableSmsReminders { get; set; } = false;
    public string DefaultFollowUpType { get; set; } = "Call";
    public int AutoEscalateAfterDaysOverdue { get; set; } = 3;
}

/// <summary>
/// Same public API as before (Statuses, Sources, Users, FollowUp, Add/Remove*), so
/// SettingsController / SettingsApiController / LookupData don't need any changes.
/// Internally this now reads/writes SQL Server through AppDbContext instead of an
/// in-memory list.
/// </summary>
public static class SettingsStore
{
    public static List<string> Statuses => DbAccessor.Db.StatusOptions.OrderBy(s => s.Id).Select(s => s.Value).ToList();

    public static List<string> Sources => DbAccessor.Db.SourceOptions.OrderBy(s => s.Id).Select(s => s.Value).ToList();

    public static List<UserRoleEntry> Users => DbAccessor.Db.Users.OrderBy(u => u.Id).ToList();

    /// <summary>Employee names, sourced from Users/Roles so Assign dropdowns stay in sync with Settings.</summary>
    public static List<string> Employees => Users.Select(u => u.Name).ToList();

    public static FollowUpSettings FollowUp
    {
        get
        {
            var db = DbAccessor.Db;
            var row = db.FollowUpSettings.FirstOrDefault();
            if (row is null)
            {
                row = new FollowUpSettings { Id = 1 };
                db.FollowUpSettings.Add(row);
                db.SaveChanges();
            }
            return row;
        }
    }

    public static void AddStatus(string status)
    {
        status = status.Trim();
        if (string.IsNullOrWhiteSpace(status)) return;
        var db = DbAccessor.Db;
        if (!db.StatusOptions.Any(s => s.Value.ToLower() == status.ToLower()))
        {
            db.StatusOptions.Add(new LeadStatusOption { Value = status });
            db.SaveChanges();
        }
    }

    public static void RemoveStatus(string status)
    {
        var db = DbAccessor.Db;
        var existing = db.StatusOptions.FirstOrDefault(s => s.Value.ToLower() == status.ToLower());
        if (existing is not null)
        {
            db.StatusOptions.Remove(existing);
            db.SaveChanges();
        }
    }

    public static void AddSource(string source)
    {
        source = source.Trim();
        if (string.IsNullOrWhiteSpace(source)) return;
        var db = DbAccessor.Db;
        if (!db.SourceOptions.Any(s => s.Value.ToLower() == source.ToLower()))
        {
            db.SourceOptions.Add(new LeadSourceOption { Value = source });
            db.SaveChanges();
        }
    }

    public static void RemoveSource(string source)
    {
        var db = DbAccessor.Db;
        var existing = db.SourceOptions.FirstOrDefault(s => s.Value.ToLower() == source.ToLower());
        if (existing is not null)
        {
            db.SourceOptions.Remove(existing);
            db.SaveChanges();
        }
    }

    public static void AddUser(UserRoleEntry user)
    {
        var db = DbAccessor.Db;
        db.Users.Add(user);
        db.SaveChanges();
    }

    public static void RemoveUser(string email)
    {
        var db = DbAccessor.Db;
        var existing = db.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
        if (existing is not null)
        {
            db.Users.Remove(existing);
            db.SaveChanges();
        }
    }

    public static void UpdateFollowUpSettings(FollowUpSettings updated)
    {
        var db = DbAccessor.Db;
        var row = FollowUp; // ensures the row exists and is tracked
        row.DefaultReminderDaysBefore = updated.DefaultReminderDaysBefore;
        row.EnableEmailReminders = updated.EnableEmailReminders;
        row.EnableSmsReminders = updated.EnableSmsReminders;
        row.DefaultFollowUpType = updated.DefaultFollowUpType;
        row.AutoEscalateAfterDaysOverdue = updated.AutoEscalateAfterDaysOverdue;
        db.SaveChanges();
    }

    /// <summary>Called once at startup (see Program.cs) to seed the same sample statuses/
    /// sources/users the old in-memory version shipped with, but only if the tables are
    /// empty — so it never overwrites real data on subsequent runs.</summary>
    public static void SeedIfEmpty(AppDbContext db)
    {
        if (!db.StatusOptions.Any())
        {
            db.StatusOptions.AddRange(
                new[] { "New", "Contacted", "Qualified", "Proposal Sent", "Negotiation", "Won", "Lost" }
                    .Select(v => new LeadStatusOption { Value = v }));
        }

        if (!db.SourceOptions.Any())
        {
            db.SourceOptions.AddRange(
                new[] { "Website", "Referral", "Cold Call", "Social Media", "Trade Show", "Advertisement", "Partner" }
                    .Select(v => new LeadSourceOption { Value = v }));
        }

        if (!db.Users.Any())
        {
            db.Users.AddRange(new List<UserRoleEntry>
            {
                new UserRoleEntry{ Name="Ananya Sharma", Email="ananya.sharma@nimbuscrm.com", Role="Sales Manager", Status="Active" },
                new UserRoleEntry{ Name="Rohit Verma", Email="rohit.verma@nimbuscrm.com", Role="Sales Executive", Status="Active" },
                new UserRoleEntry{ Name="Priya Nair", Email="priya.nair@nimbuscrm.com", Role="Sales Executive", Status="Active" },
                new UserRoleEntry{ Name="Karan Mehta", Email="karan.mehta@nimbuscrm.com", Role="Sales Executive", Status="Active" },
                new UserRoleEntry{ Name="Sneha Kulkarni", Email="sneha.kulkarni@nimbuscrm.com", Role="Telecaller", Status="Active" },
                new UserRoleEntry{ Name="Arjun Desai", Email="arjun.desai@nimbuscrm.com", Role="Telecaller", Status="Inactive" },
            });
        }

        if (!db.FollowUpSettings.Any())
        {
            db.FollowUpSettings.Add(new FollowUpSettings { Id = 1 });
        }

        db.SaveChanges();
    }
}
