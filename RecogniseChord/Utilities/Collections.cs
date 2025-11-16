using Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Music.Adjustment;
using static Music.ChordPermutation;


namespace Music
{
    public static class Collections
    {

        /// <summary>
        /// ОКРЕМІ НОТИ
        /// </summary>
        /// <returns></returns>
        public static Scale AllPosibleNotes()
        {
            Note Do = new("c");
            Scales chrom = new(Do, MODE.chrome);
            Scale extended = (Scale)chrom.Clone();

            foreach (Note note_ in chrom.Notes)
            {
                if (note_.CheckIfSharpable())
                {
                    note_.EnharmonizeSharp();
                    extended.AddNote(note_);
                }
                if (note_.CheckIfFlatable())
                {
                    note_.EnharmonizeFlat();
                    extended.AddNote(note_);
                }
            }
            extended = ExcludeDoubles(extended);
            extended.SortByPitch();

            return extended;

        }


        // ТРИЗВУКИ 3-голосся


        public static List<ChordT> Triads_Inversions_3voices_AllTonals(int octave = 0)
        {
            ChordT majtriad = new(), mintriad = new();
            List<ChordT> majorchords = new(), minorchords = new(), alltriads = new();
            Note note = new(NOTES.DO, ALTER.NATURAL, octave);
            majtriad.MajorTriad(note);
            majorchords = TransposePermute(majtriad);
            mintriad.MinorTriad(note);
            minorchords = TransposePermute(mintriad);
            alltriads.AddRange(majorchords);
            alltriads.AddRange(minorchords);
            return alltriads;
        }

        
        
        // ТРИЗВУКИ класичне 4-голосся

        // в межах однієї тональності 

        public static List<ChordT> Triads_4voice_InTonal(string input, MODE mode = MODE.dur)
        {
            ChordT initial = new();
            if (mode == MODE.dur) initial.MajorTriad(input);
            else initial.MinorTriad(input);
            Note basenote = (Note)initial.Notes[0].Clone();
            List<ChordT> triad = initial.PermuteList(basenote.Oct);
            List<ChordT> alltriads = new();
            foreach (ChordT ch in triad)
            {
                alltriads.Add(basenote + ch);
            }

            alltriads = SortByRange(alltriads);
            return alltriads;
        }

        public static List<ChordT> Triads_4voice_InTonal(Note note, MODE mode = MODE.dur)
        {
            // тризвуки (без обернень)
            ChordT initial = new();
            if (mode == MODE.dur) initial.MajorTriad(note);
            else initial.MinorTriad(note);
            Note basenote = (Note)initial.Notes[0].Clone();
            List<ChordT> triad = initial.PermuteList(basenote.Oct);
            List<ChordT> alltriads = new();
            foreach (ChordT ch in triad)
            {
                alltriads.Add(basenote + ch);
            }

            alltriads = SortByRange(alltriads);
            return alltriads;
        }


        public static List<ChordT> Triads_Inversions_4voice_InTonal(ChordT initial, MODE mode = MODE.dur)
        { // тризвуки і обернення у заданій тональності
            
            Note basenote = (Note)initial.Notes[0].Clone();
            Note terzia = (Note)initial.Notes[1].Clone();
            Note quinta = (Note)initial.Notes[2].Clone();
            List<ChordT> triad = initial.PermuteList(basenote.Oct);
            List<ChordT> alltriads = new();
            List<ChordT> sixthchord = new();
            List<ChordT> foursixthchord = new();
            foreach (ChordT ch in triad)
            {
                alltriads.Add(basenote + ch);
            }

            alltriads = SortByRange(alltriads);
            ChordT gcc = new(basenote, quinta, basenote);
            ChordT ggc = new(quinta, quinta, basenote);
            List<ChordT> Gcc = gcc.PermuteList(basenote.Oct);
            Gcc = SortByRange(Gcc);
            List<ChordT> Ggc = ggc.PermuteList(basenote.Oct);
            Ggc = SortByRange(Ggc);
            foreach (ChordT ch in Gcc)
            {
                sixthchord.Add(terzia + ch);
            }
            foreach (ChordT ch in Ggc)
            {
                sixthchord.Add(terzia + ch);
            }
            sixthchord = SplitDoubles(sixthchord);
            sixthchord = ExcludeDoubles(sixthchord);
            foreach (ChordT ch in triad)
            {
                foursixthchord.Add(quinta + ch);
            }
            alltriads.AddRange(sixthchord);
            alltriads.AddRange(foursixthchord);
            return alltriads;
        }

        public static List<ChordT> Triads_Inversions_4voices_InTonal(string input, MODE mode = MODE.dur)
        {
            ChordT initial = new();
            if (mode == MODE.dur) initial.MajorTriad(input);
            else initial.MinorTriad(input);
            return Triads_Inversions_4voice_InTonal(initial, mode);
        }

        public static List<ChordT> Triads_Inversions_4voices_InTonal(Note note, MODE mode = MODE.dur)
        {
            ChordT initial = new();
            if (mode == MODE.dur) initial.MajorTriad(note);
            else initial.MinorTriad(note);
            return Triads_Inversions_4voice_InTonal(initial, mode);
        }

        // в усіх тональностях 

        public static List<ChordT> Triads_Inversions_4voices_AllTonals(int octave = 0)
        { // тризвуки та їх обернення в усіх тональностях
            List<ChordT> alltriads = new();
            Note note = new(NOTES.DO, ALTER.NATURAL, octave);
            List<ChordT> cdur = Triads_Inversions_4voices_InTonal(note);
            List<ChordT> cmoll = Triads_Inversions_4voices_InTonal(note, MODE.moll);
            alltriads.AddRange(cdur);
            alltriads.AddRange(cmoll);

            alltriads = Interval.AllTonalities(alltriads);

            return alltriads;
        }

        public static List<ChordT> Triads_Inversions_4voiсes_AllTonals(string input)
        {// тризвуки та їх обернення в усіх тональностях

            List<ChordT> alltriads = new();
            List<ChordT> majtriad = Triads_Inversions_4voices_InTonal(input);
            List<ChordT> mintriad = Triads_Inversions_4voices_InTonal(input, MODE.moll);
            alltriads.AddRange(majtriad);
            alltriads.AddRange(mintriad);

            alltriads = Interval.AllTonalities(alltriads);

            return alltriads;
        }

        public static List<ChordT> TRIADS()
        {// тризвуки (без обернень) в усіх тональностях в класисчному діапазоні

            List<ChordT> set = new();
            List<ChordT> major = Triads_4voice_InTonal("f,,");
            List<ChordT> minor = Triads_4voice_InTonal("f,,", MODE.moll);
            set.AddRange(major);
            set.AddRange(minor);
            set = Interval.AllTonalities(set);
            set = AddEnharmonized(set);
            set = AddBaseOctDownList(set);
            set = AddOctUpList(set);
            set = AddOctUpList(set);
            set = SortByBase(set);
            set = FilterClassic(set);
            set = set.Distinct().ToList();
            return set;
        }
        public static List<ChordT> ALLTRIADS()
        {// тризвуки та їх обернення в усіх тональностях в класисчному діапазоні

            List<ChordT> set = Triads_Inversions_4voices_AllTonals();
            set = AddEnharmonized(set);
            set = AddBaseOctDownList(set);
            set = AddOctUpList(set);
            set = AddOctUpList(set);
            set = SortByBase(set);
            set = FilterClassic(set);
            set = set.Distinct().ToList();
            return set;
        }

        public static List<ChordT> ALLSEVENTH()
        {
            List<ChordT> set = SeventhChords_AllTypesAllTonal();
            set = AddBaseOctDownList(set);
            set = AddOctUpList(set);
            set = AddOctUpList(set);
            set = FilterClassic(set);
            set = AddEnharmonized(set);
            set = FilterTrippleAlter(set);

            return set;
        }

        public static List<ChordT> ALLSEVENTHandTRIADS()
        {
            List<ChordT> set = new();
            List<ChordT> set1 = SeventhChords_AllTypesAllTonal();
            List<ChordT> set2 = Triads_Inversions_4voices_AllTonals();
            set.AddRange(set1);
            set.AddRange(set2);
            set = AddBaseOctDownList(set);
            set = AddOctUpList(set);
            set = AddOctUpList(set);
            set = FilterClassic(set);
            set = AddEnharmonized(set);
            set = FilterTrippleAlter(set);
            return set;
        }


        // СПЕТАКОРДИ 

        public static List<ChordT> SeventhChords_InTonal(Note note, SEPTS mode)
        {
            ChordT chord = new();
            chord.SeventhChord(note, mode);
            return chord.PermuteList();
        }

        public static List<ChordT> SeventhChordsAllTypes_InTonal(Note note)
        {
            List<ChordT> chords = new();
            foreach (SEPTS mode in Enum.GetValues(typeof(SEPTS)))
            {
                ChordT chord = new(); chord.SeventhChord(note, mode);
                chords.Add(chord);
            }
            return chords;
        }

        public static List<ChordT> SeventhChords_AllTonal(SEPTS mode = default)
        {
            ChordT chord = new();
            Note note = new Note("");
            chord.SeventhChord(note, mode);
            return chord.TransposePermuteList();
        }

        public static List<ChordT> SeventhChords_AllTypesAllTonal()
        {
            List<ChordT> chords = new();
            Note note = new Note("f,");  
            foreach (SEPTS mode in Enum.GetValues(typeof(SEPTS)))
            {
                ChordT chord = new(); chord.SeventhChord(note, mode);
                List<ChordT> temp = chord.TransposePermuteList();
                chords.AddRange(temp);
            }
           
            return chords;           
        }


    }
}
