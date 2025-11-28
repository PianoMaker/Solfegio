using System.Globalization;
using Microsoft.AspNetCore.Localization;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Додаємо сервіси локалізації та вказуємо шлях до ресурсів.
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        // 2. Додаємо Razor Pages та підтримку локалізації для них.
        builder.Services.AddRazorPages()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();

        var app = builder.Build();

        // 3. Налаштовуємо підтримувані культури.
        var supportedCultures = new[] { new CultureInfo("uk"), new CultureInfo("en") };
        var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture("uk")
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
        app.UseAuthorization();
        app.MapRazorPages();
        app.Use(async (context, next) =>
        {
            await next();
            var ct = context.Response.ContentType;
            if (!string.IsNullOrEmpty(ct) && ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase) && !ct.Contains("charset", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = ct + "; charset=utf-8";
            }
        });
        app.Run();
    }
}