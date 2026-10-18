using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecogniseChord.Models;
using RecogniseChord.Services;

namespace RecogniseChord.Pages;

public class VerifyCertificateModel : PageModel
{
    private readonly ICertificateStore _certificateStore;

    public VerifyCertificateModel(ICertificateStore certificateStore) => _certificateStore = certificateStore;

    public CertificateRecord? Certificate { get; private set; }
    public string? Number { get; private set; }

    public async Task OnGetAsync(string? number)
    {
        Number = number;
        if (number is { Length: 10 } && number.All(char.IsAsciiDigit))
            Certificate = await _certificateStore.FindAsync(number);
    }

    public async Task<IActionResult> OnGetLookupAsync(string? number)
    {
        if (number is not { Length: 10 } || !number.All(char.IsAsciiDigit))
            return new JsonResult(new { valid = false });

        var certificate = await _certificateStore.FindAsync(number);
        if (certificate == null) return new JsonResult(new { valid = false });

        return new JsonResult(new
        {
            valid = true,
            number = certificate.Number,
            recipientName = certificate.RecipientName,
            level = certificate.Level,
            correctCount = certificate.CorrectCount,
            total = certificate.Total,
            issuedAtUtc = certificate.IssuedAtUtc
        });
    }
}
