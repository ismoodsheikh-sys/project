using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class FuelModel(FleetDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }

    public List<FuelRecord> Records { get; set; } = [];
    public List<Vehicle> AllVehicles { get; set; } = [];
    public List<WeeklyStat> WeeklyStats { get; set; } = [];
    public double TotalLiters { get; set; }
    public double AvgCostPerLiter { get; set; }
    public double MonthFuelCost { get; set; }
    public List<(string Type, double Liters)> ByType { get; set; } = [];
    public AppSettings Settings { get; set; } = new();

    private IQueryable<FuelRecord> BuildQuery()
    {
        var query = db.FuelRecords.Include(f => f.Vehicle).Include(f => f.Driver).AsQueryable();
        if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter != "All")
        {
            query = query.Where(f => f.FuelType == TypeFilter);
        }
        return query;
    }

    public async Task OnGetAsync()
    {
        Records = await BuildQuery().OrderByDescending(f => f.Date).ToListAsync();
        AllVehicles = await db.Vehicles.OrderBy(v => v.PlateNumber).ToListAsync();
        WeeklyStats = await db.WeeklyStats.OrderBy(w => w.WeekStart).ToListAsync();
        Settings = await db.Settings.FirstOrDefaultAsync() ?? new AppSettings();

        var all = await db.FuelRecords.ToListAsync();
        TotalLiters = all.Sum(f => f.Liters);
        AvgCostPerLiter = all.Count > 0 ? all.Average(f => f.CostPerLiter) : 0;
        MonthFuelCost = all.Sum(f => f.TotalCost) * 3.1;
        ByType = all.GroupBy(f => f.FuelType).Select(g => (Type: g.Key, Liters: g.Sum(f => f.Liters))).OrderByDescending(g => g.Liters).ToList();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var records = await BuildQuery().OrderByDescending(f => f.Date).ToListAsync();
        var excel = ExcelExport.Build(
            ["Vehicle", "Driver", "Date", "Fuel Type", "Liters", "Cost per Liter", "Total Cost", "Odometer", "Station", "Payment Method"],
            records.Select(f => new object?[] { f.Vehicle?.PlateNumber, f.Driver?.Name, f.Date, f.FuelType, f.Liters, f.CostPerLiter, f.TotalCost, f.Odometer, f.Station, f.PaymentMethod }));
        return File(excel, ExcelExport.ContentType, $"fuel-records-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public static string InvoiceNumber(int id) => $"INV-F{id:D6}";

    public async Task<IActionResult> OnGetInvoicePdfAsync(int id)
    {
        var record = await db.FuelRecords.Include(f => f.Vehicle).Include(f => f.Driver).FirstOrDefaultAsync(f => f.Id == id);
        if (record is null) return NotFound();
        var settings = await db.Settings.FirstOrDefaultAsync() ?? new AppSettings();
        var pdf = InvoicePdf.Build(settings, record, InvoiceNumber(record.Id));
        return File(pdf, "application/pdf", $"{InvoiceNumber(record.Id)}.pdf");
    }

    public async Task<IActionResult> OnGetInvoiceExcelAsync(int id)
    {
        var record = await db.FuelRecords.Include(f => f.Vehicle).Include(f => f.Driver).FirstOrDefaultAsync(f => f.Id == id);
        if (record is null) return NotFound();
        var settings = await db.Settings.FirstOrDefaultAsync() ?? new AppSettings();
        var excel = InvoiceExcel.Build(settings, record, InvoiceNumber(record.Id));
        return File(excel, ExcelExport.ContentType, $"{InvoiceNumber(record.Id)}.xlsx");
    }

    public async Task<IActionResult> OnPostAddAsync(int vehicleId, string fuelType, DateOnly date, double liters, double costPerLiter, int odometer, string station, string paymentMethod)
    {
        db.FuelRecords.Add(new FuelRecord
        {
            VehicleId = vehicleId, FuelType = fuelType, Date = date, Liters = liters, CostPerLiter = costPerLiter,
            Odometer = odometer, Station = station, PaymentMethod = paymentMethod,
        });
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        if (vehicle is not null && odometer > vehicle.OdometerKm) vehicle.OdometerKm = odometer;
        await db.SaveChangesAsync();
        return RedirectToPage(new { TypeFilter });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var record = await db.FuelRecords.FindAsync(id);
        if (record is not null)
        {
            db.FuelRecords.Remove(record);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { TypeFilter });
    }
}
