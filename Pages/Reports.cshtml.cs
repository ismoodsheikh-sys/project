using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class ReportsModel(FleetDbContext db) : PageModel
{
    public List<WeeklyStat> WeeklyStats { get; set; } = [];
    public List<(string Category, double Km, string Color)> CategoryDistance { get; set; } = [];
    public List<Vehicle> WorstHealthVehicles { get; set; } = [];

    public List<(string Category, double Cost, string Color)> CategoryFuelCost { get; set; } = [];
    public List<(string Plate, double Cost, double Liters)> TopFuelVehicles { get; set; } = [];

    public List<(string Category, double Cost, int Count)> MaintenanceByCategory { get; set; } = [];
    public List<(string Plate, double Cost, int Count)> TopMaintenanceVehicles { get; set; } = [];

    public List<Driver> RankedDrivers { get; set; } = [];

    public async Task OnGetAsync()
    {
        WeeklyStats = await db.WeeklyStats.OrderBy(w => w.WeekStart).ToListAsync();

        var trips = await db.Trips.Include(t => t.Vehicle).Where(t => t.DistanceKm > 0).ToListAsync();
        var distByCat = trips.GroupBy(t => ChartPalette.BucketCategory(t.Vehicle!.Category)).ToDictionary(g => g.Key, g => g.Sum(t => t.DistanceKm));
        CategoryDistance = ChartPalette.CategoryOrder.Select(c => (c, distByCat.GetValueOrDefault(c, 0), ChartPalette.ColorFor(c))).Where(c => c.Item2 > 0).ToList();

        WorstHealthVehicles = await db.Vehicles.OrderBy(v => v.HealthScore).Take(6).ToListAsync();

        var fuel = await db.FuelRecords.Include(f => f.Vehicle).ToListAsync();
        var fuelByCat = fuel.GroupBy(f => ChartPalette.BucketCategory(f.Vehicle!.Category)).ToDictionary(g => g.Key, g => g.Sum(f => f.TotalCost));
        CategoryFuelCost = ChartPalette.CategoryOrder.Select(c => (c, fuelByCat.GetValueOrDefault(c, 0), ChartPalette.ColorFor(c))).ToList();
        TopFuelVehicles = fuel.GroupBy(f => f.Vehicle!.PlateNumber)
            .Select(g => (Plate: g.Key, Cost: g.Sum(f => f.TotalCost), Liters: g.Sum(f => f.Liters)))
            .OrderByDescending(g => g.Cost).Take(5).ToList();

        var maint = await db.MaintenanceRecords.Include(m => m.Vehicle).ToListAsync();
        MaintenanceByCategory = maint.GroupBy(m => m.Category)
            .Select(g => (Category: g.Key, Cost: g.Sum(m => m.Cost), Count: g.Count()))
            .OrderByDescending(g => g.Cost).ToList();
        TopMaintenanceVehicles = maint.GroupBy(m => m.Vehicle!.PlateNumber)
            .Select(g => (Plate: g.Key, Cost: g.Sum(m => m.Cost), Count: g.Count()))
            .OrderByDescending(g => g.Cost).Take(5).ToList();

        RankedDrivers = await db.Drivers.OrderByDescending(d => d.SafetyScore).ToListAsync();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        await OnGetAsync();

        var sheets = new (string Name, IEnumerable<string> Headers, IEnumerable<IEnumerable<object?>> Rows)[]
        {
            ("Weekly fleet stats", ["Week", "Distance (km)", "Fuel Cost", "Maintenance Cost", "Utilization %"],
                WeeklyStats.Select(w => new object?[] { w.WeekLabel, w.DistanceKm, w.FuelCost, w.MaintenanceCost, w.UtilizationPct })),
            ("Distance by category", ["Category", "Distance (km)"],
                CategoryDistance.Select(c => new object?[] { c.Category, c.Km })),
            ("Top fuel spend by vehicle", ["Plate", "Cost", "Liters"],
                TopFuelVehicles.Select(t => new object?[] { t.Plate, t.Cost, t.Liters })),
            ("Top maintenance spend", ["Plate", "Cost", "Records"],
                TopMaintenanceVehicles.Select(t => new object?[] { t.Plate, t.Cost, t.Count })),
            ("Driver safety leaderboard", ["Driver", "Safety Score", "Rating", "Trips Completed"],
                RankedDrivers.Select(d => new object?[] { d.Name, d.SafetyScore, d.Rating, d.TripsCompleted })),
        };

        var excel = ExcelExport.BuildMultiSheet(sheets);
        return File(excel, ExcelExport.ContentType, $"fleet-report-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
