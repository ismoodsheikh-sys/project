using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class TripsModel(FleetDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public List<Trip> Trips { get; set; } = [];
    public List<Vehicle> ActiveVehicles { get; set; } = [];
    public List<Driver> AvailableDrivers { get; set; } = [];
    public int InProgressCount { get; set; }
    public int ScheduledCount { get; set; }
    public int CompletedYesterdayCount { get; set; }
    public double DistanceInProgress { get; set; }

    private IQueryable<Trip> BuildQuery()
    {
        var query = db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).AsQueryable();
        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
        {
            query = query.Where(t => t.Status == StatusFilter);
        }
        return query;
    }

    public async Task OnGetAsync()
    {
        Trips = await BuildQuery().OrderByDescending(t => t.StartTime).ToListAsync();

        ActiveVehicles = await db.Vehicles.Where(v => v.Status == "Active").OrderBy(v => v.PlateNumber).ToListAsync();
        AvailableDrivers = await db.Drivers.Where(d => d.Status != "Suspended").OrderBy(d => d.Name).ToListAsync();

        InProgressCount = await db.Trips.CountAsync(t => t.Status == "In Progress");
        ScheduledCount = await db.Trips.CountAsync(t => t.Status == "Scheduled");
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        CompletedYesterdayCount = await db.Trips.CountAsync(t => t.Status == "Completed" && t.StartTime.Date == yesterday);
        DistanceInProgress = await db.Trips.Where(t => t.Status == "In Progress").SumAsync(t => (double?)t.DistanceKm) ?? 0;
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var trips = await BuildQuery().OrderByDescending(t => t.StartTime).ToListAsync();
        var excel = ExcelExport.Build(
            ["Trip Code", "Vehicle", "Driver", "Origin", "Destination", "Start", "End", "Status", "Distance (km)", "Fuel Used (L)", "Purpose"],
            trips.Select(t => new object?[] { t.TripCode, t.Vehicle?.PlateNumber, t.Driver?.Name, t.Origin, t.Destination, t.StartTime, t.EndTime, t.Status, t.DistanceKm, t.FuelUsedL, t.Purpose }));
        return File(excel, ExcelExport.ContentType, $"trips-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> OnPostAddAsync(int vehicleId, int driverId, string origin, string destination, DateTime startTime, string purpose, string? notes)
    {
        var count = await db.Trips.CountAsync() + 5501;
        db.Trips.Add(new Trip
        {
            TripCode = $"TRP-{count}", VehicleId = vehicleId, DriverId = driverId, Origin = origin, Destination = destination,
            StartTime = startTime, Purpose = purpose, Notes = notes, Status = startTime <= DateTime.UtcNow ? "In Progress" : "Scheduled",
        });
        await db.SaveChangesAsync();
        return RedirectToPage(new { StatusFilter });
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, string status)
    {
        var trip = await db.Trips.FindAsync(id);
        if (trip is not null)
        {
            trip.Status = status;
            if (status == "Completed" && trip.EndTime is null) trip.EndTime = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { StatusFilter });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var trip = await db.Trips.FindAsync(id);
        if (trip is not null)
        {
            db.Trips.Remove(trip);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { StatusFilter });
    }
}
