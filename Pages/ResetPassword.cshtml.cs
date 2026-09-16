using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class ResetPasswordModel(FleetDbContext db) : PageModel
{
    [BindProperty]
    public string NewPassword { get; set; } = "";

    [BindProperty]
    public string ConfirmPassword { get; set; } = "";

    public bool LinkValid { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(Guid token)
    {
        LinkValid = await IsValidResetTokenAsync(token);
    }

    public async Task<IActionResult> OnPostAsync(Guid token)
    {
        LinkValid = await IsValidResetTokenAsync(token);
        if (!LinkValid) return Page();

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
        {
            Error = "Password must be at least 8 characters.";
            return Page();
        }
        if (NewPassword != ConfirmPassword)
        {
            Error = "Passwords don't match.";
            return Page();
        }

        var otp = await db.OtpCodes.Include(o => o.User).FirstAsync(o => o.Token == token);
        var hasher = new PasswordHasher<AppUser>();
        otp.User!.PasswordHash = hasher.HashPassword(otp.User, NewPassword);
        otp.IsUsed = true;
        await db.SaveChangesAsync();

        return RedirectToPage("/Login", new { reset = "success" });
    }

    private async Task<bool> IsValidResetTokenAsync(Guid token)
    {
        var otp = await db.OtpCodes.FirstOrDefaultAsync(o => o.Token == token);
        return otp is not null && otp.Purpose == "reset" && otp.VerifiedAt != null && !otp.IsUsed && otp.ExpiresAt > DateTime.UtcNow;
    }
}
