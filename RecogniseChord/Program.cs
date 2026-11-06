using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using RecogniseChord.Services;
using Amazon.Runtime;
using Amazon.S3;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddRazorPages()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();
        var r2AccessKeyId = builder.Configuration["CloudflareR2:AccessKeyId"];
        var r2SecretAccessKey = builder.Configuration["CloudflareR2:SecretAccessKey"];
        if (!string.IsNullOrWhiteSpace(r2AccessKeyId) && !string.IsNullOrWhiteSpace(r2SecretAccessKey))
        {
            var r2Endpoint = builder.Configuration["CloudflareR2:Endpoint"];
            if (string.IsNullOrWhiteSpace(r2Endpoint) || string.IsNullOrWhiteSpace(builder.Configuration["CloudflareR2:BucketName"]))
                throw new InvalidOperationException("Cloudflare R2 endpoint and bucket name must be configured.");

            builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                new BasicAWSCredentials(r2AccessKeyId, r2SecretAccessKey),
                new AmazonS3Config
                {
                    ServiceURL = r2Endpoint,
                    AuthenticationRegion = "auto",
                    ForcePathStyle = true
                }));
            builder.Services.AddSingleton<R2JsonObjectStore>();
            builder.Services.AddSingleton<ICertificateStore, R2CertificateStore>();
            builder.Services.AddSingleton<IFailedQuizAttemptStore, R2FailedQuizAttemptStore>();
            builder.Services.AddSingleton<ICertifiedQuizChordAttemptStore, R2CertifiedQuizChordAttemptStore>();
        }
        else if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<ICertificateStore, CertificateJsonStore>();
            builder.Services.AddSingleton<IFailedQuizAttemptStore, FailedQuizAttemptJsonStore>();
            builder.Services.AddSingleton<ICertifiedQuizChordAttemptStore, CertifiedQuizChordAttemptJsonStore>();
        }
        else
        {
            throw new InvalidOperationException("Cloudflare R2 credentials must be configured outside Development.");
        }
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
