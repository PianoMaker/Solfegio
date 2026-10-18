using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddRazorPages()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();
        var googleClientId = builder.Configuration["Google:ClientId"];
        var googleClientSecret = builder.Configuration["Google:ClientSecret"];
        var authentication = builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret)
                ? GoogleDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie();

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/signin-google";
            });
        }

        var app = builder.Build();
        var supportedCultures = new[] { "uk", "en" };
        var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
        app.UseRequestLocalization(localizationOptions);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();
        app.Use(async (context, next) =>
        {
            await next();
            var contentType = context.Response.ContentType;
            if (!string.IsNullOrEmpty(contentType) && contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) && !contentType.Contains("charset", StringComparison.OrdinalIgnoreCase))
                context.Response.ContentType = contentType + "; charset=utf-8";
        });
        app.Run();
    }
}
