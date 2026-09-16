using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class TrackingModel(FleetDbContext db) : PageModel
{
    public List<LiveVehicle> LiveVehicles { get; set; } = [];
    public List<GeofenceZone> Geofences { get; set; } = [];
    public LiveVehicle? Selected { get; set; }

    public async Task OnGetAsync()
    {
        LiveVehicles = await db.LiveVehicles.Include(l => l.Vehicle).ThenInclude(v => v!.AssignedDriver).OrderByDescending(l => l.SpeedKph).ToListAsync();
        Geofences = await db.Geofences.ToListAsync();
        Selected = LiveVehicles.FirstOrDefault();
    }

    public async Task<IActionResult> OnPostSimulateAsync()
    {
        var live = await db.LiveVehicles.ToListAsync();
        var rng = Random.Shared;
        var headings = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        foreach (var lv in live)
        {
            if (lv.Status is "Offline") continue;
            lv.Left = Math.Clamp(lv.Left + rng.Next(-6, 7), 5, 92);
            lv.Top = Math.Clamp(lv.Top + rng.Next(-6, 7), 8, 88);
            lv.SpeedKph = lv.Status == "Idle" ? 0 : rng.Next(8, 70);
            lv.Heading = headings[rng.Next(headings.Length)];
            lv.LastUpdated = DateTime.UtcNow;
            if (lv.Status == "Active" && lv.SpeedKph > 60 && rng.Next(4) == 0) lv.Status = "Overspeeding";
            else if (lv.Status == "Overspeeding") lv.Status = "Active";
        }
        await db.SaveChangesAsync();
        return RedirectToPage();
    }
}
