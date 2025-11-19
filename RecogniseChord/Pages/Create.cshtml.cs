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
        [BindProperty] public int SelectedCount { get; set; } = 0; //2..5
        [BindProperty] public string SelectedType { get; set; } = string.Empty; // українська назва
        [BindProperty] public string SelectedQuality { get; set; } = string.Empty; // українська назва
        [BindProperty] public string RootNote { get; set; } = "C"; // корінь

        public List<string> CountOptions { get; } = new() { "2", "3", "4", "5" };
        public List<string> TypeOptions { get; private set; } = new();
        public List<string> QualityOptions { get; private set; } = new();
        public string? GeneratedFileRelative { get; private set; }
        public bool CanPlay => !string.IsNullOrEmpty(GeneratedFileRelative);

        // Маппінг enum -> українська назва (копія з Index)
        private static readonly Dictionary<string, string> TypeToUkrainian = new()
        {
            ["SECUNDA"] = "секунда",
            ["TERZIA"] = "терція",
            ["QUARTA"] = "кварта",
            ["QUINTA"] = "квінта",
            ["SEKSTA"] = "секста",
            ["SEPTYMA"] = "септима",
            ["OCTAVA"] = "октава",
            ["TRI"] = "тризвук",
            ["SEXT"] = "сексакторд",
            ["QSEXT"] = "квартсекстакорд",
            ["SEPT"] = "септакорд",
            ["QUINTS"] = "квінтсекстакорд",
            ["TERZQ"] = "терцквартакорд",
            ["SEC"] = "секундакорд",
            ["NONACORD"] = "нонакорд",
            ["NONACORD_1i"] = "нонакорд в 1 оберненні",
            ["NONACORD_2i"] = "нонакорд в 2 оберненні",
            ["NONACORD_3i"] = "нонакорд в 3 оберненні",
            ["NONACORD_4i"] = "нонакорд в 4 оберненні",
            ["CORD69"] = "акорд 6/9"
        };

        private static readonly Dictionary<string, string> UkrainianToType =
            TypeToUkrainian.ToDictionary(kv => kv.Value, kv => kv.Key);

        // Маппінг для якостей (залежить від кількості нот)
        private static Dictionary<string, string> GetQualityToUkrainian(int count) => count switch
        {
            2 => new() { ["MAJ"] = "велика", ["MIN"] = "мала", ["PERFECT"] = "чиста" },
            3 => new() { ["MAJ"] = "мажорний", ["MIN"] = "мінорний", ["AUG"] = "збільшений", ["DIM"] = "зменшений" },
            4 => new()
            {
                ["MAJAUG"] = "великий збільшений",
                ["MAJMAJ"] = "великий мажорний",
                ["MAJMIN"] = "малий мажорний",
                ["MINMAJ"] = "великий мінорний",
                ["MINMIN"] = "малий мінорний",
                ["MINDIM"] = "малий зменшений",
                ["DIMDIM"] = "зменшений",
                ["ALTQUINT"] = "з пониженою квінтою",
                ["ALTPRIM"] = "альт. прима"
            },
            5 => new()
            {
                ["HAUG"] = "двічі збільшений",
                ["HMAJ"] = "збільшений мажорний",
                ["HDOM"] = "збільшений домінантовий",
                ["NMJAUG"] = "великий збільшений",
                ["NMAJ"] = "великий мажорний",
                ["NDOM"] = "великий домінантовий",
                ["NMIN"] = "великий мінорний",
                ["NMDOM"] = "малий домінантовий",
                ["NMMIN"] = "малий мінорний",
                ["NMHALFDIM"] = "малий напівзменшений",
                ["NMDIM"] = "малий зменшений"
            },
            _ => new()
        };

        private static Dictionary<string, string> GetUkrainianToQuality(int count)
        {
            var map = GetQualityToUkrainian(count);
            return map.ToDictionary(kv => kv.Value, kv => kv.Key);
        }

        private Note MakeRoot() => new(GetNoteByLetter(RootNote), ALTER.NATURAL, 1);

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

        public List<string> RootOptions { get; } = new() { "C", "D", "E", "F", "G", "A", "B" };

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
                if (SelectedCount >= 2 && !string.IsNullOrEmpty(SelectedType) && !string.IsNullOrEmpty(SelectedQuality))
                {
                    var chord = BuildChord();
                    if (chord != null)
                    {
                        var path = chord.SaveWave();
                        var idx = path.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            GeneratedFileRelative = "/" + path.Substring(idx + 7).TrimStart('/', '\\').Replace('\\', '/');
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
            var keys = SelectedCount switch
            {
                2 => new[] { "SECUNDA", "TERZIA", "QUARTA", "QUINTA", "SEKSTA", "SEPTYMA", "OCTAVA" },
                3 => new[] { "TRI", "SEXT", "QSEXT" },
                4 => new[] { "SEPT", "QUINTS", "TERZQ", "SEC" },
                5 => new[] { "NONACORD", "NONACORD_1i", "NONACORD_2i", "NONACORD_3i", "NONACORD_4i", "CORD69" },
                _ => Array.Empty<string>()
            };

            foreach (var key in keys)
            {
                TypeOptions.Add(TypeToUkrainian.GetValueOrDefault(key, key));
            }
        }

        private void PopulateQualities()
        {
            QualityOptions.Clear();
            if (SelectedCount <= 0) return;

            // Якщо обрано тип інтервалу (для count==2), показуємо тільки відповідні якості
            if (SelectedCount == 2 && !string.IsNullOrWhiteSpace(SelectedType))
            {
                var perfectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    TypeToUkrainian.GetValueOrDefault("QUARTA", "кварта"),
                    TypeToUkrainian.GetValueOrDefault("QUINTA", "квінта"),
                    TypeToUkrainian.GetValueOrDefault("OCTAVA", "октава")
                };

                if (perfectNames.Contains(SelectedType))
                {
                    QualityOptions.Add("чиста");
                    return;
                }
            }

            // Для акордів та інших інтервалів — повний список якостей
            var map = GetQualityToUkrainian(SelectedCount);
            QualityOptions.AddRange(map.Values);
        }

        private ChordT? BuildChord()
        {
            MessageL(14, $"BuildChord: Count={SelectedCount}, Type={SelectedType}, Quality={SelectedQuality}, Root={RootNote}");
            
            // Маппінг українських назв -> enum ключі
            var typeKey = UkrainianToType.GetValueOrDefault(SelectedType, string.Empty);
            var qualityMap = GetUkrainianToQuality(SelectedCount);
            var qualityKey = qualityMap.GetValueOrDefault(SelectedQuality, string.Empty);

            var root = MakeRoot();
            var chord = new ChordT();
            
            // intervals
            if (SelectedCount == 2)
            {
                var note2 = (Note)root.Clone();
                QUALITY qual = qualityKey == "MIN" ? QUALITY.MIN : 
                              qualityKey == "PERFECT" ? QUALITY.PERFECT : QUALITY.MAJ;
                INTERVALS interval = Enum.TryParse<INTERVALS>(typeKey, out var intr) ? intr : INTERVALS.SECUNDA;
                // transpose second note
                note2.Transpose(interval, qual, DIR.UP);
                chord.AddNote(root);
                chord.AddNote(note2);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built interval: {notes}");
                return chord;
            }
            if (SelectedCount == 3)
            {
                // triads & inversions
                TRIADS triadQuality = Enum.TryParse<TRIADS>(qualityKey, out var tq) ? tq : TRIADS.MAJ;
                chord.TriadChord(root, triadQuality);
                ApplyTriadInversion(chord, typeKey);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built triad chord: {notes}");
                return chord;
            }
            if (SelectedCount == 4)
            {
                SEPTS septQuality = Enum.TryParse<SEPTS>(qualityKey, out var sq) ? sq : SEPTS.MAJMAJ;
                chord.SeventhChord(root, septQuality);
                ApplySeventhInversion(chord, typeKey);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built seventh chord: {notes}");
                return chord;
            }
            if (SelectedCount == 5)
            {
                NINTHS ninthQuality = Enum.TryParse<NINTHS>(qualityKey, out var nq) ? nq : NINTHS.NMAJ;
                chord.NinthChord(root, ninthQuality);
                ApplyNinthInversion(chord, typeKey);
                var notes = string.Join(", ", chord.Notes.Select(n => n.ToString()));
                MessageL(COLORS.gray, $"Built ninth chord: {notes}");
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
