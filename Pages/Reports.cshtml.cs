using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class ReportsModel(FleetDbContext db) : PageModel
{
    private static readonly Dictionary<string, string> PeriodLabels = new()
    {
        ["day"] = "Today",
        ["week"] = "This week",
        ["month"] = "This month",
        ["quarter"] = "This quarter",
    };

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "quarter";

    public string PeriodLabel => PeriodLabels.GetValueOrDefault(Period, "This quarter");
    public string PeriodRangeLabel { get; set; } = "";

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
        if (!PeriodLabels.ContainsKey(Period)) Period = "quarter";

        WeeklyStats = await db.WeeklyStats.OrderBy(w => w.WeekStart).ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (periodStart, periodEnd) = GetPeriodRange(today, Period);
        PeriodRangeLabel = periodStart == periodEnd
            ? periodStart.ToString("MMM d, yyyy")
            : $"{periodStart:MMM d} – {periodEnd:MMM d, yyyy}";

        var trips = await db.Trips.Include(t => t.Vehicle)
            .Where(t => t.DistanceKm > 0 && t.StartTime >= periodStart.ToDateTime(TimeOnly.MinValue) && t.StartTime < periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .ToListAsync();
        var distByCat = trips.GroupBy(t => ChartPalette.BucketCategory(t.Vehicle!.Category)).ToDictionary(g => g.Key, g => g.Sum(t => t.DistanceKm));
        CategoryDistance = ChartPalette.CategoryOrder.Select(c => (c, distByCat.GetValueOrDefault(c, 0), ChartPalette.ColorFor(c))).Where(c => c.Item2 > 0).ToList();

        WorstHealthVehicles = await db.Vehicles.OrderBy(v => v.HealthScore).Take(6).ToListAsync();

        var fuel = await db.FuelRecords.Include(f => f.Vehicle).Where(f => f.Date >= periodStart && f.Date <= periodEnd).ToListAsync();
        var fuelByCat = fuel.GroupBy(f => ChartPalette.BucketCategory(f.Vehicle!.Category)).ToDictionary(g => g.Key, g => g.Sum(f => f.TotalCost));
        CategoryFuelCost = ChartPalette.CategoryOrder.Select(c => (c, fuelByCat.GetValueOrDefault(c, 0), ChartPalette.ColorFor(c))).ToList();
        TopFuelVehicles = fuel.GroupBy(f => f.Vehicle!.PlateNumber)
            .Select(g => (Plate: g.Key, Cost: g.Sum(f => f.TotalCost), Liters: g.Sum(f => f.Liters)))
            .OrderByDescending(g => g.Cost).Take(5).ToList();

        var maint = await db.MaintenanceRecords.Include(m => m.Vehicle).Where(m => m.Date >= periodStart && m.Date <= periodEnd).ToListAsync();
        MaintenanceByCategory = maint.GroupBy(m => m.Category)
            .Select(g => (Category: g.Key, Cost: g.Sum(m => m.Cost), Count: g.Count()))
            .OrderByDescending(g => g.Cost).ToList();
        TopMaintenanceVehicles = maint.GroupBy(m => m.Vehicle!.PlateNumber)
            .Select(g => (Plate: g.Key, Cost: g.Sum(m => m.Cost), Count: g.Count()))
            .OrderByDescending(g => g.Cost).Take(5).ToList();

        RankedDrivers = await db.Drivers.OrderByDescending(d => d.SafetyScore).ToListAsync();
    }

    private static (DateOnly Start, DateOnly End) GetPeriodRange(DateOnly today, string period)
    {
        switch (period)
        {
            case "day":
                return (today, today);
            case "week":
                var weekOffset = ((int)today.DayOfWeek + 6) % 7;
                var weekStart = today.AddDays(-weekOffset);
                return (weekStart, weekStart.AddDays(6));
            case "month":
                var monthStart = new DateOnly(today.Year, today.Month, 1);
                return (monthStart, monthStart.AddMonths(1).AddDays(-1));
            default:
                var quarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
                var quarterStart = new DateOnly(today.Year, quarterStartMonth, 1);
                return (quarterStart, quarterStart.AddMonths(3).AddDays(-1));
        }
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
        return File(excel, ExcelExport.ContentType, $"fleet-report-{Period}-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
