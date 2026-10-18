using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RecogniseChord.Pages
{
    public class LangModel : PageModel
    {
        public IActionResult OnGet(string? culture, string? returnUrl)
        {
            if (culture is "uk" or "en")
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps }
                );
            }

            if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

            var referer = Request.Headers.Referer.ToString();
            if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) &&
                string.Equals(refererUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
                return LocalRedirect(refererUri.PathAndQuery);

            return RedirectToPage("/Index");
        }
    }
}
