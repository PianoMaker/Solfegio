using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using System.Text.Json;
using QRCoder;
using RecogniseChord.Models;
using RecogniseChord.Services;

namespace RecogniseChord.Pages
{
    public class GetCertifiedModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ICertificateStore _certificateStore;

        public GetCertifiedModel(IConfiguration configuration, ICertificateStore certificateStore)
        {
            _configuration = configuration;
            _certificateStore = certificateStore;
        }

        public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
        public string DisplayName => User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        [TempData]
        public string? LoginError { get; set; }

        public IActionResult OnGetGoogleLogin(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(_configuration["Google:ClientId"]) || string.IsNullOrWhiteSpace(_configuration["Google:ClientSecret"]))
            {
                LoginError = "GoogleNotConfigured";
                return RedirectToPage("/GetCertified");
            }

            var destination = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Page("/GetCertified")!;
            return Challenge(new AuthenticationProperties { RedirectUri = destination }, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> OnPostIssueCertificateAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return Unauthorized();
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var recipientName = DisplayName.Trim();
            if (string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(recipientName) || recipientName.Length > 100) return BadRequest();
            if (TempData.Peek("__CertifiedQuizProgress") is not string progressJson) return BadRequest();

            QuizProgressData? progress;
            try { progress = JsonSerializer.Deserialize<QuizProgressData>(progressJson); }
            catch { return BadRequest(); }
            if (progress == null || progress.Answered != progress.Total || progress.Incorrect > 1 || progress.Correct + progress.Incorrect != progress.Total)
                return BadRequest();

            var expectedTotal = progress.Level switch
            {
                "easy" => 10,
                "base" => 15,
                "advanced" => 20,
                _ => 0
            };
            if (progress.Total != expectedTotal) return BadRequest();

            var certificate = await _certificateStore.GetOrCreateForOwnerAndLevelAsync(
                ownerId, recipientName, progress.Level, progress.Correct, progress.Total);

            progress.CertificateNumber = certificate.Number;
            TempData["__CertifiedQuizProgress"] = JsonSerializer.Serialize(progress);
            var verificationUrl = Url.Page("/VerifyCertificate", pageHandler: null, values: new { number = certificate.Number }, protocol: Request.Scheme)!;
            var qrUrl = Url.Page("/GetCertified", pageHandler: "Qr", values: new { number = certificate.Number }, protocol: Request.Scheme)!;
            return new JsonResult(new
            {
                number = certificate.Number,
                recipientName = certificate.RecipientName,
                level = certificate.Level,
                correctCount = certificate.CorrectCount,
                total = certificate.Total,
                issuedAtUtc = certificate.IssuedAtUtc,
                verificationUrl,
                qrUrl
            });
        }

        public async Task<IActionResult> OnGetQrAsync(string number)
        {
            var certificate = await _certificateStore.FindAsync(number);
            if (certificate == null) return NotFound();

            var verifyUrl = Url.Page("/VerifyCertificate", pageHandler: null, values: new { number = certificate.Number }, protocol: Request.Scheme)!;
            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(verifyUrl, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(qrData).GetGraphic(8);
            return File(png, "image/png");
        }

        private sealed class QuizProgressData
        {
            public string Level { get; set; } = string.Empty;
            public int Total { get; set; }
            public int Answered { get; set; }
            public int Correct { get; set; }
            public int Incorrect { get; set; }
            public string? CertificateNumber { get; set; }
        }
    }
}
