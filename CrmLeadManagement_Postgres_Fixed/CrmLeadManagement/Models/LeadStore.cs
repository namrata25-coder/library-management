using Microsoft.EntityFrameworkCore;

namespace CrmLeadManagement.Models;

/// <summary>
/// Same public API as before (Leads, GetById, AddLead, UpdateLead, DeleteLead, Next*Number),
/// so LeadController / LeadsApiController / FinanceController don't need any changes.
/// Internally this now reads/writes SQL Server through AppDbContext instead of an
/// in-memory list. See DbAccessor for how the DbContext is reached from here.
/// </summary>
public static class LeadStore
{
    /// <summary>All leads, with their call/follow-up/requirement/visit/finance history loaded.</summary>
    public static List<Lead> Leads => DbAccessor.Db.Leads.ToList();

    /// <summary>Returns a tracked entity — callers can mutate it (e.g. lead.CallHistory.Add(...))
    /// and the change is saved automatically at the end of the request.</summary>
    public static Lead? GetById(string id) => DbAccessor.Db.Leads.FirstOrDefault(l => l.LeadId == id);

    /// <summary>Generates the next sequential Lead ID, e.g. LD-1009.</summary>
    public static string NextId()
    {
        var maxNum = Leads
            .Select(l => l.LeadId.Replace("LD-", ""))
            .Select(n => int.TryParse(n, out var v) ? v : 1000)
            .DefaultIfEmpty(1000)
            .Max();
        return $"LD-{maxNum + 1}";
    }

    /// <summary>Generates the next sequential Estimation number, e.g. EST-2005.</summary>
    public static string NextEstimationNumber()
    {
        var maxNum = Leads.SelectMany(l => l.Estimations)
            .Select(e => e.Number.Replace("EST-", ""))
            .Select(n => int.TryParse(n, out var v) ? v : 2000)
            .DefaultIfEmpty(2000)
            .Max();
        return $"EST-{maxNum + 1}";
    }

    /// <summary>Generates the next sequential Quotation number, e.g. QUO-2005.</summary>
    public static string NextQuotationNumber()
    {
        var maxNum = Leads.SelectMany(l => l.Quotations)
            .Select(q => q.Number.Replace("QUO-", ""))
            .Select(n => int.TryParse(n, out var v) ? v : 2000)
            .DefaultIfEmpty(2000)
            .Max();
        return $"QUO-{maxNum + 1}";
    }

    /// <summary>Generates the next sequential Proposal number, e.g. PRO-2005.</summary>
    public static string NextProposalNumber()
    {
        var maxNum = Leads.SelectMany(l => l.Proposals)
            .Select(p => p.Number.Replace("PRO-", ""))
            .Select(n => int.TryParse(n, out var v) ? v : 2000)
            .DefaultIfEmpty(2000)
            .Max();
        return $"PRO-{maxNum + 1}";
    }

    /// <summary>Inserts a brand-new lead into the database. Shows up immediately in Lead List / Assigned List.</summary>
    public static Lead AddLead(Lead lead)
    {
        if (string.IsNullOrWhiteSpace(lead.LeadId))
            lead.LeadId = NextId();
        if (string.IsNullOrWhiteSpace(lead.CreatedDate))
            lead.CreatedDate = DateTime.Now.ToString("yyyy-MM-dd");
        if (string.IsNullOrWhiteSpace(lead.AssignedDate))
            lead.AssignedDate = DateTime.Now.ToString("yyyy-MM-dd");

        DbAccessor.Db.Leads.Add(lead);
        DbAccessor.Db.SaveChanges();
        return lead;
    }

    /// <summary>
    /// Updates the core fields of an existing lead in place, preserving its call/follow-up/
    /// requirement/visit history so nothing already logged against the lead is lost.
    /// </summary>
    public static bool UpdateLead(Lead updated)
    {
        var existing = GetById(updated.LeadId);
        if (existing is null) return false;

        existing.LeadName = updated.LeadName;
        existing.CompanyName = updated.CompanyName;
        existing.MobileNumber = updated.MobileNumber;
        existing.Email = updated.Email;
        existing.Designation = updated.Designation;
        existing.Location = updated.Location;
        existing.LeadStatus = updated.LeadStatus;
        existing.Source = updated.Source;
        existing.AssignedTo = updated.AssignedTo;
        existing.FollowUpDate = updated.FollowUpDate;
        existing.Priority = updated.Priority;
        existing.Remarks = updated.Remarks;

        DbAccessor.Db.SaveChanges();
        return true;
    }

    /// <summary>Removes a lead permanently from the database.</summary>
    public static bool DeleteLead(string id)
    {
        var existing = GetById(id);
        if (existing is null) return false;

        DbAccessor.Db.Leads.Remove(existing);
        DbAccessor.Db.SaveChanges();
        return true;
    }

    /// <summary>Called once at startup (see Program.cs) to seed the same sample leads the
    /// old in-memory version shipped with, but only if the Leads table is empty — so it
    /// never overwrites real data on subsequent runs.</summary>
    public static void SeedIfEmpty(AppDbContext db)
    {
        if (db.Leads.Any()) return;

        db.Leads.AddRange(new List<Lead>
        {
            new Lead
            {
                LeadId = "LD-1001",
                LeadName = "Vikram Rao",
                CompanyName = "Orion Steel Industries",
                MobileNumber = "+91 98765 43210",
                Email = "vikram.rao@orionsteel.com",
                Designation = "Procurement Manager",
                Location = "Pune, Maharashtra",
                LeadStatus = "Qualified",
                Source = "Website",
                AssignedTo = "Ananya Sharma",
                CreatedDate = "2026-08-02",
                FollowUpDate = "2026-09-15",
                Priority = "High",
                AssignedDate = "2026-08-03",
                Remarks = "Interested in bulk steel supply for new plant. Requested price quote.",
                CallHistory = new()
                {
                    new CallHistoryEntry{ CallDate="2026-08-04", CallTime="10:15 AM", CallType="Outgoing", Duration="6m 20s", Employee="Ananya Sharma", CallStatus="Connected", Notes="Discussed requirement scope, sending catalog." },
                    new CallHistoryEntry{ CallDate="2026-08-10", CallTime="03:40 PM", CallType="Incoming", Duration="3m 05s", Employee="Ananya Sharma", CallStatus="Connected", Notes="Client asked about delivery timelines." },
                },
                FollowUps = new()
                {
                    new FollowUpEntry{ FollowUpDate="2026-08-12", FollowUpTime="11:00 AM", FollowUpType="Email", AssignedEmployee="Ananya Sharma", Status="Completed", Remarks="Sent detailed price quote via email." },
                    new FollowUpEntry{ FollowUpDate="2026-09-15", FollowUpTime="02:00 PM", FollowUpType="Call", AssignedEmployee="Ananya Sharma", Status="Pending", Remarks="Confirm decision on quote." },
                },
                Requirements = new()
                {
                    new RequirementEntry{ RequirementType="Price Quote", ItemName="MS Steel Rods (12mm)", Quantity="500 tons", Priority="High", Details="Quote needed for Q4 plant expansion.", ExpectedDate="2026-08-15", Remarks="Urgent, competing with two other vendors." }
                },
                Visits = new()
                {
                    new VisitEntry{ VisitDate="2026-08-06", VisitTime="11:30 AM", VisitLocation="Orion Steel Industries, MIDC Bhosari, Pune", Employee="Ananya Sharma", Conversation="Walked through the plant expansion plan and shared sample catalog.", Outcome="Interested", Remarks="Client wants a formal price quote within a week." },
                    new VisitEntry{ VisitDate="2026-09-02", VisitTime="04:00 PM", VisitLocation="Orion Steel Industries, MIDC Bhosari, Pune", Employee="Ananya Sharma", Conversation="Reviewed the quote in person, discussed payment terms.", Outcome="Follow-up Needed", Remarks="Awaiting internal budget approval on their end." },
                }
            },
            new Lead
            {
                LeadId = "LD-1002",
                LeadName = "Meera Iyer",
                CompanyName = "Bluewave Textiles",
                MobileNumber = "+91 91234 56780",
                Email = "meera.iyer@bluewavetex.com",
                Designation = "CEO",
                Location = "Coimbatore, Tamil Nadu",
                LeadStatus = "New",
                Source = "Referral",
                AssignedTo = "Rohit Verma",
                CreatedDate = "2026-09-01",
                FollowUpDate = "2026-09-14",
                Priority = "Medium",
                AssignedDate = "2026-09-01",
                Remarks = "Referred by an existing client. Yet to be contacted.",
            },
            new Lead
            {
                LeadId = "LD-1003",
                LeadName = "Sanjay Kapoor",
                CompanyName = "Nexgen Electronics",
                MobileNumber = "+91 90000 11223",
                Email = "sanjay.k@nexgenelec.in",
                Designation = "Operations Head",
                Location = "Bengaluru, Karnataka",
                LeadStatus = "Proposal Sent",
                Source = "Trade Show",
                AssignedTo = "Priya Nair",
                CreatedDate = "2026-07-20",
                FollowUpDate = "2026-09-13",
                Priority = "High",
                AssignedDate = "2026-07-21",
                Remarks = "Met at Bengaluru tech expo, strong interest in demo units.",
                CallHistory = new()
                {
                    new CallHistoryEntry{ CallDate="2026-07-25", CallTime="12:00 PM", CallType="Outgoing", Duration="8m 40s", Employee="Priya Nair", CallStatus="Connected", Notes="Walked through product lineup." }
                },
                Requirements = new()
                {
                    new RequirementEntry{ RequirementType="Demo", ItemName="NX-200 Controller Unit", Quantity="2 units", Priority="Medium", Details="On-site demo requested for engineering team.", ExpectedDate="2026-09-20", Remarks="Prefers demo on a Friday." }
                },
                Visits = new()
                {
                    new VisitEntry{ VisitDate="2026-07-28", VisitTime="02:15 PM", VisitLocation="Nexgen Electronics, Whitefield, Bengaluru", Employee="Priya Nair", Conversation="Introduced the product lineup and answered technical questions.", Outcome="Interested", Remarks="Engineering team to review specs before demo." }
                }
            },
            new Lead
            {
                LeadId = "LD-1004",
                LeadName = "Farhan Sheikh",
                CompanyName = "Coastal Logistics Pvt Ltd",
                MobileNumber = "+91 99887 76655",
                Email = "farhan.sheikh@coastallog.com",
                Designation = "Fleet Manager",
                Location = "Mumbai, Maharashtra",
                LeadStatus = "Negotiation",
                Source = "Cold Call",
                AssignedTo = "Karan Mehta",
                CreatedDate = "2026-06-11",
                FollowUpDate = "2026-09-16",
                Priority = "High",
                AssignedDate = "2026-06-12",
                Remarks = "Negotiating annual maintenance contract terms.",
                Visits = new()
                {
                    new VisitEntry{ VisitDate="2026-09-05", VisitTime="10:00 AM", VisitLocation="Coastal Logistics Pvt Ltd, JNPT Road, Mumbai", Employee="Karan Mehta", Conversation="Discussed AMC pricing tiers and SLA terms with the fleet team.", Outcome="Follow-up Needed", Remarks="Waiting on their legal team to review the contract draft." }
                }
            },
            new Lead
            {
                LeadId = "LD-1005",
                LeadName = "Divya Menon",
                CompanyName = "Sunrise Pharma",
                MobileNumber = "+91 98111 22334",
                Email = "divya.menon@sunrisepharma.com",
                Designation = "Supply Chain Lead",
                Location = "Hyderabad, Telangana",
                LeadStatus = "Contacted",
                Source = "Social Media",
                AssignedTo = "Sneha Kulkarni",
                CreatedDate = "2026-08-25",
                FollowUpDate = "2026-09-18",
                Priority = "Low",
                AssignedDate = "2026-08-26",
                Remarks = "Early stage conversation, gauging interest.",
            },
            new Lead
            {
                LeadId = "LD-1006",
                LeadName = "Aditya Joshi",
                CompanyName = "Greenfield Agro",
                MobileNumber = "+91 97766 55443",
                Email = "aditya.joshi@greenfieldagro.in",
                Designation = "Director",
                Location = "Nagpur, Maharashtra",
                LeadStatus = "Won",
                Source = "Partner",
                AssignedTo = "Arjun Desai",
                CreatedDate = "2026-05-15",
                FollowUpDate = "2026-09-10",
                Priority = "Medium",
                AssignedDate = "2026-05-16",
                Remarks = "Deal closed, onboarding in progress.",
            },
            new Lead
            {
                LeadId = "LD-1007",
                LeadName = "Neha Kulkarni",
                CompanyName = "Skyline Constructions",
                MobileNumber = "+91 96655 44332",
                Email = "neha.k@skylineconstruct.com",
                Designation = "Project Manager",
                Location = "Nashik, Maharashtra",
                LeadStatus = "Lost",
                Source = "Advertisement",
                AssignedTo = "Ananya Sharma",
                CreatedDate = "2026-04-02",
                FollowUpDate = "2026-06-05",
                Priority = "Low",
                AssignedDate = "2026-04-03",
                Remarks = "Went with a competitor due to pricing.",
            },
            new Lead
            {
                LeadId = "LD-1008",
                LeadName = "Ramesh Chandran",
                CompanyName = "Pinnacle Realty",
                MobileNumber = "+91 95544 33221",
                Email = "ramesh.c@pinnaclerealty.in",
                Designation = "VP Sales",
                Location = "Chennai, Tamil Nadu",
                LeadStatus = "Qualified",
                Source = "Website",
                AssignedTo = "Rohit Verma",
                CreatedDate = "2026-08-18",
                FollowUpDate = "2026-09-14",
                Priority = "Medium",
                AssignedDate = "2026-08-19",
                Remarks = "Wants a tailored proposal for 3 upcoming sites.",
            },
        });

        db.SaveChanges();
    }
}
