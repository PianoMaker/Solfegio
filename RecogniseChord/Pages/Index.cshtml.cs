using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using static Music.Messages;

namespace RecogniseChord.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public string SelectedChord { get; set; }

        [BindProperty]
        public string SelectedType { get; set; }

        [BindProperty]
        public int SelectedCount { get; set; } = 0;

        public List<string> Options { get; set; } = new();

        public List<string> Types { get; set; } = new();

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

        }

        private void PopulateTypes(string chord)
        {
            MessageL(14, $"PopulateTypes starts with {chord}!");
            Types.Clear();
            if (string.IsNullOrEmpty(chord)) return;
            switch (chord.ToLowerInvariant())
            {
                default: Types.Add("Невідомий акорд"); break;
                case "секунда":
                case "терція":
                case "секста":
                case "септима":
                    Types.AddRange(new[] { "мала", "велика" }); break;
                case "кварта":
                case "квінта":
                    Types.Add("чиста"); break;
            }
        }

        private void PopulateOptions(int count)
        {
            MessageL(14, $"PopulateOptions starts with {count}!");
            Options.Clear();
            if (count <= 1)
            {
                Options.Add("Спочатку оберіть кількість нот");
                return;
            }
            switch (count)
            {
                case 2:
                    Options.AddRange(new[]
                    {
                        "Cекунда",
                        "Терція",
                        "Кварта",
                        "Квінта",
                        "Секста",
                        "Септима",
                        "Октава",
                        "Нона",
                        "Децима"
                    });
                    break;
                case 3:
                    Options.AddRange(new[] { "Тризвук", "Секстакорд", "Квартсекстакорд" }); break;
                case 4:
                    Options.AddRange(new[] { "Септакорд", "Квінтсекстакорд", "Терцквартакорд", "Секундакорд" }); break;
                case 5: Options.Add("нонакорд"); break;
                default: Options.Add("невірно обрано опцію"); break;

            }

        }

        public void OnGet()
        {
            MessageL(14, "OnGet starts!");
            PopulateOptions(0);
        }

        // Хендлер для форми вибору кількості звуків
        public IActionResult OnPostSelect()
        {
            MessageL(14, $"OnPostSelect starts! SelectedCount={SelectedCount}");
            PopulateOptions(SelectedCount > 0 ? SelectedCount : 1);
            PopulateTypes(SelectedChord);
            return Page();
        }

        public IActionResult OnPost()
        {
            MessageL(14, "OnPost starts!");
            PopulateOptions(SelectedCount);
            PopulateTypes(SelectedChord);
            return Page();
        }

        public IActionResult OnPostPlay()
        {
            MessageL(14, "Play button clicked!");
            PopulateOptions(SelectedCount);
            PopulateTypes(SelectedChord);
            return Page();
        }

        public IActionResult OnPostRecognise()
        {
            MessageL(14, "OnPostRecognise starts!");
            PopulateOptions(SelectedCount > 0 ? SelectedCount : 1);
            PopulateTypes(SelectedChord);
            return Page();

        }
    }
}
