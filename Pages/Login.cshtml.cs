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

public class LoginModel(FleetDbContext db) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = "alicia.ferreira@mmta.gov";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool Remember { get; set; } = true;

    public bool ResetSuccess { get; set; }
    public string? Error { get; set; }

    public void OnGet(string? reset, string? sessionExpired)
    {
        ResetSuccess = reset == "success";
        if (sessionExpired == "1")
        {
            Error = "Your account is no longer available. Please sign in again.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == Email);
        var hasher = new PasswordHasher<AppUser>();
        var valid = user is not null && hasher.VerifyHashedPassword(user, user.PasswordHash, Password) != PasswordVerificationResult.Failed;

        if (!valid || user is null)
        {
            Error = "That email and password combination doesn't match our records.";
            return Page();
        }
        if (user.Status == "Suspended")
        {
            Error = "This account has been suspended. Contact your fleet administrator.";
            return Page();
        }

        user.LastActiveAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = Remember, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        return RedirectToPage("/Dashboard");
    }
}
