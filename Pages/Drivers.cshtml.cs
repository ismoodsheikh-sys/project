using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class DriversModel(FleetDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? DepotFilter { get; set; }

    public List<Driver> Drivers { get; set; } = [];
    public Dictionary<int, Vehicle> AssignedVehicleByDriver { get; set; } = [];
    public Dictionary<int, List<Trip>> RecentTripsByDriver { get; set; } = [];
    public int TotalCount { get; set; }
    public int OnTripCount { get; set; }
    public double AvgSafety { get; set; }
    public int ExpiringLicenses { get; set; }
    public string? DeleteError { get; set; }

    private IQueryable<Driver> BuildQuery()
    {
        var query = db.Drivers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(d => d.Name.Contains(s) || d.LicenseNumber.Contains(s) || d.Phone.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
        {
            query = query.Where(d => d.Status == StatusFilter);
        }
        if (!string.IsNullOrWhiteSpace(DepotFilter) && DepotFilter != "All")
        {
            query = query.Where(d => d.Depot == DepotFilter);
        }
        return query;
    }

    public async Task OnGetAsync(string? deleteError)
    {
        DeleteError = deleteError is { Length: > 0 } name ? $"Can't remove {name} — they have trip history on record. Set their status to Suspended instead if they should no longer drive." : null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Drivers = await BuildQuery().OrderBy(d => d.Name).ToListAsync();

        var vehicles = await db.Vehicles.Where(v => v.AssignedDriverId != null).ToListAsync();
        AssignedVehicleByDriver = vehicles.Where(v => v.AssignedDriverId.HasValue).ToDictionary(v => v.AssignedDriverId!.Value);

        var driverNames = Drivers.Select(d => d.Name).ToList();
        RecentTripsByDriver = (await db.Trips.Include(t => t.Driver).Where(t => driverNames.Contains(t.Driver!.Name))
                .OrderByDescending(t => t.StartTime).ToListAsync())
            .GroupBy(t => t.DriverId).ToDictionary(g => g.Key, g => g.Take(6).ToList());

        TotalCount = await db.Drivers.CountAsync();
        OnTripCount = await db.Drivers.CountAsync(d => d.Status == "On Trip");
        AvgSafety = await db.Drivers.AverageAsync(d => (double?)d.SafetyScore) ?? 0;
        ExpiringLicenses = await db.Drivers.CountAsync(d => d.LicenseExpiry.DayNumber - today.DayNumber >= 0 && d.LicenseExpiry.DayNumber - today.DayNumber <= 30);
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var drivers = await BuildQuery().OrderBy(d => d.Name).ToListAsync();
        var excel = ExcelExport.Build(
            ["Name", "License Number", "License Class", "License Expiry", "Phone", "Email", "Status", "Depot", "Years Experience", "Safety Score", "Rating", "Trips Completed", "Total Distance (km)"],
            drivers.Select(d => new object?[] { d.Name, d.LicenseNumber, d.LicenseClass, d.LicenseExpiry, d.Phone, d.Email, d.Status, d.Depot, d.YearsExperience, d.SafetyScore, d.Rating, d.TripsCompleted, d.TotalDistanceKm }));
        return File(excel, ExcelExport.ContentType, $"drivers-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> OnPostAddAsync(string name, string licenseNumber, string licenseClass, string phone, string email, string depot, DateOnly licenseExpiry)
    {
        var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p[0])).ToUpperInvariant();
        db.Drivers.Add(new Driver
        {
            Name = name, Initials = initials.Length > 2 ? initials[..2] : initials, LicenseNumber = licenseNumber,
            LicenseClass = licenseClass, Phone = phone, Email = email, Depot = depot, LicenseExpiry = licenseExpiry,
            Status = "Active", SafetyScore = 85, Rating = 4.5,
        });
        db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Added new driver", Target = name, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
        await db.SaveChangesAsync();
        return RedirectToPage(new { Search, StatusFilter, DepotFilter });
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string phone, string email, string status, string depot, int yearsExperience, double safetyScore, DateOnly licenseExpiry)
    {
        var driver = await db.Drivers.FindAsync(id);
        if (driver is null) return NotFound();

        driver.Phone = phone; driver.Email = email; driver.Status = status; driver.Depot = depot;
        driver.YearsExperience = yearsExperience; driver.SafetyScore = Math.Clamp(safetyScore, 0, 100); driver.LicenseExpiry = licenseExpiry;

        db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Updated driver record", Target = driver.Name, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
        await db.SaveChangesAsync();
        return RedirectToPage(new { Search, StatusFilter, DepotFilter });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var driver = await db.Drivers.FindAsync(id);
        if (driver is not null)
        {
            var hasTrips = await db.Trips.AnyAsync(t => t.DriverId == id);
            if (hasTrips)
            {
                return RedirectToPage(new { Search, StatusFilter, DepotFilter, deleteError = driver.Name });
            }

            db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Removed driver record", Target = driver.Name, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
            db.Drivers.Remove(driver);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { Search, StatusFilter, DepotFilter });
    }
}
