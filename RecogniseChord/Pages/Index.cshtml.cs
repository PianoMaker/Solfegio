using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Music;
using static Music.Messages;
using static Music.Engine;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RecogniseChord.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        // User selections (guess)
        [BindProperty] public int SelectedCount { get; set; } =0; //2..5
        [BindProperty] public string SelectedType { get; set; } = string.Empty; // chord type key (internal)
        [BindProperty] public string SelectedQuality { get; set; } = string.Empty; // quality key (internal)
        [BindProperty] public string RootNoteGuess { get; set; } = string.Empty; // optional root guess
        // Legacy / UI binding names (map to existing quality/type)
        [BindProperty] public string SelectedChord { get; set; } = string.Empty; // mapped to SelectedQuality

        // Generated chord info (current one to play / guess)
        public string? GeneratedFileRelative { get; private set; }
        public int GeneratedCount { get; private set; }
        public string GeneratedType { get; private set; } = string.Empty;
        public string GeneratedQuality { get; private set; } = string.Empty;
        public string GeneratedRoot { get; private set; } = string.Empty;
        public string GeneratedNotesDisplay { get; private set; } = string.Empty;
        public bool CanPlay => !string.IsNullOrEmpty(GeneratedFileRelative);
        public string? GuessResult { get; private set; } // "вірно" / "невірно"
        public string? GeneratedChordJson { get; private set; } // JSON payload for client playback (notes + meta)

        // Feedback for recognise action
        public bool? RecogniseOk { get; private set; }
        public string? RecogniseCorrect { get; private set; }

        // Options for UI
        public List<string> CountOptions { get; } = new() { "2", "3", "4", "5" };
        // Legacy property names expected by Razor page
        public List<string> Options { get; private set; } = new(); // qualities list (maps QualityOptions)
        public List<string> Types { get; private set; } = new(); // inversion/type list (maps TypeOptions)
        // Internal collections retained for clarity
        public List<string> TypeOptions { get; private set; } = new();
        public List<string> QualityOptions { get; private set; } = new();
        public List<string> RootOptions { get; } = new() { "C", "D", "E", "F", "G", "A", "B" };

        // Current chord object
        public ChordT currentChord = new();

        // TempData key for the current chord
        private const string CurrentChordKey = "__currentChord";

        public IndexModel(ILogger<IndexModel> logger)
        { _logger = logger; }

        public void OnGet()
        {
            MessageL(14, "Index OnGet: generating initial chord");
            var chordData = GenerateRandomChord();
            ApplyChordData(chordData); // show to user
            // store current shown chord so recognise can validate
            TempData[CurrentChordKey] = JsonSerializer.Serialize(chordData);

            // Prepare UI option lists for initial generated chord
            PopulateTypesForGenerated();
            PopulateQualitiesForGenerated();
            SyncLegacyLists();
        }

        public IActionResult OnPostPlay()
        {
            // Play should use the current chord stored in TempData and must NOT generate a new chord
            if (TempData.TryGetValue(CurrentChordKey, out var curObj) && curObj is string curJson && !string.IsNullOrWhiteSpace(curJson))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<ChordData>(curJson);
                    if (data != null)
                    {
                        ApplyChordData(data);
                        // keep it available for subsequent actions
                        TempData.Keep(CurrentChordKey);
                        MessageL(COLORS.gray, $"Play chord notes: {data.NotesDisplay}");
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageL("Chord play deserialize error: " + ex.Message);
                }
            }
            else
            {
                // If no current chord found, generate one (edge case) and store it
                var generated = GenerateRandomChord();
                ApplyChordData(generated);
                TempData[CurrentChordKey] = JsonSerializer.Serialize(generated);
            }

            PopulateTypesForGenerated();
            PopulateQualitiesForGenerated();
            SyncLegacyLists();
            return Page();
        }

        // Handler when user changes count of notes (radio buttons) -> refresh type/quality lists
        public IActionResult OnPostSelect()
        {
            PopulateTypes(SelectedCount);
            PopulateQualities(SelectedCount, SelectedType);
            SyncLegacyLists();
            // restore current chord info from TempData so UI keeps the ability to play it
            if (TempData.TryGetValue(CurrentChordKey, out var curObj) && curObj is string curJson && !string.IsNullOrWhiteSpace(curJson))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<ChordData>(curJson);
                    if (data != null)
                    {
                        ApplyChordData(data);
                        // keep current chord available for subsequent actions
                        TempData.Keep(CurrentChordKey);
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageL("Restore current chord error: " + ex.Message);
                }
            }
            return Page();
        }

        // Handler when user changes chord/type selects (legacy recognise)
        public IActionResult OnPostRecognise()
        {
            // Retrieve current generated chord (the one user is trying to recognise)
            ChordData actual = null;
            if (TempData.TryGetValue(CurrentChordKey, out var curObj) && curObj is string curJson && !string.IsNullOrWhiteSpace(curJson))
            {
                try
                {
                    actual = JsonSerializer.Deserialize<ChordData>(curJson);
                }
                catch (Exception ex)
                {
                    ErrorMessageL("Recognise deserialize error: " + ex.Message);
                }
            }

            if (actual != null)
            {
                // Map legacy SelectedChord to SelectedQuality for comparison
                if (!string.IsNullOrEmpty(SelectedChord))
                    SelectedQuality = SelectedChord;

                bool ok = SelectedCount == actual.Count &&
                           string.Equals(SelectedType, actual.Type, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(SelectedQuality, actual.Quality, StringComparison.OrdinalIgnoreCase);

                RecogniseOk = ok;
                GuessResult = ok ? "вірно" : "невірно";
                RecogniseCorrect = $"{actual.Type} {actual.Quality} (корінь {actual.Root}) — ноти: {actual.NotesDisplay}";

                // keep current chord in TempData while we display feedback
                TempData.Keep(CurrentChordKey);
            }
            else
            {
                // If no stored current chord, set feedback accordingly
                RecogniseOk = null;
                RecogniseCorrect = "Немає збереженого акорду для перевірки.";
            }

            // After showing feedback, generate a new chord and replace current
            var chordData = GenerateRandomChord();
            ApplyChordData(chordData); // show new chord to user
            TempData[CurrentChordKey] = JsonSerializer.Serialize(chordData);

            // refresh lists
            PopulateTypes(SelectedCount);
            PopulateQualities(SelectedCount, SelectedType);
            SyncLegacyLists();

            return Page();
        }

        public IActionResult OnPostGuess()
        {
            // Map legacy binding if used
            if (!string.IsNullOrEmpty(SelectedChord))
                SelectedQuality = SelectedChord;
            PopulateTypes(SelectedCount);
            PopulateQualities(SelectedCount, SelectedType);
            bool ok = SelectedCount == GeneratedCount &&
                       string.Equals(SelectedType, GeneratedType, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(SelectedQuality, GeneratedQuality, StringComparison.OrdinalIgnoreCase);
            GuessResult = ok ? "вірно" : "невірно";
            MessageL(ok ? COLORS.green : COLORS.red, $"Guess result: {GuessResult} (user: {SelectedCount}/{SelectedType}/{SelectedQuality} actual: {GeneratedCount}/{GeneratedType}/{GeneratedQuality})");
            TempData.Keep(CurrentChordKey);
            SyncLegacyLists();
            return Page();
        }

        private ChordData GenerateRandomChord()
        {
            var rnd = new Random();
            int count = rnd.Next(2,6); //2..5
            string typeKey = string.Empty;
            string qualityKey = string.Empty;
            string rootLetter = RootOptions[rnd.Next(RootOptions.Count)];
            Note root = new(GetNoteByLetter(rootLetter), ALTER.NATURAL,1);
            var chord = new ChordT();

            if (count == 2)
            {
                var intervalTypes = new[] { "SECUNDA", "TERZIA", "QUARTA", "QUINTA", "SEKSTA", "SEPTYMA", "OCTAVA" };
                typeKey = intervalTypes[rnd.Next(intervalTypes.Length)];

                // parse interval enum
                INTERVALS interval = Enum.TryParse<INTERVALS>(typeKey, out var intr) ? intr : INTERVALS.SECUNDA;

                // choose quality set depending on whether interval is a "perfect" type
                // perfect-type intervals: PRIMA, QUARTA, QUINTA, OCTAVA
                var perfectSet = new[] { INTERVALS.PRIMA, INTERVALS.QUARTA, INTERVALS.QUINTA, INTERVALS.OCTAVA };
                QUALITY qual;
                if (perfectSet.Contains(interval))
                {
                    // choose among perfect/aug/dim for perfect intervals
                    var qopts = new[] { "PERFECT", "AUG", "DIM" };
                    qualityKey = qopts[rnd.Next(qopts.Length)];
                    qual = qualityKey == "AUG" ? QUALITY.AUG
                         : qualityKey == "DIM" ? QUALITY.DIM
                         : QUALITY.PERFECT;
                }
                else
                {
                    // for imperfect intervals (seconds, thirds, sixths, sevenths) choose sensible qualities
                    var qopts = new[] { "MAJ", "MIN", "AUG", "DIM" };
                    qualityKey = qopts[rnd.Next(qopts.Length)];
                    qual = qualityKey == "MIN" ? QUALITY.MIN : qualityKey == "AUG" ? QUALITY.AUG : qualityKey == "DIM" ? QUALITY.DIM : QUALITY.MAJ;
                }

                var note2 = (Note)root.Clone();
                // transpose using resolved interval and quality upward
                note2.Transpose(interval, qual, DIR.UP);
                chord.AddNote(root);
                chord.AddNote(note2);
            }
            else if (count ==3)
            {
                var triTypes = new[] { "TRI", "SEXT", "QSEXT" };
                typeKey = triTypes[rnd.Next(triTypes.Length)];
                var qualities = new[] { "MAJ", "MIN", "AUG", "DIM" };
                qualityKey = qualities[rnd.Next(qualities.Length)];
                TRIADS triadQuality = Enum.TryParse<TRIADS>(qualityKey, out var tq) ? tq : TRIADS.MAJ;
                chord.TriadChord(root, triadQuality);
                ApplyTriadInversion(chord, typeKey);
            }
            else if (count ==4)
            {
                var septTypes = new[] { "SEPT", "QUINTS", "TERZQ", "SEC" };
                typeKey = septTypes[rnd.Next(septTypes.Length)];
                var septQualities = Enum.GetNames(typeof(SEPTS));
                qualityKey = septQualities[rnd.Next(septQualities.Length)];
                SEPTS septQuality = Enum.TryParse<SEPTS>(qualityKey, out var sq) ? sq : SEPTS.MAJMAJ;
                chord.SeventhChord(root, septQuality);
                ApplySeventhInversion(chord, typeKey);
            }
            else if (count ==5)
            {
                var ninthTypes = new[] { "NONACORD", "NONACORD_1i", "NONACORD_2i", "NONACORD_3i", "NONACORD_4i", "CORD69" };
                typeKey = ninthTypes[rnd.Next(ninthTypes.Length)];
                var ninthQualities = Enum.GetNames(typeof(NINTHS)).Where(n => n != "OTHER").ToArray();
                qualityKey = ninthQualities.Length >0 ? ninthQualities[rnd.Next(ninthQualities.Length)] : string.Empty;
                if (!string.IsNullOrEmpty(qualityKey) && Enum.TryParse<NINTHS>(qualityKey, out var nq))
                    chord.NinthChord(root, nq);
                ApplyNinthInversion(chord, typeKey);
            }

            string fullPath = chord.SaveWave();
            string rel = RelativeFromFull(fullPath);
            string notesDisplay = string.Join(", ", chord.Notes.Select(n => n.GetName()));
            // Log generated notes and chord type/quality
            MessageL(COLORS.gray, $"Generated chord ({typeKey}/{qualityKey}) notes: {notesDisplay}");

            // Build JSON payload for client-side playback: { notes: [{frequency,duration}], type, quality, count, root, file }
            var noteObjects = chord.Notes.Select(n => new
            {
                frequency = n.AbsPitch() <0 ?0 : Pitch_to_hz(n.AbsPitch()),
                duration = n.AbsDuration()
            }).ToList();

            var payload = new
            {
                notes = noteObjects,
                type = typeKey,
                quality = qualityKey,
                count = count,
                root = rootLetter,
                file = rel
            };

            string notesJson = JsonSerializer.Serialize(payload);

            return new ChordData
            {
                Count = count,
                Type = typeKey,
                Quality = qualityKey,
                Root = rootLetter,
                FileRelative = rel,
                NotesDisplay = notesDisplay,
                NotesJson = notesJson
            };
        }

        private void ApplyChordData(ChordData data)
        {
            GeneratedCount = data.Count;
            GeneratedType = data.Type;
            GeneratedQuality = data.Quality;
            GeneratedRoot = data.Root;
            GeneratedFileRelative = data.FileRelative;
            GeneratedNotesDisplay = data.NotesDisplay;
            GeneratedChordJson = data.NotesJson;
            currentChord = new ChordT();
        }

        private string RelativeFromFull(string path)
        {
            var idx = path.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
            if (idx >=0)
            {
                return "/" + path.Substring(idx +7).TrimStart('/', '\\').Replace('\\', '/');
            }
            return string.Empty;
        }

        private void PopulateTypes(int count)
        {
            TypeOptions.Clear();
            switch (count)
            {
                case 2: TypeOptions.AddRange(new[] { "SECUNDA", "TERZIA", "QUARTA", "QUINTA", "SEKSTA", "SEPTYMA", "OCTAVA" }); break;
                case 3: TypeOptions.AddRange(new[] { "TRI", "SEXT", "QSEXT" }); break;
                case 4: TypeOptions.AddRange(new[] { "SEPT", "QUINTS", "TERZQ", "SEC" }); break;
                case 5: TypeOptions.AddRange(new[] { "NONACORD", "NONACORD_1i", "NONACORD_2i", "NONACORD_3i", "NONACORD_4i", "CORD69" }); break;
            }
        }
        private void PopulateTypesForGenerated() => PopulateTypes(GeneratedCount);

        private void PopulateQualities(int count, string type)
        {
            QualityOptions.Clear();
            if (string.IsNullOrEmpty(type)) return;
            switch (count)
            {
                case 2: QualityOptions.AddRange(new[] { "MAJ", "MIN" }); break;
                case 3: QualityOptions.AddRange(new[] { "MAJ", "MIN", "AUG", "DIM" }); break;
                case 4: QualityOptions.AddRange(Enum.GetNames(typeof(SEPTS))); break;
                case 5: QualityOptions.AddRange(Enum.GetNames(typeof(NINTHS)).Where(n => n != "OTHER")); break;
            }
        }
        private void PopulateQualitiesForGenerated() => PopulateQualities(GeneratedCount, GeneratedType);

        private void SyncLegacyLists()
        {
            // Mirror internal lists to legacy names expected by Razor (Options = qualities, Types = types)
            Options = QualityOptions.ToList();
            Types = TypeOptions.ToList();
            // Ensure SelectedChord mirrors SelectedQuality if one of the lists updated
            if (!string.IsNullOrEmpty(SelectedQuality)) SelectedChord = SelectedQuality;
        }

        // Inversion helpers
        private void ApplyTriadInversion(ChordT chord, string type)
        { if (type == "SEXT") chord.InvertUp(1); else if (type == "QSEXT") chord.InvertUp(2); }
        private void ApplySeventhInversion(ChordT chord, string type)
        { if (type == "QUINTS") chord.InvertUp(1); else if (type == "TERZQ") chord.InvertUp(2); else if (type == "SEC") chord.InvertUp(3); }
        private void ApplyNinthInversion(ChordT chord, string type)
        { if (type == "NONACORD_1i") chord.InvertUp(1); else if (type == "NONACORD_2i") chord.InvertUp(2); else if (type == "NONACORD_3i") chord.InvertUp(3); else if (type == "NONACORD_4i") chord.InvertUp(4); }

        // Map root letter to NOTES enum
        private NOTES GetNoteByLetter(string l) => l.ToUpperInvariant() switch
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

        private class ChordData
        {
            public int Count { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Quality { get; set; } = string.Empty;
            public string Root { get; set; } = string.Empty;
            public string FileRelative { get; set; } = string.Empty;
            public string NotesDisplay { get; set; } = string.Empty;
            public string NotesJson { get; set; } = string.Empty; // serialized notes + meta
        }
    }
}
