using System.Text.RegularExpressions;
using Music;
using NuGet.Protocol.Plugins;
using static Music.Messages;

namespace Music
{
    public static class NotationHelpers
    {
        // Convert solfège like "do", "re", "mi", "fa", "sol", "la", "si"
        // optionally followed by accidentals/octave/duration (e.g. "re#", "solis'4")
        // into project-accepted key names ("c","d","e","f","g","a","b" or "h" for EU).
        public static string SolfegeToKey(string input, Notation? notation = Notation.eu)
        {
            MessageL(8, $"solfegeToKey: {input} notation: {notation}");
            if (string.IsNullOrWhiteSpace(input)) return input ?? string.Empty;

            input = input.Trim().ToLowerInvariant();

            // match solfege prefix and everything else (accidentals/octave/duration)
            var m = Regex.Match(input, @"^(do|re|mi|fa|sol|la|si)(.*)$", RegexOptions.Compiled);
            if (!m.Success) return input; // nothing to convert

            var sol = m.Groups[1].Value;
            var rest = m.Groups[2].Value; // preserve accidentals / octave / duration postfix

            var effectiveNotation = notation ?? Notation.eu;

            string key = sol switch
            {
                "do" => "c",
                "re" => "d",
                "mi" => "e",
                "fa" => "f",
                "sol" => "g",
                "la" => "a",
                "si" => effectiveNotation == Notation.eu ? "h" : "b",
                _ => sol
            };
            MessageL(8, $"return: {key + rest}");
            return key + rest;
        }

        // Normalize a whole notesDisplay string with space-separated tokens
        public static string NormalizeNotesDisplay(string notesDisplay, Notation? notation = null)
        {   
            MessageL(9, $"NormalizeNotesDisplay method starts with {notesDisplay}");
            if (string.IsNullOrWhiteSpace(notesDisplay)) return notesDisplay ?? string.Empty;
            var parts = notesDisplay.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                parts[i] = SolfegeToKey(parts[i], notation);
            MessageL(8, $"NormalizeNotesDisplay method returns {string.Join(' ', parts)}");
            return string.Join(' ', parts);
        }
    }
}