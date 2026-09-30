using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;

namespace VehicleFleetMS.Pages;

public class AlertsModel(FleetDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? SeverityFilter { get; set; }

    public List<Models.AlertItem> Alerts { get; set; } = [];
    public int CriticalCount { get; set; }
    public int SeriousCount { get; set; }
    public int WarningCount { get; set; }
    public int ResolvedCount { get; set; }

    public async Task OnGetAsync()
    {
        var query = db.Alerts.Include(a => a.Vehicle).Include(a => a.Driver).AsQueryable();
        if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter != "All")
        {
            query = query.Where(a => a.Type == TypeFilter);
        }
        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
        {
            query = query.Where(a => a.Status == StatusFilter);
        }
        if (!string.IsNullOrWhiteSpace(SeverityFilter) && SeverityFilter != "All")
        {
            query = query.Where(a => a.Severity == SeverityFilter);
        }
        Alerts = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

        CriticalCount = await db.Alerts.CountAsync(a => a.Severity == "Critical" && a.Status != "Resolved");
        SeriousCount = await db.Alerts.CountAsync(a => a.Severity == "Serious" && a.Status != "Resolved");
        WarningCount = await db.Alerts.CountAsync(a => a.Severity == "Warning" && a.Status != "Resolved");
        ResolvedCount = await db.Alerts.CountAsync(a => a.Status == "Resolved");
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, string status)
    {
        var alert = await db.Alerts.FindAsync(id);
        if (alert is not null)
        {
            alert.Status = status;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { TypeFilter, StatusFilter, SeverityFilter });
    }

    public async Task<IActionResult> OnPostAcknowledgeAllAsync()
    {
        var newAlerts = await db.Alerts.Where(a => a.Status == "New").ToListAsync();
        foreach (var a in newAlerts) a.Status = "Acknowledged";
        await db.SaveChangesAsync();
        return RedirectToPage(new { TypeFilter, StatusFilter, SeverityFilter });
    }
}
