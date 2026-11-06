using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using NuGet.Protocol;
using RecogniseChord.Models;
using RecogniseChord.Services;
using static Music.Messages;

namespace RecogniseChord.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;
        private readonly IStringLocalizer _localizer;
        private readonly ICertifiedQuizChordAttemptStore _certifiedQuizChordAttemptStore;

        public IWebHostEnvironment _environment;


        public int Attempts;
        public SuccessInfo Successes = new();
        public FailInfo Fails = new();

        public PrivacyModel(
            ILogger<PrivacyModel> logger,
            IWebHostEnvironment environment,
            IStringLocalizerFactory localizerFactory,
            ICertifiedQuizChordAttemptStore certifiedQuizChordAttemptStore)
        {
            _logger = logger;
            _environment = environment;
            _localizer = localizerFactory.Create("Pages.Index", typeof(IndexModel).Assembly.GetName().Name!);
            _certifiedQuizChordAttemptStore = certifiedQuizChordAttemptStore;
        }

        public async Task OnGetAsync()
        {

            string FilePath = Path.Combine(_environment.WebRootPath, "info", "info.txt");
            string SuccessPath = Path.Combine(_environment.WebRootPath, "info", "success.txt");
            string FailPath = Path.Combine(_environment.WebRootPath, "info", "fail.txt");
            
            
           
            try
            {
                var attempts = System.IO.File.ReadAllText(FilePath);
                Attempts = int.Parse(attempts);
            }
            catch (Exception ex)
            {
                ErrorMessageL("file was not found\n");
                MessageL(Music.COLORS.gray, ex.ToString());
            }
            try
            {
                var successes = System.IO.File.ReadAllText(SuccessPath);
                Successes = successes.FromJson<SuccessInfo>();
            }
            catch (Exception ex)
            {
                ErrorMessageL("file was not found\n");
                MessageL(Music.COLORS.gray, ex.ToString());
            }
            try
            {
                var fails = System.IO.File.ReadAllText(FailPath);
                Fails = fails.FromJson<FailInfo>();
            }
            catch (Exception ex)
            {
                ErrorMessageL("file was not found\n");
                MessageL(Music.COLORS.gray, ex.ToString());
            }

            try
            {
                var certifiedAttempts = await _certifiedQuizChordAttemptStore.ListAsync();
                Attempts += certifiedAttempts.Count;

                foreach (var attempt in certifiedAttempts)
                {
                    switch (attempt.ChordCount)
                    {
                        case 2:
                            if (attempt.Correct) Successes.success2s++;
                            else Fails.fail2s++;
                            break;
                        case 3:
                            if (attempt.Correct) Successes.success3s++;
                            else Fails.fail3s++;
                            break;
                        case 4:
                            if (attempt.Correct) Successes.success4s++;
                            else Fails.fail4s++;
                            break;
                        case 5:
                            if (attempt.Correct) Successes.success5s++;
                            else Fails.fail5s++;
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to load certified quiz chord attempts for public statistics.");
            }


        }


    }

}
