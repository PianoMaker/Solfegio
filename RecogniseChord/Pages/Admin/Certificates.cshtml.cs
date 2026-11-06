using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecogniseChord.Models;
using RecogniseChord.Services;

namespace RecogniseChord.Pages.Admin;

public sealed class CertificatesModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly ICertificateStore _certificateStore;
    private readonly IFailedQuizAttemptStore _failedAttemptStore;

    public CertificatesModel(
        IConfiguration configuration,
        ICertificateStore certificateStore,
        IFailedQuizAttemptStore failedAttemptStore)
    {
        _configuration = configuration;
        _certificateStore = certificateStore;
        _failedAttemptStore = failedAttemptStore;
    }

    public IReadOnlyList<CertificateRecord> Certificates { get; private set; } = Array.Empty<CertificateRecord>();
    public IReadOnlyList<FailedQuizAttempt> FailedAttempts { get; private set; } = Array.Empty<FailedQuizAttempt>();

    public async Task<IActionResult> OnGetAsync()
    {
        var adminEmail = _configuration["Admin:Email"];
        if (string.IsNullOrWhiteSpace(adminEmail))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Admin:Email is not configured.");

        if (User.Identity?.IsAuthenticated != true)
        {
            if (string.IsNullOrWhiteSpace(_configuration["Google:ClientId"]) ||
                string.IsNullOrWhiteSpace(_configuration["Google:ClientSecret"]))
                return Unauthorized();

            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Admin/Certificates")!
            }, GoogleDefaults.AuthenticationScheme);
        }

        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (!string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        Certificates = await _certificateStore.ListAsync();
        FailedAttempts = await _failedAttemptStore.ListAsync();
        return Page();
    }

    public static string DisplayLevel(string level) => level.ToLowerInvariant() switch
    {
        "easy" => "Легкий",
        "base" => "Базовий",
        "intermediate" => "Середній",
        "advanced" => "Просунутий",
        "expert" => "Експерт",
        _ => level
    };
}
