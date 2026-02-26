using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Music.Engine;
using static Music.Messages;

namespace RecogniseChord.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private const string MaxCountTempKey = "__MaxCount";

        public int RequestCount;

        public List<string> AllTimbres { get; set; } = Enum.GetNames(typeof(TIMBRE)).ToList();

        // User selections (guess) — тепер зберігають українські назви
        [BindProperty] public int SelectedCount { get; set; } = 0; //2..5
        [BindProperty] public string SelectedType { get; set; } = string.Empty; // українська назва
        [BindProperty] public string SelectedQuality { get; set; } = string.Empty; // українська назва
        [BindProperty] public string RootNoteGuess { get; set; } = string.Empty;
        // Legacy / UI binding names (map to existing quality/type)
        [BindProperty] public string SelectedChord { get; set; } = string.Empty; // mapped to SelectedQuality

        [BindProperty] public string Timbre { get; set; }

        public string UserAnswer { get; set; } = string.Empty;

        // Generated chord info (current one to play / guess)
        public string? GeneratedFileRelative { get; private set; }
        public int GeneratedCount { get; private set; }
        public string GeneratedType { get; private set; } = string.Empty; // internal key from generator
        public string GeneratedQuality { get; private set; } = string.Empty; // internal key from generator
        public string GeneratedRoot { get; private set; } = string.Empty;
        public string GeneratedNotesDisplay { get; private set; } = string.Empty;
        public bool CanPlay => !string.IsNullOrEmpty(GeneratedFileRelative);
        public string? GuessResult { get; private set; } // "вірно" / "невірно"
        public string? GeneratedChordJson { get; private set; } // JSON payload for client playback (notes + meta)
        [BindProperty]
        public int MaxCount { get; set; } = 4;

        // Feedback for recognise action
        public bool? RecogniseOk { get; private set; }
        public string? RecogniseCorrect { get; private set; }

        // Options for UI (display strings in Ukrainian)
        public List<string> CountOptions { get; } = new() { "2", "3", "4", "5" };
        public List<string> Options { get; private set; } = new(); // qualities list (display)
        public List<string> Types { get; private set; } = new(); // type list (display)
        public List<string> TypeOptions { get; private set; } = new(); // internal storage for display strings
        public List<string> QualityOptions { get; private set; } = new();
        public List<string> RootOptions { get; } = new() { "C", "D", "E", "F", "G", "A", "B" };

        // Current chord object
        public ChordT currentChord = new();
        private const int highestpitch = 80;

        // TempData key for the current chord
        private const string CurrentChordKey = "__currentChord";

        // Маппінг enum -> українська назва
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
                ["ALTQUINT"] = "з понеженою квінтою",
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
        
        public IWebHostEnvironment _environment;
        public string FilePath => Path.Combine(_environment.WebRootPath, "info", "info.txt");

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment)
        { 
            _logger = logger; 
            _environment = environment;
        
        }

        public void OnGet()
        {
            ReadInfo();            

            if (TempData.Peek(MaxCountTempKey) is string maxVal && int.TryParse(maxVal, out var mc))
            {
                MaxCount = Math.Max(2, Math.Min(5, mc));
                MessageL(7, $"MaxCo1unt set to {MaxCount}");
            }

            MessageL(14, "Index OnGet: generating initial chord");
            var chordData = GenerateRandomChord();
            ApplyChordData(chordData); // show to user
                                       // store current shown chord so recognise can validate
            TempData[CurrentChordKey] = JsonSerializer.Serialize(chordData);

            // Prepare UI option lists for initial generated chord (display strings)
            PopulateTypesForGenerated();
            PopulateQualitiesForGenerated();
            SyncLegacyLists();
            CleanOldFiles();


        }

        private void ReadInfo()
        {
            var attempts = System.IO.File.ReadAllText(FilePath);
            RequestCount = int.Parse(attempts);           
        }

        private void CleanOldFiles()
        {
            try
            {
                // sound directory under the app root
                var soundDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "sound");
                if (!System.IO.Directory.Exists(soundDir))
                {
                    MessageL(COLORS.gray, $"CleanOldFiles: directory not found: {soundDir}");
                    return;
                }

                var threshold = DateTime.UtcNow.AddHours(-1);

                foreach (var file in System.IO.Directory.EnumerateFiles(soundDir))
                {
                    try
                    {
                        var lastWriteUtc = System.IO.File.GetLastWriteTimeUtc(file);
                        if (lastWriteUtc < threshold)
                        {
                            System.IO.File.Delete(file);
                            MessageL(COLORS.gray, $"Deleted old sound file: {System.IO.Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception exFile)
                    {
                        ErrorMessageL($"Failed to delete file '{file}': {exFile.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageL($"CleanOldFiles failed: {ex.Message}");
            }
        }

        public IActionResult OnPostPlay()
        {
            MessageL(14, "Index OnPostPlay: processing user play request");
            ReadInfo();
            // Play should use the current chord stored in TempData and must NOT generate a new chord
            RestoreOrGenerateChord();

            PopulateTypesForGenerated();
            PopulateQualitiesForGenerated();
            SyncLegacyLists();
            return Page();
        }

        private void RestoreOrGenerateChord()
        {
            var data = TryGetCurrentChord(keep: true);
            if (data != null)
            {
                ApplyChordData(data);
                MessageL(COLORS.gray, $"Play chord notes: {data.NotesDisplay}");
            }
            else
            {
                var generated = GenerateRandomChord();
                ApplyChordData(generated);
                TempData[CurrentChordKey] = JsonSerializer.Serialize(generated);
            }
        }

        // Handler when user changes count of notes (radio buttons) -> refresh type/quality lists
        public IActionResult OnPostSelect()
        {
            MessageL(14, "Index OnPostSelect: processing user selection change");
            ReadInfo();
            if (Request.Form.TryGetValue(nameof(MaxCount), out var mc) && int.TryParse(mc, out int mcvalue))
                MaxCount = Math.Clamp(mcvalue, 2, 5);

            PopulateTypes(SelectedCount);
            if (SelectedCount == 0)
                _logger.LogWarning("SelectedCount is 0 in OnPostSelect");
            PopulateQualities(SelectedCount, SelectedType);
            SyncLegacyLists();
            // restore current chord info from TempData so UI keeps the ability to play it
            var restored = TryGetCurrentChord(keep: true);
            if (restored != null)
            {
                ApplyChordData(restored);
            }
            return Page();
        }

        // When user posts recognise, convert displayed strings back to internal keys before comparing
        public IActionResult OnPostRecognise()
        {
            MessageL(14, "Index OnPostRecognise: processing user recognise");
            ReadInfo();
            RequestCount++;
            ChordData actual = RestoreChordData();

            if (actual != null)
            {
                // Legacy support: якщо SelectedChord заповнено, копіюємо в SelectedQuality
                if (!string.IsNullOrEmpty(SelectedChord))
                    SelectedQuality = SelectedChord;

                // Маппінг українських назв -> enum ключі
                var typeKey = string.IsNullOrEmpty(SelectedType) ? string.Empty : UkrainianToType.GetValueOrDefault(SelectedType, string.Empty);
                var qualityMap = GetUkrainianToQuality(SelectedCount > 0 ? SelectedCount : actual.Count);
                var qualityKey = string.IsNullOrEmpty(SelectedQuality) ? string.Empty : qualityMap.GetValueOrDefault(SelectedQuality, string.Empty);

                bool ok = SelectedCount == actual.Count &&
                         string.Equals(typeKey, actual.Type, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(qualityKey, actual.Quality, StringComparison.OrdinalIgnoreCase);

                RecogniseOk = ok;
                GuessResult = ok ? "вірно" : "невірно";

                TempData[MaxCountTempKey] = MaxCount.ToString();


                // Показуємо правильну відповідь українською
                var correctTypeUkr = TypeToUkrainian.GetValueOrDefault(actual.Type, actual.Type);
                var correctQualMap = GetQualityToUkrainian(actual.Count);
                var correctQualUkr = correctQualMap.GetValueOrDefault(actual.Quality, actual.Quality);

                RecogniseCorrect = $"{correctTypeUkr} {correctQualUkr} (від ноти {actual.Root}) — ноти: {actual.NotesDisplay}";

                MessageL(ok ? COLORS.green : COLORS.red,
                         $"Recognise: user={SelectedCount}/{typeKey}/{qualityKey} actual={actual.Count}/{actual.Type}/{actual.Quality}");

                // генеруємо відображення введеної користувачем відповіді

                UserAnswer = string.Join(SelectedQuality + " " + SelectedChord);

                // keep current chord in TempData while we display feedback
                TempData.Keep(CurrentChordKey);
            }
            else
            {
                // If no stored current chord, set feedback accordingly
                RecogniseOk = null;
                RecogniseCorrect = "Немає збереженого акорду для перевірки.";
            }

            var chordData = GenerateRandomChord();
            ApplyChordData(chordData);
            TempData[CurrentChordKey] = JsonSerializer.Serialize(chordData);

            PopulateTypes(SelectedCount);
            PopulateQualities(SelectedCount, SelectedType);
            SyncLegacyLists();

            System.IO.File.WriteAllText(FilePath, RequestCount.ToString());

            SelectedCount = 0;
            SelectedType = string.Empty;
            SelectedQuality = string.Empty;


            return Page();
        }

        
        public IActionResult OnPostGuess()
        {
            MessageL(14, "Index OnPostGuess: processing user guess");
            ReadInfo();

            if (!string.IsNullOrEmpty(SelectedChord))
                SelectedQuality = SelectedChord;

            // Маппінг українських назв -> enum ключі
            var typeKey = string.IsNullOrEmpty(SelectedType) ? string.Empty : UkrainianToType.GetValueOrDefault(SelectedType, string.Empty);
            var qualityMap = GetUkrainianToQuality(SelectedCount);
            var qualityKey = string.IsNullOrEmpty(SelectedQuality) ? string.Empty : qualityMap.GetValueOrDefault(SelectedQuality, string.Empty);

            PopulateTypes(SelectedCount);
            PopulateQualities(SelectedCount, SelectedType);
            bool ok = SelectedCount == GeneratedCount &&
             string.Equals(typeKey, GeneratedType, StringComparison.OrdinalIgnoreCase) &&
   string.Equals(qualityKey, GeneratedQuality, StringComparison.OrdinalIgnoreCase);
            GuessResult = ok ? "вірно" : "невірно";
            MessageL(ok ? COLORS.green : COLORS.red, $"Guess result: {GuessResult} (user: {SelectedCount}/{typeKey}/{qualityKey} actual: {GeneratedCount}/{GeneratedType}/{GeneratedQuality})");
            TempData.Keep(CurrentChordKey);
            SyncLegacyLists();
            return Page();
        }

        public IActionResult OnPostMax()
        {
            MessageL(14, $"Index OnPostMax: processing user max count change to {MaxCount}");
            ReadInfo();

            var chordData = GenerateRandomChord();
            ApplyChordData(chordData);
            TempData[CurrentChordKey] = JsonSerializer.Serialize(chordData);

            // Refresh UI lists
            PopulateTypesForGenerated();
            PopulateQualitiesForGenerated();
            SyncLegacyLists();

            return Page();
        }


        public IActionResult OnPostTimbre()
        {
            MessageL(14, $"Index OnPostTimbre: set to {Timbre}");
            ReadInfo();

            // Refresh UI lists
            PopulateTypesForGenerated();
            PopulateQualitiesForGenerated();
            SyncLegacyLists();

            var restored = TryGetCurrentChord(keep: true);
            if (restored == null)
            {
                MessageL(4, "OnPostTimbre: no chord in TempData to restore");
                return Page();
            }

            MessageL(8, "start restoring");
            var chord = RestoreChord(restored);
            if (chord is null)
            {
                MessageL(4, "OnPostTimbre: RestoreChord returned null");
                return Page();
            }

            // Save WAV using selected timbre and update stored metadata
            TIMBRE timbreEnum = GetTimbre();
            MessageL(8, $"save wave with timbre {timbreEnum}");
            string fullPath = GetFullPath();
            chord.SaveWave(fullPath, timbreEnum);
            string rel = RelativeFromFull(fullPath);
            MessageL(8, $"WAV saved to {rel}");

            // Update stored chord metadata and TempData, then apply to page via ApplyChordData
            restored.FileRelative = rel;
            TempData[CurrentChordKey] = JsonSerializer.Serialize(restored);

            // Use ApplyChordData to set Generated* properties consistently
            ApplyChordData(restored);

            return Page();
        }
        //генереує випадкове співзвуччя та записує аудіофайл
        private ChordData GenerateRandomChord()
        {
            var rnd = new Random();
            int count = rnd.Next(2, MaxCount); //кількість звуків (2..5)
            string typeKey = string.Empty;
            string qualityKey = string.Empty;
            string rootLetter = RootOptions[rnd.Next(RootOptions.Count)];
            Note root = new(GetNoteByLetter(rootLetter), ALTER.NATURAL, 1);
            var chord = new ChordT();

            if (count == 2)
            {
                var intervalTypes = new[] { "SECUNDA", "TERZIA", "QUARTA", "QUINTA", "SEKSTA", "SEPTYMA", "OCTAVA" };
                typeKey = intervalTypes[rnd.Next(intervalTypes.Length)];

                INTERVALS interval = Enum.TryParse<INTERVALS>(typeKey, out var intr) ? intr : INTERVALS.SECUNDA;

                var perfectSet = new[] { INTERVALS.PRIMA, INTERVALS.QUARTA, INTERVALS.QUINTA, INTERVALS.OCTAVA };
                QUALITY qual;
                if (perfectSet.Contains(interval))
                {
                    qual = QUALITY.PERFECT;
                    qualityKey = "PERFECT"; // <- додано, щоб actual.Quality містив ключ
                }
                else
                {
                    var qopts = new[] { "MAJ", "MIN" };
                    qualityKey = qopts[rnd.Next(qopts.Length)];
                    qual = qualityKey == "MIN" ? QUALITY.MIN : QUALITY.MAJ;
                }

                var note2 = (Note)root.Clone();
                note2.Transpose(interval, qual, DIR.UP);
                chord.AddNote(root);
                chord.AddNote(note2);
                if (chord.Notes[0].AbsPitch() == chord.Notes[1].AbsPitch())
                {
                    chord.Notes[1].OctUp();
                    MessageL(12, "perform oct up");
                }
            }
            else if (count == 3)
            {
                var triTypes = new[] { "TRI", "SEXT", "QSEXT" };
                typeKey = triTypes[rnd.Next(triTypes.Length)];
                string[] qualitiesForGen = (typeKey == "SEXT" || typeKey == "QSEXT")
                   ? new[] { "MAJ", "MIN", "DIM" }
                   : new[] { "MAJ", "MIN", "AUG", "DIM" };
                qualityKey = qualitiesForGen[rnd.Next(qualitiesForGen.Length)];
                TRIADS triadQuality = Enum.TryParse<TRIADS>(qualityKey, out var tq) ? tq : TRIADS.MAJ;
                chord.TriadChord(root, triadQuality);
                ApplyTriadInversion(chord, typeKey);
            }
            else if (count == 4)
            {
                var septTypes = new[] { "SEPT", "QUINTS", "TERZQ", "SEC" };
                typeKey = septTypes[rnd.Next(septTypes.Length)];
                // Тимчасово вилучаємо ALTPRIM та ALTQUINT з генерації
                var septQualities = Enum.GetNames(typeof(SEPTS))
              .Where(q => q != "ALTPRIM" && q != "ALTQUINT")
     .ToArray();
                qualityKey = septQualities[rnd.Next(septQualities.Length)];
                SEPTS septQuality = Enum.TryParse<SEPTS>(qualityKey, out var sq) ? sq : SEPTS.MAJMAJ;
                chord.SeventhChord(root, septQuality);
                ApplySeventhInversion(chord, typeKey);
            }
            else if (count == 5)
            {
                var ninthTypes = new[] { "NONACORD", "NONACORD_1i", "NONACORD_2i", "NONACORD_3i", "NONACORD_4i", "CORD69" };
                typeKey = ninthTypes[rnd.Next(ninthTypes.Length)];
                var ninthQualities = Enum.GetNames(typeof(NINTHS)).Where(n => n != "OTHER").ToArray();
                qualityKey = ninthQualities.Length > 0 ? ninthQualities[rnd.Next(ninthQualities.Length)] : string.Empty;
                if (!string.IsNullOrEmpty(qualityKey) && Enum.TryParse<NINTHS>(qualityKey, out var nq))
                    chord.NinthChord(root, nq);
                ApplyNinthInversion(chord, typeKey);
            }
            if (chord.GetHighestMidiNote() > highestpitch)
            {
                MessageL(COLORS.gray, "Adjusting chord octave down to fit pitch limit");
                chord.OctDown();
            }
            else
            {
                MessageL(COLORS.gray, $"no changes, highest = {chord.GetHighestMidiNote()}");

            }
            TIMBRE timbreEnum = GetTimbre();
            string fullPath = GetFullPath();

            chord.SaveWave(fullPath, timbreEnum);
            try
            {
                var analysis = Music.AudioDiagnostics.AnalyzeWav(fullPath);
                MessageL(COLORS.cyan, $"Audio analysis: peak={analysis.Peak:F3} ( {analysis.PeakDb:F1} dBFS ), rms={analysis.Rms:F3} ( {analysis.RmsDb:F1} dBFS )");
            }
            catch (Exception ex)
            {
                ErrorMessageL($"Audio analysis failed: {ex.Message}");
            }

            string rel = RelativeFromFull(fullPath);
            string notesDisplay = string.Join(", ", chord.Notes.Select(n => n.GetName()));
            string pitchesdisplay = string.Join(", ", chord.Notes.Select(n => n.AbsPitch()));

            // Log generated notes and chord type/quality
            MessageL(COLORS.gray, $"Generated chord ({typeKey}/{qualityKey}) notes: {notesDisplay} pitches: {pitchesdisplay}");

            // Build JSON payload for client-side playback: { notes: [{frequency,duration}], type, quality, count, root, file }
            var noteObjects = chord.Notes.Select(n => new
            {
                frequency = n.AbsPitch() < 0 ? 0 : Pitch_to_hz(n.AbsPitch()),
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
                NotesJson = notesJson,                
            };
        }

        private static string GetFullPath()
        {
            string directory = Path.Combine("wwwroot", "sound");
            Directory.CreateDirectory(directory);

            // find next sequential filename exampleN.wav
            int nextIndex = GetNextIndex(directory);

            string filename = $"example{nextIndex}.wav";
            string fullPath = System.IO.Path.Combine(directory, filename);
            return fullPath;
        }

        private static int GetNextIndex(string directory)
        {
            int nextIndex = 1;
            try
            {
                var files = Directory.GetFiles(directory, "example*.wav");
                var rx = new Regex(@"example(\d+)\.wav$", RegexOptions.IgnoreCase);
                int max = 0;
                foreach (var f in files)
                {
                    var name = System.IO.Path.GetFileName(f);
                    var m = rx.Match(name);
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var val))
                    {
                        if (val > max) max = val;
                    }
                }
                nextIndex = max + 1;
            }
            catch (Exception ex)
            {
                // if directory read fails, fallback to1
                GrayMessageL("Failed to enumerate existing files: " + ex.Message);
                nextIndex = 1;
            }

            return nextIndex;
        }

        private TIMBRE GetTimbre()
        {
            TIMBRE timbreEnum;
            if (string.IsNullOrWhiteSpace(Timbre) || !Enum.TryParse<TIMBRE>(Timbre, ignoreCase: true, out timbreEnum))
            {
                timbreEnum = TIMBRE.sin;
                MessageL(COLORS.gray, $"Timbre parse failed or empty ('{Timbre}'), defaulting to {timbreEnum}");
            }
            else
            {
                MessageL(COLORS.gray, $"Timbre parsed: {timbreEnum}");
            }

            return timbreEnum;
        }

        // Apply generated chord data to properties for UI display
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
            if (idx >= 0)
            {
                return "/" + path.Substring(idx + 7).TrimStart('/', '\\').Replace('\\', '/');
            }
            return string.Empty;
        }

        private void PopulateTypes(int count)
        {
            TypeOptions.Clear();
            var keys = count switch
            {
                2 => new[] { "SECUNDA", "TERZIA", "QUARTA", "QUINTA", "SEKSTA", "SEPTYMA", "OCTAVA" },
                3 => new[] { "TRI", "SEXT", "QSEXT" },
                4 => new[] { "SEPT", "QUINTS", "TERZQ", "SEC" },
                5 => new[] { "NONACORD", "NONACORD_1i", "NONACORD_2i", "NONACORD_3i", "NONACORD_4i" },
                _ => Array.Empty<string>()
            };

            foreach (var key in keys)
            {
                TypeOptions.Add(TypeToUkrainian.GetValueOrDefault(key, key));
            }
        }
        private void PopulateTypesForGenerated() => PopulateTypes(GeneratedCount);

        // заповнює спадний список якостей інтервалів
        private void PopulateQualities(int count, string? typeUkr = null)
        {
            QualityOptions.Clear();
            if (count <= 0) return;

            // If asking about intervals of two sounds (count==2) and the selected type is a perfect interval
            // (кварта / квінта / октава) then only show "чиста"
            if (count == 2 && !string.IsNullOrWhiteSpace(typeUkr))
            {
                var perfectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    TypeToUkrainian.GetValueOrDefault("QUARTA", "кварта"),
                    TypeToUkrainian.GetValueOrDefault("QUINTA", "квінта"),
                    TypeToUkrainian.GetValueOrDefault("OCTAVA", "октава")
                };

                if (perfectNames.Contains(typeUkr))
                {
                    QualityOptions.Add("чиста");
                    return;
                }
            }

            // Fallback: populate usual qualities for the given count
            var map = GetQualityToUkrainian(count);
            QualityOptions.AddRange(map.Values);
        }
        private void PopulateQualitiesForGenerated()
        {
            var genTypeUkr = TypeToUkrainian.GetValueOrDefault(GeneratedType, string.Empty);
            PopulateQualities(GeneratedCount, genTypeUkr);
        }

        private void SyncLegacyLists()
        {
            Options = QualityOptions.ToList();
            Types = TypeOptions.ToList();
            if (!string.IsNullOrEmpty(SelectedQuality)) SelectedChord = SelectedQuality;
        }

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
            public string NotesJson { get; set; } = string.Empty;
        }

        private ChordData RestoreChordData()
        {
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

            return actual;
        }

        private ChordData TryGetCurrentChord(bool keep = false)
        {
            if (TempData.TryGetValue(CurrentChordKey, out var curObj) && curObj is string curJson && !string.IsNullOrWhiteSpace(curJson))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<ChordData>(curJson);
                    if (data != null && keep)
                    {
                        TempData.Keep(CurrentChordKey);
                    }
                    return data;
                }
                catch (Exception ex)
                {
                    ErrorMessageL("Recognise deserialize error: " + ex.Message);
                }
            }

            return null;
        }


        private ChordT RestoreChord(ChordData cd)
        {
            MessageL(8, $"start RestoreChord method");
            try
            {
                var notesDisplay = cd.NotesDisplay;
                string normalized = NotationHelpers.NormalizeNotesDisplay(notesDisplay, Notation.us);
                
                MessageL(8, $"sent to chord: {normalized}");
                return new ChordT(normalized);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("RestoreChord failed: {Message}", ex.Message);
                return null;
            }
        }
    }
}
