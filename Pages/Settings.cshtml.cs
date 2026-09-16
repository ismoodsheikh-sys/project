using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class SettingsModel(FleetDbContext db, IWebHostEnvironment env) : PageModel
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp", "image/svg+xml",
    };

    public AppSettings Settings { get; set; } = new();
    public string? Saved { get; set; }
    public AppUser? CurrentUser { get; set; }
    public string? AccountError { get; set; }
    public string? PasswordError { get; set; }

    public async Task<IActionResult> OnGetAsync(string? saved)
    {
        CurrentUser = await GetCurrentUserAsync();
        if (CurrentUser is null) return await SignOutStaleSessionAsync();

        Settings = await GetOrCreateAsync();
        Saved = saved;
        return Page();
    }

    public async Task<IActionResult> OnPostAccountAsync(string name, string username, string email, string phone)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return await SignOutStaleSessionAsync();

        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
        {
            return await ReloadWithAccountErrorAsync("That email is already in use by another account.");
        }
        if (await db.Users.AnyAsync(u => u.Username == username && u.Id != user.Id))
        {
            return await ReloadWithAccountErrorAsync("That username is already taken.");
        }

        user.Name = name;
        user.Username = username;
        user.Email = email;
        user.Phone = phone;
        var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p[0])).ToUpperInvariant();
        user.Initials = initials.Length > 2 ? initials[..2] : initials;
        db.AuditLog.Add(new AuditLogItem { Actor = user.Name, Action = "Updated own profile", Target = user.Email, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToPage(new { saved = "account", tab = "security" });
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return await SignOutStaleSessionAsync();

        var hasher = new PasswordHasher<AppUser>();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            return await ReloadWithPasswordErrorAsync("Current password is incorrect.");
        }
        if (newPassword.Length < 8)
        {
            return await ReloadWithPasswordErrorAsync("New password must be at least 8 characters.");
        }
        if (newPassword != confirmPassword)
        {
            return await ReloadWithPasswordErrorAsync("New password and confirmation don't match.");
        }

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        db.AuditLog.Add(new AuditLogItem { Actor = user.Name, Action = "Changed own password", Target = user.Email, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "—" });
        await db.SaveChangesAsync();

        return RedirectToPage(new { saved = "password", tab = "security" });
    }

    public async Task<IActionResult> OnPostUploadAvatarAsync(IFormFile? avatarFile)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return await SignOutStaleSessionAsync();

        if (avatarFile is null || avatarFile.Length == 0)
        {
            return await ReloadWithAccountErrorAsync("Choose an image file to upload.");
        }
        if (avatarFile.Length > 2 * 1024 * 1024)
        {
            return await ReloadWithAccountErrorAsync("Photo must be 2MB or smaller.");
        }
        if (!AllowedImageTypes.Contains(avatarFile.ContentType))
        {
            return await ReloadWithAccountErrorAsync("Photo must be a PNG, JPEG, WEBP, or SVG image.");
        }

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        if (!string.IsNullOrEmpty(user.PhotoPath))
        {
            var previous = Path.Combine(env.WebRootPath, user.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(previous)) System.IO.File.Delete(previous);
        }

        var ext = Path.GetExtension(avatarFile.FileName) is { Length: > 0 } e ? e : ".png";
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await avatarFile.CopyToAsync(stream);
        }

        user.PhotoPath = $"/uploads/avatars/{fileName}";
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "account", tab = "security" });
    }

    public async Task<IActionResult> OnPostRemoveAvatarAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return await SignOutStaleSessionAsync();

        if (!string.IsNullOrEmpty(user.PhotoPath))
        {
            var previous = Path.Combine(env.WebRootPath, user.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(previous)) System.IO.File.Delete(previous);
            user.PhotoPath = null;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { saved = "account", tab = "security" });
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? await db.Users.FindAsync(userId) : null;
    }

    private async Task<IActionResult> SignOutStaleSessionAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login", new { sessionExpired = "1" });
    }

    private async Task<IActionResult> ReloadWithAccountErrorAsync(string error)
    {
        Settings = await GetOrCreateAsync();
        CurrentUser = await GetCurrentUserAsync();
        Saved = null;
        AccountError = error;
        return Page();
    }

    private async Task<IActionResult> ReloadWithPasswordErrorAsync(string error)
    {
        Settings = await GetOrCreateAsync();
        CurrentUser = await GetCurrentUserAsync();
        Saved = null;
        PasswordError = error;
        return Page();
    }

    public async Task<IActionResult> OnPostProfileAsync(string agencyName, string agencyShort, string divisionName, string headquartersAddress, string timezone, string contactEmail, string contactPhone, string brandAccent)
    {
        var s = await GetOrCreateAsync();
        s.AgencyName = agencyName; s.AgencyShort = agencyShort; s.DivisionName = divisionName; s.HeadquartersAddress = headquartersAddress;
        s.Timezone = timezone; s.ContactEmail = contactEmail; s.ContactPhone = contactPhone; s.BrandAccent = brandAccent;
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "profile" });
    }

    public async Task<IActionResult> OnPostUploadLogoAsync(IFormFile? logoFile)
    {
        var s = await GetOrCreateAsync();

        if (logoFile is null || logoFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose an image file to upload.");
            return await ReloadWithErrorAsync("Choose an image file to upload.");
        }
        if (logoFile.Length > 2 * 1024 * 1024)
        {
            return await ReloadWithErrorAsync("Logo must be 2MB or smaller.");
        }
        if (!AllowedImageTypes.Contains(logoFile.ContentType))
        {
            return await ReloadWithErrorAsync("Logo must be a PNG, JPEG, WEBP, or SVG image.");
        }

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "logo");
        Directory.CreateDirectory(uploadsDir);

        // Remove any previous logo file before saving the new one.
        if (!string.IsNullOrEmpty(s.LogoPath))
        {
            var previous = Path.Combine(env.WebRootPath, s.LogoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(previous)) System.IO.File.Delete(previous);
        }

        var ext = Path.GetExtension(logoFile.FileName) is { Length: > 0 } e ? e : ".png";
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await logoFile.CopyToAsync(stream);
        }

        s.LogoPath = $"/uploads/logo/{fileName}";
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "logo" });
    }

    public async Task<IActionResult> OnPostRemoveLogoAsync()
    {
        var s = await GetOrCreateAsync();
        if (!string.IsNullOrEmpty(s.LogoPath))
        {
            var previous = Path.Combine(env.WebRootPath, s.LogoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(previous)) System.IO.File.Delete(previous);
            s.LogoPath = null;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { saved = "logo" });
    }

    private async Task<IActionResult> ReloadWithErrorAsync(string error)
    {
        Settings = await GetOrCreateAsync();
        Saved = null;
        LogoError = error;
        return Page();
    }

    public string? LogoError { get; set; }

    public async Task<IActionResult> OnPostGpsAsync(int locationUpdateIntervalSeconds, int overspeedingThresholdKph, int offlineAlertTimeoutMinutes, string mapProvider, string defaultMapCenter, bool geofenceExitAlerts, bool routeDeviationDetection, bool idleEngineTracking)
    {
        var s = await GetOrCreateAsync();
        s.LocationUpdateIntervalSeconds = locationUpdateIntervalSeconds; s.OverspeedingThresholdKph = overspeedingThresholdKph;
        s.OfflineAlertTimeoutMinutes = offlineAlertTimeoutMinutes; s.MapProvider = mapProvider; s.DefaultMapCenter = defaultMapCenter;
        s.GeofenceExitAlerts = geofenceExitAlerts; s.RouteDeviationDetection = routeDeviationDetection; s.IdleEngineTracking = idleEngineTracking;
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "gps" });
    }

    public async Task<IActionResult> OnPostSecurityAsync(bool requireTwoFactor, bool agencySso, bool autoSuspendOnSafetyDrop, string sessionTimeout, string passwordExpiry, string ipAllowlist)
    {
        var s = await GetOrCreateAsync();
        s.RequireTwoFactor = requireTwoFactor; s.AgencySso = agencySso; s.AutoSuspendOnSafetyDrop = autoSuspendOnSafetyDrop;
        s.SessionTimeout = sessionTimeout; s.PasswordExpiry = passwordExpiry; s.IpAllowlist = ipAllowlist;
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "security" });
    }

    public async Task<IActionResult> OnPostNotificationsAsync(
        bool notifyOverspeedingEmail, bool notifyOverspeedingSms, bool notifyOverspeedingPush,
        bool notifyLowFuelEmail, bool notifyLowFuelSms, bool notifyLowFuelPush,
        bool notifyMaintenanceEmail, bool notifyMaintenanceSms, bool notifyMaintenancePush,
        bool notifyOfflineEmail, bool notifyOfflineSms, bool notifyOfflinePush,
        bool notifyAccidentEmail, bool notifyAccidentSms, bool notifyAccidentPush,
        bool notifyDocumentExpiryEmail, bool notifyDocumentExpirySms, bool notifyDocumentExpiryPush)
    {
        var s = await GetOrCreateAsync();
        s.NotifyOverspeedingEmail = notifyOverspeedingEmail; s.NotifyOverspeedingSms = notifyOverspeedingSms; s.NotifyOverspeedingPush = notifyOverspeedingPush;
        s.NotifyLowFuelEmail = notifyLowFuelEmail; s.NotifyLowFuelSms = notifyLowFuelSms; s.NotifyLowFuelPush = notifyLowFuelPush;
        s.NotifyMaintenanceEmail = notifyMaintenanceEmail; s.NotifyMaintenanceSms = notifyMaintenanceSms; s.NotifyMaintenancePush = notifyMaintenancePush;
        s.NotifyOfflineEmail = notifyOfflineEmail; s.NotifyOfflineSms = notifyOfflineSms; s.NotifyOfflinePush = notifyOfflinePush;
        s.NotifyAccidentEmail = notifyAccidentEmail; s.NotifyAccidentSms = notifyAccidentSms; s.NotifyAccidentPush = notifyAccidentPush;
        s.NotifyDocumentExpiryEmail = notifyDocumentExpiryEmail; s.NotifyDocumentExpirySms = notifyDocumentExpirySms; s.NotifyDocumentExpiryPush = notifyDocumentExpiryPush;
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "notifications" });
    }

    public async Task<IActionResult> OnPostBackupAsync(string backupFrequency, string dataRetentionPeriod)
    {
        var s = await GetOrCreateAsync();
        s.BackupFrequency = backupFrequency; s.DataRetentionPeriod = dataRetentionPeriod;
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "backup" });
    }

    public async Task<IActionResult> OnPostBackupNowAsync()
    {
        var s = await GetOrCreateAsync();
        s.LastBackupAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToPage(new { saved = "backup" });
    }

    private async Task<AppSettings> GetOrCreateAsync()
    {
        var s = await db.Settings.FirstOrDefaultAsync();
        if (s is null)
        {
            s = new AppSettings();
            db.Settings.Add(s);
            await db.SaveChangesAsync();
        }
        return s;
    }
}
