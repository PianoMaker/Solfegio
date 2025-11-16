using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Music;
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

        ChordT currentChord = new ChordT();

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

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
            GenerateChord();


            return Page();
        }

        private void GenerateChord()
        {
            var rnd = new Random();

            // define range: SOL (G) in octave1 .. SOL (G) in octave2
            var gLow = new Note(NOTES.SOL, ALTER.NATURAL,1);
            var gHigh = new Note(NOTES.SOL, ALTER.NATURAL,2);
            int minAbs = gLow.AbsPitch();
            int maxAbs = gHigh.AbsPitch();

            // generate random root in range
            var root = Note.GenerateRandomNoteInRange(minAbs, maxAbs);

            // choose chord type randomly from common set
            var chordTypes = new[] { CHORDS.TRI, CHORDS.SEXT, CHORDS.SEPT, CHORDS.NONACORD, CHORDS.CORD69 };
            var chosenType = chordTypes[rnd.Next(chordTypes.Length)];

            // determine number of voices: use SelectedCount if set, otherwise default3
            int voices = rnd.Next(2, 6); // default random between 2 and 5

            // create base Chord and wrap into ChordT
            var baseChord = Chord.CreateRandomFrom(root, chosenType, voices, rnd, true);
            currentChord = new ChordT(baseChord.GetNotes());

            MessageL(14, $"Generated chord root={root.GetName()} type={chosenType} voices={voices}");
        }

        public IActionResult OnPostRecognise()
        {
            MessageL(14, "OnPostRecognise starts!");
            PopulateOptions(SelectedCount > 0 ? SelectedCount : 1);
            PopulateTypes(SelectedChord);
            return Page();

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
                case "октава":
                    Types.Add("чиста"); break;
                case "тризвук":
                case "секстакорд":
                case "квартсекстакорд":
                    Types.AddRange(new[] { "мажорний", "мінорний", "зменшений", "збільшений" }); break;
                case "септакорд":
                case "квінтсекстакорд":
                case "терцквартакорд":
                case "секундакорд":
                    Types.AddRange(new[] { "великий мажорний", "малий мажорний", "великий мінорний", "малий мінорний", "малий зменшений", "зменшений" }); break;
                case "нонакорд":
                    Types.AddRange(new[] { "мажорний", "домінантовий", "мінорний", "мажорний зі #9", "мажорний зі b9" }); break;
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

    }
}
