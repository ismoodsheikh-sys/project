using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class DashboardModel(FleetDbContext db) : PageModel
{
    public int TotalVehicles { get; set; }
    public int ActiveVehicles { get; set; }
    public int MaintenanceVehicles { get; set; }
    public int IdleVehicles { get; set; }
    public int OutOfServiceVehicles { get; set; }
    public int TotalDrivers { get; set; }
    public int OnTripDrivers { get; set; }
    public int OpenAlerts { get; set; }
    public double FleetUtilizationPct { get; set; }
    public double TodayDistanceKm { get; set; }
    public double MonthFuelCost { get; set; }

    public List<WeeklyStat> WeeklyStats { get; set; } = [];
    public List<Vehicle> AttentionVehicles { get; set; } = [];
    public List<AppNotification> Notifications { get; set; } = [];
    public List<ActivityLog> RecentActivity { get; set; } = [];
    public List<(string Category, double Km, string Color)> CategoryDistance { get; set; } = [];

    public async Task OnGetAsync()
    {
        TotalVehicles = await db.Vehicles.CountAsync();
        ActiveVehicles = await db.Vehicles.CountAsync(v => v.Status == "Active");
        MaintenanceVehicles = await db.Vehicles.CountAsync(v => v.Status == "Maintenance");
        IdleVehicles = await db.Vehicles.CountAsync(v => v.Status == "Idle");
        OutOfServiceVehicles = await db.Vehicles.CountAsync(v => v.Status == "Out of Service");
        TotalDrivers = await db.Drivers.CountAsync();
        OnTripDrivers = await db.Drivers.CountAsync(d => d.Status == "On Trip");
        OpenAlerts = await db.Alerts.CountAsync(a => a.Status == "New");
        MonthFuelCost = await db.FuelRecords.SumAsync(f => f.Liters * f.CostPerLiter) * 3.1;

        WeeklyStats = await db.WeeklyStats.OrderBy(w => w.WeekStart).ToListAsync();
        FleetUtilizationPct = WeeklyStats.Count > 0 ? WeeklyStats[^1].UtilizationPct : 0;
        TodayDistanceKm = 1842.6;

        AttentionVehicles = await db.Vehicles
            .Where(v => v.Status == "Maintenance" || v.Status == "Out of Service" || v.HealthScore < 65)
            .OrderBy(v => v.HealthScore).Take(5).ToListAsync();

        Notifications = await db.Notifications.OrderByDescending(n => n.CreatedAt).Take(6).ToListAsync();
        RecentActivity = await db.ActivityLog.OrderByDescending(a => a.OccurredAt).Take(7).ToListAsync();

        var rawTrips = await db.Trips.Include(t => t.Vehicle).Where(t => t.DistanceKm > 0).ToListAsync();
        var grouped = rawTrips.GroupBy(t => ChartPalette.BucketCategory(t.Vehicle!.Category))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.DistanceKm));
        CategoryDistance = ChartPalette.CategoryOrder
            .Select(c => (Category: c, Km: grouped.GetValueOrDefault(c, 0), Color: ChartPalette.ColorFor(c)))
            .Where(c => c.Km > 0)
            .ToList();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var vehicles = await db.Vehicles.Include(v => v.AssignedDriver).OrderBy(v => v.PlateNumber).ToListAsync();
        var excel = ExcelExport.Build(
            ["Plate", "Make", "Model", "Status", "Category", "Depot", "Assigned Driver", "Health Score", "Odometer (km)", "Next Service Due"],
            vehicles.Select(v => new object?[] { v.PlateNumber, v.Make, v.Model, v.Status, v.Category, v.Depot, v.AssignedDriver?.Name, v.HealthScore, v.OdometerKm, v.NextServiceDue }));
        return File(excel, ExcelExport.ContentType, $"fleet-overview-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
