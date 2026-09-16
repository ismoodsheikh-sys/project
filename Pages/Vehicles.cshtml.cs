using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class VehiclesModel(FleetDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    public List<Vehicle> Vehicles { get; set; } = [];
    public List<Driver> AllDrivers { get; set; } = [];
    public Dictionary<int, List<MaintenanceRecord>> MaintenanceByVehicle { get; set; } = [];
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int MaintenanceCount { get; set; }
    public int ExpiringSoonCount { get; set; }

    private IQueryable<Vehicle> BuildQuery()
    {
        var query = db.Vehicles.Include(v => v.AssignedDriver).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(v => v.PlateNumber.Contains(s) || v.Make.Contains(s) || v.Model.Contains(s) || v.Vin.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
        {
            query = query.Where(v => v.Status == StatusFilter);
        }
        if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "All")
        {
            query = query.Where(v => v.Category == CategoryFilter);
        }
        return query;
    }

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Vehicles = await BuildQuery().OrderBy(v => v.PlateNumber).ToListAsync();
        AllDrivers = await db.Drivers.OrderBy(d => d.Name).ToListAsync();

        var ids = Vehicles.Select(v => v.Id).ToList();
        MaintenanceByVehicle = (await db.MaintenanceRecords.Where(m => ids.Contains(m.VehicleId)).OrderByDescending(m => m.Date).ToListAsync())
            .GroupBy(m => m.VehicleId).ToDictionary(g => g.Key, g => g.ToList());

        TotalCount = await db.Vehicles.CountAsync();
        ActiveCount = await db.Vehicles.CountAsync(v => v.Status == "Active");
        MaintenanceCount = await db.Vehicles.CountAsync(v => v.Status == "Maintenance");
        ExpiringSoonCount = await db.Vehicles.CountAsync(v =>
            (v.InsuranceExpiry.DayNumber - today.DayNumber >= 0 && v.InsuranceExpiry.DayNumber - today.DayNumber <= 30) ||
            (v.RegistrationExpiry.DayNumber - today.DayNumber >= 0 && v.RegistrationExpiry.DayNumber - today.DayNumber <= 30));
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var vehicles = await BuildQuery().OrderBy(v => v.PlateNumber).ToListAsync();
        var excel = ExcelExport.Build(
            ["Plate", "Make", "Model", "Year", "Category", "VIN", "Status", "Depot", "Assigned Driver", "Fuel Type", "Odometer (km)", "Health Score", "Insurance Expiry", "Registration Expiry", "Next Service Due"],
            vehicles.Select(v => new object?[] { v.PlateNumber, v.Make, v.Model, v.Year, v.Category, v.Vin, v.Status, v.Depot, v.AssignedDriver?.Name, v.FuelType, v.OdometerKm, v.HealthScore, v.InsuranceExpiry, v.RegistrationExpiry, v.NextServiceDue }));
        return File(excel, ExcelExport.ContentType, $"vehicles-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> OnPostAddAsync(
        string plateNumber, string make, string model, int year, string category, string vin,
        string fuelType, string depot, string gpsDeviceId, DateOnly registrationExpiry, DateOnly insuranceExpiry)
    {
        db.Vehicles.Add(new Vehicle
        {
            PlateNumber = plateNumber, Make = make, Model = model, Year = year, Category = category, Vin = vin,
            FuelType = fuelType, Depot = depot, GpsDeviceId = gpsDeviceId, Status = "Active", HealthScore = 100,
            RegistrationExpiry = registrationExpiry, InsuranceExpiry = insuranceExpiry,
            LastServiceDate = DateOnly.FromDateTime(DateTime.UtcNow), NextServiceDue = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(3),
        });
        db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Registered new vehicle", Target = plateNumber, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
        await db.SaveChangesAsync();
        return RedirectToPage(new { Search, StatusFilter, CategoryFilter });
    }

    public async Task<IActionResult> OnPostEditAsync(
        int id, string make, string model, int year, string category, string fuelType, string status, string depot,
        int odometerKm, int healthScore, int? assignedDriverId, DateOnly registrationExpiry, DateOnly insuranceExpiry, DateOnly nextServiceDue)
    {
        var vehicle = await db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();

        vehicle.Make = make; vehicle.Model = model; vehicle.Year = year; vehicle.Category = category;
        vehicle.FuelType = fuelType; vehicle.Status = status; vehicle.Depot = depot;
        vehicle.OdometerKm = odometerKm; vehicle.HealthScore = healthScore;
        vehicle.AssignedDriverId = assignedDriverId;
        vehicle.RegistrationExpiry = registrationExpiry; vehicle.InsuranceExpiry = insuranceExpiry; vehicle.NextServiceDue = nextServiceDue;

        db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Updated vehicle record", Target = vehicle.PlateNumber, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
        await db.SaveChangesAsync();
        return RedirectToPage(new { Search, StatusFilter, CategoryFilter });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var vehicle = await db.Vehicles.FindAsync(id);
        if (vehicle is not null)
        {
            db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Removed vehicle record", Target = vehicle.PlateNumber, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
            db.Vehicles.Remove(vehicle);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { Search, StatusFilter, CategoryFilter });
    }
}
