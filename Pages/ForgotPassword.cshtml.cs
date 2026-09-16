using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Data;
using VehicleFleetMS.Helpers;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Pages;

public class ForgotPasswordModel(FleetDbContext db) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == Email);

        // Always redirect the same way whether or not the email exists, so the
        // form can't be used to enumerate valid accounts.
        if (user is null)
        {
            return RedirectToPage("/ResetPassword", new { token = Guid.Empty });
        }

        var resetToken = new OtpCode
        {
            UserId = user.Id,
            Code = OtpHelper.GenerateCode(),
            Purpose = "reset",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            VerifiedAt = DateTime.UtcNow,
        };
        db.OtpCodes.Add(resetToken);
        await db.SaveChangesAsync();

        return RedirectToPage("/ResetPassword", new { token = resetToken.Token });
    }
}
