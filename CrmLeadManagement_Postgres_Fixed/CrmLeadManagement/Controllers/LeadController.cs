using CrmLeadManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadManagement.Controllers;

public class LeadController : Controller
{
    // GET /Lead/List
    public IActionResult List()
    {
        ViewData["Title"] = "Lead List";
        ViewData["ActivePage"] = "LeadList";
        return View(LeadStore.Leads);
    }

    // GET /Lead/Assigned
    public IActionResult Assigned()
    {
        ViewData["Title"] = "Assigned Leads";
        ViewData["ActivePage"] = "AssignedList";
        return View(LeadStore.Leads);
    }

    // GET /Lead/Details?id=LD-1001&tab=call
    // subtab is only relevant when tab=finance (estimation | quotation | proposal)
    public IActionResult Details(string id, string? tab, string? subtab)
    {
        var lead = LeadStore.GetById(id);
        if (lead is null)
        {
            return RedirectToAction("List");
        }

        ViewData["Title"] = "Lead Details";
        ViewData["ActivePage"] = "LeadList";
        ViewData["ActiveTab"] = string.IsNullOrWhiteSpace(tab) ? "call" : tab;
        ViewData["ActiveFinanceSubTab"] = string.IsNullOrWhiteSpace(subtab) ? "estimation" : subtab;
        return View(lead);
    }

    /// <summary>
    /// Handles both Add (empty LeadId) and Edit/Reassign (existing LeadId) from the
    /// shared lead modal. Shared by both the Lead List and Assigned List pages, which
    /// each pass their own URL back in returnUrl so the user lands back where they were.
    /// </summary>
    [HttpPost]
    public IActionResult Save(LeadInputModel Input, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(Input.LeadId))
        {
            LeadStore.AddLead(Input.ToLead());
        }
        else
        {
            LeadStore.UpdateLead(Input.ToLead());
        }

        return RedirectToReturnUrl(returnUrl, "List");
    }

    [HttpPost]
    public IActionResult Delete(string id, string? returnUrl)
    {
        LeadStore.DeleteLead(id);
        return RedirectToReturnUrl(returnUrl, "List");
    }

    /// <summary>Logs a new call against the lead and returns to the Call tab.</summary>
    [HttpPost]
    public IActionResult AddCall(string leadId, string callDate, string callTime, string callType, string callStatus, string duration, string? remarks)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is null) return RedirectToAction("List");

        if (!string.IsNullOrWhiteSpace(callDate) && !string.IsNullOrWhiteSpace(callTime) && !string.IsNullOrWhiteSpace(callStatus))
        {
            lead.CallHistory.Add(new CallHistoryEntry
            {
                CallDate = callDate,
                CallTime = callTime,
                CallType = string.IsNullOrWhiteSpace(callType) ? "Outgoing" : callType,
                CallStatus = callStatus,
                Duration = duration ?? "",
                Employee = lead.AssignedTo,
                Notes = remarks ?? ""
            });
        }

        return RedirectToAction("Details", new { id = leadId, tab = "call" });
    }

    /// <summary>Logs a new follow-up against the lead and returns to the Follow-up tab.</summary>
    [HttpPost]
    public IActionResult AddFollowUp(string leadId, string followUpDate, string followUpTime, string status, string? remarks)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is null) return RedirectToAction("List");

        if (!string.IsNullOrWhiteSpace(followUpDate) && !string.IsNullOrWhiteSpace(followUpTime) && !string.IsNullOrWhiteSpace(status))
        {
            lead.FollowUps.Add(new FollowUpEntry
            {
                FollowUpDate = followUpDate,
                FollowUpTime = followUpTime,
                FollowUpType = "Call",
                AssignedEmployee = lead.AssignedTo,
                Status = status,
                Remarks = remarks ?? ""
            });

            // Keep the lead's "next follow-up" quick-glance field in sync with the latest entry.
            lead.FollowUpDate = followUpDate;
        }

        return RedirectToAction("Details", new { id = leadId, tab = "followup" });
    }

    /// <summary>Adds a new requirement (with optional image) against the lead and returns to the Requirement tab.</summary>
    [HttpPost]
    public async Task<IActionResult> AddRequirement(string leadId, string title, string description, string priority,
        string expectedDate, string budget, string status, IFormFile? image)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is null) return RedirectToAction("List");

        // Basic server-side validation — required fields must be present.
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(priority) || string.IsNullOrWhiteSpace(status))
        {
            TempData["RequirementError"] = "Requirement Title, Priority and Status are required.";
            return RedirectToAction("Details", new { id = leadId, tab = "requirement" });
        }

        string imagePath = "";
        if (image is not null && image.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (allowedExtensions.Contains(ext))
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "requirements");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imagePath = $"/uploads/requirements/{fileName}";
            }
        }

        lead.Requirements.Add(new RequirementEntry
        {
            Title = title,
            Description = description ?? "",
            Priority = priority,
            ExpectedDate = expectedDate ?? "",
            Budget = budget ?? "",
            Status = status,
            ImagePath = imagePath
        });

        return RedirectToAction("Details", new { id = leadId, tab = "requirement" });
    }

    /// <summary>Assigns or re-assigns the lead to an employee and logs the change.</summary>
    [HttpPost]
    public IActionResult AssignLead(string leadId, string assignedEmployee, string assignmentDate, string? remarks)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is null) return RedirectToAction("List");

        if (!string.IsNullOrWhiteSpace(assignedEmployee) && !string.IsNullOrWhiteSpace(assignmentDate))
        {
            lead.AssignmentHistory.Add(new AssignmentEntry
            {
                AssignedEmployee = assignedEmployee,
                AssignmentDate = assignmentDate,
                Remarks = remarks ?? ""
            });

            lead.AssignedTo = assignedEmployee;
            lead.AssignedDate = assignmentDate;
        }

        return RedirectToAction("Details", new { id = leadId, tab = "assign" });
    }

    // ===================== FINANCE: ESTIMATION =====================

    /// <summary>Creates a new Estimation, or updates an existing one when 'number' is supplied. Always linked to leadId.</summary>
    [HttpPost]
    public async Task<IActionResult> SaveEstimation(string leadId, string? number, string date, string projectRequirement,
        List<string>? itemName, List<string>? itemQty, List<string>? itemRate,
        string? discount, string? tax, string? validUntil, string? terms, string? notes, string status, IFormFile? attachment)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is null) return RedirectToAction("List");

        var items = BuildLineItems(itemName, itemQty, itemRate);
        var subtotal = items.Sum(i => ParseDec(i.Amount));
        var discountVal = ParseDec(discount);
        var taxVal = ParseDec(tax);
        var total = subtotal - discountVal + taxVal;

        var existing = string.IsNullOrWhiteSpace(number) ? null : lead.Estimations.FirstOrDefault(e => e.Number == number);
        var entry = existing ?? new EstimationEntry { Number = LeadStore.NextEstimationNumber() };

        var (attPath, attName, attError) = await FinanceAttachmentService.SaveAsync(attachment, "estimations");
        if (attError is not null)
        {
            TempData["EstimationError"] = attError;
            return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "estimation" });
        }
        if (!string.IsNullOrWhiteSpace(attPath))
        {
            // Replacing an existing attachment — remove the old file so uploads don't pile up.
            FinanceAttachmentService.Delete(entry.AttachmentPath);
            entry.AttachmentPath = attPath;
            entry.AttachmentFileName = attName;
        }

        entry.Date = date ?? "";
        entry.LeadCustomer = $"{lead.LeadName} ({lead.CompanyName})";
        entry.ProjectRequirement = projectRequirement ?? "";
        entry.Items = items;
        entry.Subtotal = subtotal.ToString("0.##");
        entry.Discount = string.IsNullOrWhiteSpace(discount) ? "0" : discount;
        entry.Tax = string.IsNullOrWhiteSpace(tax) ? "0" : tax;
        entry.Total = total.ToString("0.##");
        entry.ValidUntil = validUntil ?? "";
        entry.TermsConditions = terms ?? "";
        entry.Notes = notes ?? "";
        entry.Status = string.IsNullOrWhiteSpace(status) ? "Draft" : status;

        if (existing is null) lead.Estimations.Add(entry);

        return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "estimation" });
    }

    [HttpPost]
    public IActionResult DeleteEstimation(string leadId, string number)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is not null)
        {
            var existing = lead.Estimations.FirstOrDefault(e => e.Number == number);
            if (existing is not null)
            {
                FinanceAttachmentService.Delete(existing.AttachmentPath);
                lead.Estimations.Remove(existing);
            }
        }
        return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "estimation" });
    }

    // ===================== FINANCE: QUOTATION =====================

    /// <summary>Creates a new Quotation, or updates an existing one when 'number' is supplied. Always linked to leadId.</summary>
    [HttpPost]
    public async Task<IActionResult> SaveQuotation(string leadId, string? number, string date, string projectRequirement,
        List<string>? itemName, List<string>? itemQty, List<string>? itemRate,
        string? discount, string? tax, string? validUntil, string? terms, string? notes, string status, IFormFile? attachment)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is null) return RedirectToAction("List");

        var items = BuildLineItems(itemName, itemQty, itemRate);
        var subtotal = items.Sum(i => ParseDec(i.Amount));
        var discountVal = ParseDec(discount);
        var taxVal = ParseDec(tax);
        var grandTotal = subtotal - discountVal + taxVal;

        var existing = string.IsNullOrWhiteSpace(number) ? null : lead.Quotations.FirstOrDefault(q => q.Number == number);
        var entry = existing ?? new QuotationEntry { Number = LeadStore.NextQuotationNumber() };

        var (attPath, attName, attError) = await FinanceAttachmentService.SaveAsync(attachment, "quotations");
        if (attError is not null)
        {
            TempData["QuotationError"] = attError;
            return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "quotation" });
        }
        if (!string.IsNullOrWhiteSpace(attPath))
        {
            FinanceAttachmentService.Delete(entry.AttachmentPath);
            entry.AttachmentPath = attPath;
            entry.AttachmentFileName = attName;
        }

        entry.Date = date ?? "";
        entry.LeadCustomer = $"{lead.LeadName} ({lead.CompanyName})";
        entry.ProjectRequirement = projectRequirement ?? "";
        entry.Items = items;
        entry.Subtotal = subtotal.ToString("0.##");
        entry.Discount = string.IsNullOrWhiteSpace(discount) ? "0" : discount;
        entry.Tax = string.IsNullOrWhiteSpace(tax) ? "0" : tax;
        entry.GrandTotal = grandTotal.ToString("0.##");
        entry.ValidUntil = validUntil ?? "";
        entry.TermsConditions = terms ?? "";
        entry.Notes = notes ?? "";
        entry.Status = string.IsNullOrWhiteSpace(status) ? "Draft" : status;

        if (existing is null) lead.Quotations.Add(entry);

        return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "quotation" });
    }

    [HttpPost]
    public IActionResult DeleteQuotation(string leadId, string number)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is not null)
        {
            var existing = lead.Quotations.FirstOrDefault(q => q.Number == number);
            if (existing is not null)
            {
                FinanceAttachmentService.Delete(existing.AttachmentPath);
                lead.Quotations.Remove(existing);
            }
        }
        return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "quotation" });
    }

    // ===================== FINANCE: PROPOSAL =====================

    /// <summary>Creates a new Proposal, or updates an existing one when 'number' is supplied. Always linked to leadId.</summary>
    [HttpPost]
    public async Task<IActionResult> SaveProposal(string leadId, string? number, string date, string projectRequirement,
        string title, string? scopeOfWork, string? description, string? deliverables, string? timeline,
        string? budget, string? terms, string? notes, string status, IFormFile? attachment)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is null) return RedirectToAction("List");

        var existing = string.IsNullOrWhiteSpace(number) ? null : lead.Proposals.FirstOrDefault(p => p.Number == number);
        var entry = existing ?? new ProposalEntry { Number = LeadStore.NextProposalNumber() };

        var (attPath, attName, attError) = await FinanceAttachmentService.SaveAsync(attachment, "proposals");
        if (attError is not null)
        {
            TempData["ProposalError"] = attError;
            return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "proposal" });
        }
        if (!string.IsNullOrWhiteSpace(attPath))
        {
            FinanceAttachmentService.Delete(entry.AttachmentPath);
            entry.AttachmentPath = attPath;
            entry.AttachmentFileName = attName;
        }

        entry.Date = date ?? "";
        entry.LeadCustomer = $"{lead.LeadName} ({lead.CompanyName})";
        entry.ProjectRequirement = projectRequirement ?? "";
        entry.Title = title ?? "";
        entry.ScopeOfWork = scopeOfWork ?? "";
        entry.Description = description ?? "";
        entry.Deliverables = deliverables ?? "";
        entry.Timeline = timeline ?? "";
        entry.Budget = budget ?? "";
        entry.TermsConditions = terms ?? "";
        entry.Notes = notes ?? "";
        entry.Status = string.IsNullOrWhiteSpace(status) ? "Draft" : status;

        if (existing is null) lead.Proposals.Add(entry);

        return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "proposal" });
    }

    [HttpPost]
    public IActionResult DeleteProposal(string leadId, string number)
    {
        var lead = LeadStore.GetById(leadId);
        if (lead is not null)
        {
            var existing = lead.Proposals.FirstOrDefault(p => p.Number == number);
            if (existing is not null)
            {
                FinanceAttachmentService.Delete(existing.AttachmentPath);
                lead.Proposals.Remove(existing);
            }
        }
        return RedirectToAction("Details", new { id = leadId, tab = "finance", subtab = "proposal" });
    }

    private static decimal ParseDec(string? s) => decimal.TryParse(s, out var v) ? v : 0;

    /// <summary>Builds the Items/Services line-item list from the parallel form arrays posted by the Finance forms.</summary>
    private static List<FinanceLineItem> BuildLineItems(List<string>? names, List<string>? qtys, List<string>? rates)
    {
        var items = new List<FinanceLineItem>();
        if (names is null) return items;

        for (int i = 0; i < names.Count; i++)
        {
            var name = names[i];
            if (string.IsNullOrWhiteSpace(name)) continue;

            var qty = (qtys is not null && i < qtys.Count) ? qtys[i] : "";
            var rate = (rates is not null && i < rates.Count) ? rates[i] : "";
            var amount = ParseDec(qty) * ParseDec(rate);

            items.Add(new FinanceLineItem { ItemName = name, Quantity = qty, Rate = rate, Amount = amount.ToString("0.##") });
        }

        return items;
    }

    private IActionResult RedirectToReturnUrl(string? returnUrl, string fallbackAction)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(fallbackAction);
    }
}
