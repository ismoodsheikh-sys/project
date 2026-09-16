using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class UsersModel(FleetDbContext db) : PageModel
{
    public List<AppUser> Users { get; set; } = [];
    public List<AuditLogItem> AuditLog { get; set; } = [];
    public string? RemoveError { get; set; }
    public int ActiveCount { get; set; }
    public int InvitedCount { get; set; }

    public async Task OnGetAsync(string? removeError)
    {
        Users = await db.Users.OrderBy(u => u.Name).ToListAsync();
        AuditLog = await db.AuditLog.OrderByDescending(a => a.OccurredAt).Take(20).ToListAsync();
        ActiveCount = Users.Count(u => u.Status == "Active");
        InvitedCount = Users.Count(u => u.Status == "Invited");
        RemoveError = removeError == "self" ? "You can't remove your own account while signed in to it." : null;
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var users = await db.Users.OrderBy(u => u.Name).ToListAsync();
        var excel = ExcelExport.Build(
            ["Name", "Email", "Role", "Department", "Status", "Last Active"],
            users.Select(u => new object?[] { u.Name, u.Email, u.Role, u.Department, u.Status, u.LastActiveAt }));
        return File(excel, ExcelExport.ContentType, $"users-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> OnPostInviteAsync(string name, string email, string role, string department)
    {
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return RedirectToPage();
        }
        var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p[0])).ToUpperInvariant();
        var user = new AppUser { Name = name, Initials = initials.Length > 2 ? initials[..2] : initials, Email = email, Role = role, Department = department, Status = "Invited" };
        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, OtpHelper.GenerateCode() + "Aa1!");
        db.Users.Add(user);
        db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Invited new user", Target = email, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetRoleAsync(int id, string role)
    {
        var user = await db.Users.FindAsync(id);
        if (user is not null)
        {
            user.Role = role;
            db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = $"Changed role to {role}", Target = user.Email, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id.ToString())
        {
            return RedirectToPage(new { removeError = "self" });
        }

        var user = await db.Users.FindAsync(id);
        if (user is not null)
        {
            db.AuditLog.Add(new AuditLogItem { Actor = User.Identity?.Name ?? "System", Action = "Removed user account", Target = user.Email, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
