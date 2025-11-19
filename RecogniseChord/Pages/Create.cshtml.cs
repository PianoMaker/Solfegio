using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Music;
using static Music.Messages;
using static Music.Engine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RecogniseChord.Pages
{
    public class CreateModel : PageModel
    {
        [BindProperty] public int SelectedCount { get; set; } =0; //2..5
        [BindProperty] public string SelectedType { get; set; } = string.Empty; // chord type key
        [BindProperty] public string SelectedQuality { get; set; } = string.Empty; // quality key
        [BindProperty] public string RootNote { get; set; } = "C"; // кор≥нь

        public List<string> CountOptions { get; } = new() { "2","3","4","5" };
        public List<string> TypeOptions { get; private set; } = new();
        public List<string> QualityOptions { get; private set; } = new();
        public string? GeneratedFileRelative { get; private set; }
        public bool CanPlay => !string.IsNullOrEmpty(GeneratedFileRelative);

        private Note MakeRoot() => new(GetNoteByLetter(RootNote), ALTER.NATURAL,1);

        private NOTES GetNoteByLetter(string l)
        {
            return l.ToUpperInvariant() switch
            {
                "C" => NOTES.DO,
                "D" => NOTES.RE,
                "E" => NOTES.MI,
                "F" => NOTES.FA,
                "G" => NOTES.SOL,
                "A" => NOTES.LA,
                "B" => NOTES.SI,
                _ => NOTES.DO
            };
        }

        public List<string> RootOptions { get; } = new(){ "C","D","E","F","G","A","B" };

        public void OnGet() { }

        public IActionResult OnPostCount()
        {
            MessageL(14, $"OnPostCount: SelectedCount={SelectedCount}");
            PopulateTypes();
            QualityOptions.Clear();
            GeneratedFileRelative = null;
            return Page();
        }

        public IActionResult OnPostType()
        {
            PopulateTypes(); // ensure type list matches count
            PopulateQualities();
            GeneratedFileRelative = null;
            return Page();
        }

        public IActionResult OnPostQuality()
        {
            MessageL(14, $"OnPostQuality: SelectedQuality={SelectedQuality}");
            PopulateTypes();
            PopulateQualities();
            try
            {
                if (SelectedCount >=2 && !string.IsNullOrEmpty(SelectedType) && !string.IsNullOrEmpty(SelectedQuality))
                {
                    var chord = BuildChord();
                    if (chord != null)
                    {
                        var path = chord.SaveWave();
                        var idx = path.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
                        if (idx >=0)
                        {
                            GeneratedFileRelative = "/" + path.Substring(idx +7).TrimStart('/', '\\').Replace('\\','/');
                        }
                        MessageL(14, $"Generated chord saved to: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageL(ex.Message);
            }
            return Page();
        }

        private void PopulateTypes()
        {
            MessageL(14, $"PopulateTypes for SelectedCount={SelectedCount}");
            TypeOptions.Clear();
            switch (SelectedCount)
            {
                case 2:
                    // intervals
                    TypeOptions.AddRange(new[]{ "SECUNDA","TERZIA","QUARTA","QUINTA","SEKSTA","SEPTYMA","OCTAVA" });
                    break;
                case 3:
                    // triad + inversions
                    TypeOptions.AddRange(new[]{ "TRI","SEXT","QSEXT" });
                    break;
                case 4:
                    // seventh chord inversions
                    TypeOptions.AddRange(new[]{ "SEPT","QUINTS","TERZQ","SEC" });
                    break;
                case 5:
                    // ninth chords (base + inversions +6/9)
                    TypeOptions.AddRange(new[]{ "NONACORD","NONACORD_1i","NONACORD_2i","NONACORD_3i","NONACORD_4i","CORD69" });
                    break;
                default:
                    break;
            }
        }

        private void PopulateQualities()
        {
            // Populate internal keys (MAJ/MIN/AUG/DIM and enum names) Ч UI will show Ukrainian labels.
            QualityOptions.Clear();
            if (string.IsNullOrEmpty(SelectedType)) return;
            switch (SelectedCount)
            {
                case 2:
                    // interval qualities: use internal keys
                    QualityOptions.AddRange(new[]{ "MAJ","MIN" });
                    break;
                case 3:
                    // triad qualities (internal keys)
                    QualityOptions.AddRange(new[]{ "MAJ","MIN","AUG","DIM" });
                    break;
                case 4:
                    // seventh qualities (SEPTS enum names)
                    QualityOptions.AddRange(Enum.GetNames(typeof(SEPTS)));
                    break;
                case 5:
                    // ninth chord qualities (NINTHS enum names excluding OTHER)
                    QualityOptions.AddRange(Enum.GetNames(typeof(NINTHS)).Where(n => n != "OTHER"));
                    break;
            }
        }

        private ChordT? BuildChord()
        {
            MessageL(14, $"BuildChord: Count={SelectedCount}, Type={SelectedType}, Quality={SelectedQuality}, Root={RootNote}");
            var root = MakeRoot();
            var chord = new ChordT();
            // intervals
            if (SelectedCount ==2)
            {
                var note2 = (Note)root.Clone();
                QUALITY qual = SelectedQuality == "MIN" || SelectedQuality == "мала" ? QUALITY.MIN : QUALITY.MAJ;
                INTERVALS interval = Enum.TryParse<INTERVALS>(SelectedType, out var intr) ? intr : INTERVALS.SECUNDA;
                // transpose second note
                note2.Transpose(interval, qual, DIR.UP);
                chord.AddNote(root);
                chord.AddNote(note2);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built triad chord: {notes}");
                return chord;
            }
            if (SelectedCount ==3)
            {
                // triads & inversions
                // SelectedQuality is internal key (MAJ/MIN/AUG/DIM)
                TRIADS triadQuality = Enum.TryParse<TRIADS>(SelectedQuality, out var tq) ? tq : TRIADS.MAJ;
                chord.TriadChord(root, triadQuality);
                ApplyTriadInversion(chord, SelectedType);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built triad chord: {notes}");
                return chord;
            }
            if (SelectedCount ==4)
            {
                SEPTS septQuality = Enum.TryParse<SEPTS>(SelectedQuality, out var sq) ? sq : SEPTS.MAJMAJ;
                chord.SeventhChord(root, septQuality);
                ApplySeventhInversion(chord, SelectedType);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built triad chord: {notes}");
                return chord;
            }
            if (SelectedCount ==5)
            {
                NINTHS ninthQuality = Enum.TryParse<NINTHS>(SelectedQuality, out var nq) ? nq : NINTHS.NMAJ;
                chord.NinthChord(root, ninthQuality);
                ApplyNinthInversion(chord, SelectedType);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built triad chord: {notes}");
                return chord;
            }
            return null;
        }

        private void ApplyTriadInversion(ChordT chord, string type)
        {
            if (type == "SEXT") chord.InvertUp(1);
            else if (type == "QSEXT") chord.InvertUp(2);
        }
        private void ApplySeventhInversion(ChordT chord, string type)
        {
            if (type == "QUINTS") chord.InvertUp(1); //1st inversion
            else if (type == "TERZQ") chord.InvertUp(2); //2nd
            else if (type == "SEC") chord.InvertUp(3); //3rd
        }
        private void ApplyNinthInversion(ChordT chord, string type)
        {
            if (type == "NONACORD_1i") chord.InvertUp(1);
            else if (type == "NONACORD_2i") chord.InvertUp(2);
            else if (type == "NONACORD_3i") chord.InvertUp(3);
            else if (type == "NONACORD_4i") chord.InvertUp(4);
        }
    }
}
