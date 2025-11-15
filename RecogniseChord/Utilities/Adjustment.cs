using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using static Music.Globals;
using static Music.Messages;
using static Music.Menues;
using System.Xml.Serialization;
namespace Music
{

    public delegate List<T> Filter<T>(List<T> chords, Note melody) where T : Chord;


    public static class Adjustment
    {
        public static T Adjust<T>(T ch, int octave = 1) where T : Scale
        {
            for (int i = 0; i < ch.Notes.Count - 1; i++)
            {
                while (ch.Notes[0].Oct > octave) ch.OctDown();
                while (ch.Notes[0].Oct < octave) ch.OctUp();
                ch.Adjust(octave);
            }
            return ch;
        }

        public static T[] Adjust<T>(T[] ch, int octave = 1) where T : Scale
        {
            foreach (T ch2 in ch)
                for (int i = 0; i < ch2.Notes.Count - 1; i++)
                {
                    while (ch2.Notes[0].Oct > octave) ch2.OctDown();
                    while (ch2.Notes[0].Oct < octave) ch2.OctUp();
                    ch2.Adjust(octave);
                }
            return ch;
        }

        public static T[] Adjust<T>(ref T[] ch, int octave = 1) where T : Scale
        {
            foreach (T ch2 in ch)
                for (int i = 0; i < ch2.Notes.Count - 1; i++)
                {
                    while (ch2.Notes[0].Oct > octave) ch2.OctDown();
                    while (ch2.Notes[0].Oct < octave) ch2.OctUp();
                    ch2.Adjust(octave);
                }
            return ch;
        }

        public static List<T> Adjust<T>(List<T> ch, int octave = 1) where T : Scale
        {
            foreach (T ch2 in ch)
                for (int i = 0; i < ch2.Notes.Count - 1; i++)
                {
                    while (ch2.Notes[0].Oct > octave) ch2.OctDown();
                    while (ch2.Notes[0].Oct < octave) ch2.OctUp();
                    ch2.Adjust(octave);
                }
            return ch;
        }

        public static void Adjust<T>(ref List<T> ch, int octave = 1) where T : Scale
        {
            foreach (T ch2 in ch)
                for (int i = 0; i < ch2.Notes.Count - 1; i++)
                {
                    while (ch2.Notes[0].Oct > octave) ch2.OctDown();
                    while (ch2.Notes[0].Oct < octave) ch2.OctUp();
                    ch2.Adjust();
                }
        }



        public static List<T> AddBaseOctDownList<T>(List<T> chords) where T : Scale
        {
            List<T> list = Clone(chords);
            List<T> list2 = Clone(list);
            BaseOctDown(list2);
            list.AddRange(list2);
            return list;
        }

        public static List<T> AddOctDownList<T>(List<T> chords) where T : Scale
        {
            List<T> list = Clone(chords);
            List<T> list2 = Clone(list);
            Interval.OctDown(list2);
            list.AddRange(list2);
            return list;
        }

        public static List<T> AddOctUpList<T>(List<T> chords) where T : Scale
        {
            List<T> list = Clone(chords);
            List<T> list2 = Clone(list);
            Interval.OctUp(list2);
            list.AddRange(list2);
            return list;
        }


        public static List<T> AddEnharmonized<T>(List<T> chords) where T : Scale
        {
            List<T> list = Clone(chords);
            List<T> result = Clone(chords);
            foreach (T t in list)
            {
                if (t.CheckIfSharpable() == true)
                {
                    T temp = (T)t.Clone();
                    temp.EnharmonizeToSharp();
                    result.Add(temp);
                }
                else if (t.CheckIfFlatable() == true)
                {
                    T temp = (T)t.Clone();
                    temp.EnharmonizeToFlat();
                    result.Add(temp);
                }
            }
            result = result.Distinct().ToList();    
            //result = ExcludeDoubles(result);
            return result;
        }

        public static void BaseOctDown<T>(List<T> ch) where T : Scale
        {
            foreach (T ch2 in ch)
                ch2.Notes[0].OctDown();
        }

        private static List<T> Clone<T>(List<T> list) where T : Scale 
        {
            List<T> result = new();

            foreach (T ch2 in list)
            {
                T temp = (T)ch2.Clone();
                result.Add(temp);
            }
            return result;
        }

        private static List<Note> Clone(List<Note> list) 
        {
            List<Note> result = new();

            foreach (Note note in list)
            {
                Note temp = (Note)note.Clone();
                result.Add(temp);
            }
            return result;
        }

        public static List<T> CommonBase<T>(List<T> chords, Note note) where T : Chord
        { // транспонує окремі акорди так щоб усі акорди у масиві мали спільний бас
            List<T> adjusted = new();
            List<T> templist = Clone(chords);
            foreach (var chord in templist)
            {
                T temp = (T)chord.Clone();
                temp.TransposeToLowNote(note);
                adjusted.Add(temp);
            }
            return ToOctave(adjusted, note.Oct);
        }

        public static List<T> CommonBase<T>(List<T> chords, string note) where T : Chord
        {
            List<T> templist = Clone(chords);
            try
            {
                if (note is null) throw new IncorrectNote("incorrect tagret note");
                Note target = new(note);
                return CommonBase(templist, target);
            }
            catch (IncorrectNote e)
            {
                Messages.Message(12, e.Message);
                return chords;
            }
        }

        public static List<T> CommonMelody<T>(List<T> chords, Note note) where T : Chord
        {//всі акорди приводить до заданого мелодичного тону
            List<T> adjusted = new();
            List<T> templist = Clone(chords);
            foreach (var chord in templist)
            {
                T temp = (T)chord.Clone();
                temp.TransposeToHighNote(note);
                //if (temp.Notes[^1].Oct == 1) Console.WriteLine("\n!" + adjusted[^1]);
                //else Console.Write("+");
                adjusted.Add(temp);
            }
            return adjusted;
        }

        public static List<T> CommonMelody<T>(List<T> chords, string note) where T : Chord
        {

            try
            {
                if (note is null) throw new IncorrectNote("incorrect tagret note");
                Note target = new(note);
                return CommonMelody(chords, target);
            }
            catch (IncorrectNote e)
            {
                Messages.Message(12, e.Message);
                return chords;
            }
        }


        public static void Enharmonize<T>(T scale, int mode = 0) where T : Scale
        {

            foreach (Note note in scale.Notes)
            {
                switch (mode)
                {
                    default: note.EnharmonizeSmart(); break;
                    case -1: note.EnharmonizeFlat(); break;
                    case 1: note.EnharmonizeSharp(); break;
                    case 2: note.EnharmonizeSharp(); break;
                    case 3: note.EnharmonizeDoubles(); break;
                }
            }
        }
        public static void Enharmonize<T>(T[] scales, int mode = 0) where T : Scale
        {

            foreach (T scale in scales)
                Enharmonize(scale, mode);
        }
        public static void Enharmonize<T>(List<T> scales, int mode = 0) where T : Scale
        {

            foreach (T scale in scales)
                Enharmonize(scale, mode);
        }

        public static void Enharmonize<T>(List<List<T>> scales, int mode = 0) where T : Scale
        {

            foreach (List<T> scale in scales)
                Enharmonize(scale, mode);
        }
        public static void Enharmonize<T>(List<T>[] scales, int mode = 0) where T : Scale
        {

            foreach (List<T> scale in scales)
                Enharmonize(scale, mode);
        }

        public static void Enharmonize<T>(List<T[]> scales, int mode = 0) where T : Scale
        {

            foreach (T[] scale in scales)
                Enharmonize(scale, mode);
        }

        public static void EnharmonizeToSharp<T>(List<T> scales, bool nodoubles = true) where T : Scale
        {
            if (nodoubles)
            {
                foreach (T scale in scales)
                    if (scale.CheckIfSharpable() == false) return;
            }
            else
            {
                foreach (T scale in scales)
                    scale.EnharmonizeToSharp(nodoubles);
            }
        }

        public static void EnharmonizeToFlat<T>(List<T> scales, bool nodoubles = true) where T : Scale
        {
            if (nodoubles)
            {
                foreach (T scale in scales)
                    if (scale.CheckIfFlatable() == false) return;
            }
            else
            {
                foreach (T scale in scales)
                    scale.EnharmonizeToFlat(nodoubles);
            }
        }

        public static List<T> EnharmonizeToSharps<T>(List<T> scales, bool nodoubles = true) where T : Scale
        {
            List<T> values = new();
            values = Clone(scales);
            EnharmonizeToSharp(values, nodoubles);
            return values;
        }

        public static List<T> EnharmonizeToFlats<T>(List<T> scales, bool nodoubles = true) where T : Scale
        {
            List<T> values = new();
            values = Clone(scales);
            EnharmonizeToFlat(values, nodoubles);
            return values;
        }

        public static List<T> ExcludeDoubles<T>(List<T> chords) where T : Scale
        {
            if (chords.Count() == 0) return chords;
            bool trigger = true;
            List<T> reslut = new();            
            reslut.Add(chords[0]);
            for (int i = 1; i < chords.Count; i++)
            {
                for (int j = i + 1; j < chords.Count; j++)
                {
                    trigger = true;
                    if (chords[i] == chords[j]) { trigger = false; break; }
                }
                if (trigger == true) reslut.Add(chords[i]);
            }
            return reslut;
        }

        public static T ExcludeDoubles<T>(T scale) where T : Scale, new()
        {
            if (scale.Notes.Count() == 0) return scale;
            bool trigger = true;
            T newscale = new();

            //MessageL(COLORS.darkred, "Initial scale:");
           // scale.Display();

            newscale.AddNote(scale.Notes[0]);
            for (int i = 1; i < scale.Notes.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    trigger = true;
                    //Message(8, scale.Notes[i] + " vs " + scale.Notes[j]);
                    if (scale.Notes[i] == scale.Notes[j]) { trigger = false;/* Message(8, "false\n"); */break; }
                    //else Message(8, "true\n");
                }
                if (trigger == true) { /*Message(8, "true\n"); */newscale.AddNote(scale.Notes[i]); }
               // else Message(8, "false\n");
            }
           // MessageL(COLORS.green, "New scale:");
            //scale.Display();
            
            return newscale;
        }


        public static List<T> FilterByRange<T>(List<T> chords, Note low, Note high) where T : Scale
        {//повертає акорди в межає вказаного діапазону
            bool ifadd = true;
            List<T> temp = new();
            foreach (T chord in chords)
            {
                foreach (Note nt in chord.Notes)
                {
                    if (nt.AbsPitch() < low.AbsPitch() || nt.AbsPitch() > high.AbsPitch())
                    { ifadd = false; break; }
                    else ifadd = true;
                }
                if (ifadd == true) temp.Add(chord);
            }
            return temp;
        }

        public static List<T> FilterByVoiceRange<T>(List<T> chords, int index, Note low, Note high) where T : Scale
        {//повертає акорди, в якому заданий голос знаходиться в межає вказаного діапазону
            try
            {
                bool ifadd = true;
                List<T> temp = new();
                foreach (T chord in chords)
                {
                    if (chord.Notes.Count <= index) throw new IncorrectNote(toomuchnotes());
                    if (chord.Notes[index].AbsPitch() < low.AbsPitch() || chord.Notes[index].AbsPitch() > high.AbsPitch())
                    {
                        ifadd = false;
                        //if(index == 1) Console.WriteLine(chord.Notes[index] + " vs " +  low + " vs " + high);
                        // Console.ReadKey();                       

                    }
                    else ifadd = true;
                    if (ifadd == true) temp.Add(chord);
                }
                return temp;
            }
            catch (IncorrectNote e)
            {
                Message(12, e.Message);
                return chords;
            }
        }

        public static List<T> FilterClassic<T>(List<T> chords) where T : Scale
        {//повертає акорди, в якому заданий голос знаходиться в межає вказаного діапазону
            bool ifadd = true;
            //Прописані в Globals
            //Note SopranoH = new Note("a'"), SopranoL = new Note("c"), AltoH = new Note("e'"), AltoL = new Note("g,"),
            //    TenorH = new Note("a"), TenorL = new Note("c,"), BaseH = new Note("d"), BaseL = new Note("f,,");

            try
            {
                chords = FilterByVoiceRange(chords, 0, BaseL, BaseH);
                chords = FilterByVoiceRange(chords, 1, TenorL, TenorH);
                chords = FilterByVoiceRange(chords, 2, AltoL, AltoH);
                chords = FilterByVoiceRange(chords, 3, SopranoL, SopranoH);
            }
            catch (IncorrectNote e)
            {
                Message(12, e.Message);
                return chords;
            }

            return chords;
        }

        public static List<T> FilterTrippleAlter<T>(List<T> chords) where T : Chord
        { // фільтр потрійних дубль-знаків
            List<T> result = new();

            foreach (T t in chords)
            {
                bool add = true;
                foreach (Note note in t.Notes)
                {
                    if (note.Alter > 2 || note.Alter < -2)
                        add = false; continue;
                }
                if (add) result.Add(t);
            }
            return result;
        }

        public static bool ChoirRange<T>(T chord) where T : Scale
        {//повертає акорди, в якому заданий голос знаходиться в межає вказаного діапазону
            if (chord.Notes.Count < 4) throw new IncorrectNote("The function choirRange is intended for 4-voiced harmony");
            List<bool> conditions =  new() { chord.Notes[0].IfBase(), chord.Notes[1].IfTenor() , chord.Notes[^2].IfAlto() , chord.Notes[^1].IfSoprano() };
            return conditions.All(x => x);
        }


        public static List<T> FilterToMelody<T>(List<T> chords, Note melody) where T : Chord
        {
            List<T> result = new();
            foreach (T chord in chords)
            {
                if (melody.Equals(chord.Notes[^1]))
                { result.Add(chord); }
            }
            return result;
        }

        public static List<T> FilterToSharpness<T>(List<T> chords, float minsharpness, float maxsharpness) where T : Chord
        { //фільтр "дієзних" та "бемольних" акордів з параметрами 
            List<T> result = new();

            foreach (T chord in chords)
            {
                if (chord.CheckForMaxSharp() <= maxsharpness && chord.CheckForMinSharp() >= minsharpness)
                { result.Add(chord); }
            }
            return result;

        }

        public static List<T> FilterToSharpness<T>(List<T> chords, Tonalities tonality) where T : Chord
        { 

            return FilterToSharpness(chords, tonality.MinSharpness(), tonality.MaxSharpness());

        }


        public static bool MatchMelody(Chord chord, Note note)
        {
            return chord.Notes[^1] == note;
        }

        public static bool MatchBase(Chord chord, Note note)
        {
            return chord.Notes[0] == note;
        }

        public static bool MatchIndexNote(Chord chord, Note note, int index)
        {
            if (index < 0 || index >= chord.Notes.Count()) throw new IncorrectNote("Incorrect index");
            return chord.Notes[index] == note;
        }



        public static List<T> SortBySharpness<T>(List<T> chords) where T : Chord
        {
            var sorted = chords.OrderBy(p => p.Sharpness());
            List<T> sortedlist = sorted.ToList();
            return sortedlist;
        }

        public static int SortByRange(Chord first, Chord second)
        {
            return first.Range().CompareTo(second.Range());
        }

        public static int SortByBase(Chord first, Chord second)
        {
            return first.Notes[0].AbsPitch().CompareTo(second.Notes[0].AbsPitch());

        }

        public static int SortBySharpness(Chord first, Chord second)
        {
            return first.Sharpness().CompareTo(second.Sharpness());

        }

        public static int SortBySharpness_Desc(Chord first, Chord second)
        {
            return second.Sharpness().CompareTo(first.Sharpness());

        }

        public static List<T> SortByRange<T>(List<T> chords) where T : Chord
        {
            var sorted = chords.OrderBy(p => p.Range());

            List<T> sortedlist = sorted.ToList();
            List<T> result = new();
            //Console.WriteLine("test....");
            foreach (T chord in sortedlist)
            {
                // StringOutput.Display(chord);
                result.Add(chord);
            }
            //Console.WriteLine("end test....");
            return result;
        }

        public static void SortByRange1<T>(List<T> chords) where T : Chord
        {
            chords.Sort(SortByRange);
        }

        public static List<T> SortByBase<T>(List<T> chords) where T : Chord
        {
            List<T> result = new();
            chords.Sort(SortByBase);
            result.AddRange(chords);
            return result;
        }

        public static List<T> SplitDoubles<T>(List<T> chords) where T : Chord
        {//розкидує подвійні ноти по октавах

            List<T> values = new();
            for (int i = 0; i < chords.Count; i++)
            {
                for (int j = i + 1; j < chords.Count; j++)
                {
                    if (chords[i] == chords[j])
                    {
                        // Console.WriteLine(chords[i] + " == " + chords[j]);
                        for (int k = 1; k < chords[i].Notes.Count; k++)
                        {
                            if (chords[j].Notes[k].AbsPitch() == chords[j].Notes[k - 1].AbsPitch())
                                chords[j].Notes[k].OctUp();
                            if (chords[j].Notes[k].AbsPitch() < chords[j].Notes[k - 1].AbsPitch())
                                chords[j].Notes[k].OctUp();
                        }
                    }
                }
                values.Add(chords[i]);
            }
            return SortByRange(values);
        }





        public static List<T> ToOctave<T>(List<T> chords, int octave) where T : Scale
        {
            List<T> octaved = new();

            foreach (var chord in chords)
            {
                while (chord.Notes[0].Oct > octave)
                    chord.OctDown();
                while (chord.Notes[0].Oct < octave)
                    chord.OctUp();
                octaved.Add(chord);
            }
            return octaved;
        }

    }
}
