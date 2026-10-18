using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace RecogniseChord.Pages
{
    public class GetCertifiedModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public GetCertifiedModel(IConfiguration configuration) => _configuration = configuration;

        public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
        public string DisplayName => User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        [TempData]
        public string? LoginError { get; set; }

        public IActionResult OnGetGoogleLogin(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(_configuration["Google:ClientId"]) || string.IsNullOrWhiteSpace(_configuration["Google:ClientSecret"]))
            {
                LoginError = "Вхід через Google ще не налаштовано. Додайте Google:ClientId і Google:ClientSecret у конфігурацію сервера.";
                return RedirectToPage("/GetCertified");
            }

            var destination = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Page("/GetCertified")!;
            return Challenge(new AuthenticationProperties { RedirectUri = destination }, GoogleDefaults.AuthenticationScheme);
        }
    }
}
