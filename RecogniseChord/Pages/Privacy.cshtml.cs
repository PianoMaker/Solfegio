using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using NuGet.Protocol;
using RecogniseChord.Models;
using static Music.Messages;

namespace RecogniseChord.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;
        private readonly IStringLocalizer _localizer;

        public IWebHostEnvironment _environment;


        public int Attempts;
        public SuccessInfo Successes = new();
        public FailInfo Fails = new();

        public PrivacyModel(ILogger<PrivacyModel> logger, IWebHostEnvironment environment, IStringLocalizerFactory localizerFactory)
        {
            _logger = logger;
            _environment = environment;
            _localizer = localizerFactory.Create("Pages.Index", typeof(IndexModel).Assembly.GetName().Name!);
        }

        public void OnGet()
        {

            string FilePath = Path.Combine(_environment.WebRootPath, "info", "info.txt");
            string SuccessPath = Path.Combine(_environment.WebRootPath, "info", "successes.txt");
            string FailPath = Path.Combine(_environment.WebRootPath, "info", "fails.txt");
            
            
           
            try
            {
                var attempts = System.IO.File.ReadAllText(FilePath);
                Attempts = int.Parse(attempts);
            }
            catch (Exception ex)
            {
                ErrorMessageL(ex.ToString());
            }
            try
            {
                var successes = System.IO.File.ReadAllText(SuccessPath);
                Successes = successes.FromJson<SuccessInfo>();
            }
            catch (Exception ex)
            {
                ErrorMessageL(ex.ToString());
            }
            try
            {
                var fails = System.IO.File.ReadAllText(FailPath);
                Fails = fails.FromJson<FailInfo>();
            }
            catch (Exception ex)
            {
                ErrorMessageL(ex.ToString());
            }


        }


    }

}
