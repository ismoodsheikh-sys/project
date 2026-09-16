using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class MaintenanceModel(FleetDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    public List<MaintenanceRecord> Records { get; set; } = [];
    public List<Vehicle> AllVehicles { get; set; } = [];
    public List<Vehicle> UpcomingService { get; set; } = [];
    public int OverdueCount { get; set; }
    public int InProgressCount { get; set; }
    public int ScheduledCount { get; set; }
    public double MtdCost { get; set; }

    private IQueryable<MaintenanceRecord> BuildQuery()
    {
        var query = db.MaintenanceRecords.Include(m => m.Vehicle).AsQueryable();
        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
        {
            query = query.Where(m => m.Status == StatusFilter);
        }
        if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "All")
        {
            query = query.Where(m => m.Category == CategoryFilter);
        }
        return query;
    }

    public async Task OnGetAsync()
    {
        Records = await BuildQuery().OrderByDescending(m => m.Date).ToListAsync();

        AllVehicles = await db.Vehicles.OrderBy(v => v.PlateNumber).ToListAsync();
        UpcomingService = await db.Vehicles.OrderBy(v => v.NextServiceDue).Take(4).ToListAsync();

        OverdueCount = await db.MaintenanceRecords.CountAsync(m => m.Status == "Overdue");
        InProgressCount = await db.MaintenanceRecords.CountAsync(m => m.Status == "In Progress");
        ScheduledCount = await db.MaintenanceRecords.CountAsync(m => m.Status == "Scheduled");
        var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        MtdCost = await db.MaintenanceRecords.Where(m => m.Date >= monthStart).SumAsync(m => (double?)m.Cost) ?? 0;
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var records = await BuildQuery().OrderByDescending(m => m.Date).ToListAsync();
        var excel = ExcelExport.Build(
            ["Vehicle", "Service Type", "Category", "Status", "Date", "Due Date", "Workshop", "Cost", "Parts Replaced"],
            records.Select(m => new object?[] { m.Vehicle?.PlateNumber, m.ServiceType, m.Category, m.Status, m.Date, m.DueDate, m.Workshop, m.Cost, m.PartsReplaced }));
        return File(excel, ExcelExport.ContentType, $"maintenance-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> OnPostAddAsync(int vehicleId, string serviceType, string category, string workshop, DateOnly date, double cost, string? notes)
    {
        db.MaintenanceRecords.Add(new MaintenanceRecord
        {
            VehicleId = vehicleId, ServiceType = serviceType, Category = category, Workshop = workshop,
            Date = date, Cost = cost, Status = "Scheduled", PartsReplaced = notes ?? "—",
        });
        await db.SaveChangesAsync();
        return RedirectToPage(new { StatusFilter, CategoryFilter });
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int id, string status)
    {
        var record = await db.MaintenanceRecords.Include(m => m.Vehicle).FirstOrDefaultAsync(m => m.Id == id);
        if (record is not null)
        {
            record.Status = status;
            if (status == "Completed" && record.Vehicle is not null)
            {
                record.Vehicle.LastServiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
                if (record.Vehicle.Status == "Maintenance") record.Vehicle.Status = "Active";
            }
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { StatusFilter, CategoryFilter });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var record = await db.MaintenanceRecords.FindAsync(id);
        if (record is not null)
        {
            db.MaintenanceRecords.Remove(record);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { StatusFilter, CategoryFilter });
    }
}
