using static Music.Engine;
using static Music.Collections;
using static Music.ChordT;
using static Music.Menues;
using static Music.Messages;
using static System.Console;
using System.Threading.Tasks;
using System.ComponentModel.Design;
using System.Collections.Specialized;

namespace Music
{

    delegate bool Voicing(ChordT chord1, ChordT chord2, bool testmode = false);
    delegate ChordT Chooser(List<ChordT> chosenchords, bool testmode);

    public class ChordProgression
    {
        Note melody;
        ChordT? previous;

        public ChordProgression(Note melody, ChordT previous)
        {
            this.melody = melody;
            this.previous = previous;
        }

        public ChordProgression(Note melody)
        {
            this.melody = melody;
            this.previous = null;
        }

        public ChordT Prev { get { return previous; } }
        public Note Melody { get { return melody; } }

        private int CommonNotes(ChordT other)
        {
            if (previous.Notes.Count >= other.Notes.Count) return previous.Notes.Count;
            else return other.Notes.Count;
        }
        public ChordT FindNextChord(List<ChordT> allchords, Tonalities tonality, bool iffirst, bool iffinal, int findmode, bool testmode = false)
        {

            List<ChordT> selectedchords = new();
            List<ChordT> chosenchords = new();
            List<ChordT> intonalchords = new();
            Chooser chooser;

            if (testmode)  MessageL(8, allchords.Count + " chords in a base");


            if (findmode == 1) { chooser = RandomChooser; }
            else if (findmode == 2) { chooser = MinJumpChooser; }
            else throw new IncorrectNote("incorrect chocie");


            try
            {
                // тональний фільтр  
                intonalchords = TonalFilter(allchords, tonality, iffinal, testmode, intonalchords);

                // фільтруємо за мелодією
                MelodyMatchFilter(selectedchords, intonalchords);

                if (selectedchords.Count < 1) throw new IncorrectNote("Не знайшли акордів для ноти " + melody.GetName());
                else if (testmode) MessageL(8, selectedchords.Count + " melody-matched chords found");

                // фільтруємо за голосоведенням
                chosenchords = VoicingFilter(testmode, selectedchords, chosenchords);

                if (iffirst) return RandomChooser(chosenchords, testmode);
                else return chooser(chosenchords, testmode);
            }
            catch (ImpossibleVoicing e)
            {
                MessageL(12, e.Message);
                return null;
            }
            catch (IncorrectNote e)
            {
                MessageL(12, e.Message);
                return null;
            }
        }

        private ChordT RandomChooser(List<ChordT> chosenchords, bool testmode)
        {
            Random rnd = new();
            if (chosenchords.Count < 1) /*ReadKey()*/;
            int index = rnd.Next(chosenchords.Count - 1);

            if (testmode) MessageL(8, "random index chosen = " + index + " / " + chosenchords.Count);
            return chosenchords[index];
        }


        private ChordT MinJumpChooser(List<ChordT> chosenchords, bool testmode)
        {
           int jumper = Jumpness(chosenchords[0]);
            int index = 0;

            for (int i = 0; i < chosenchords.Count; i++)
            {
                int temp = Jumpness(chosenchords[i]);
                if(testmode)MessageL(8, "index = " + i + " / " + chosenchords.Count +  "; jump = " + temp);
                if (temp < jumper)
                {
                    index = i;
                    jumper = temp;
                }
            }
            if (testmode) MessageL(8, "result index = " + index);
            return chosenchords[index];
        }

        private List<ChordT> VoicingFilter(bool testmode, List<ChordT> selectedchords, List<ChordT> chosenchords)
        {
            if (previous is null)
            {
                chosenchords = Clone(selectedchords);
                if (testmode) MessageL(8, "first chord");
            }
            else
            {
                foreach (ChordT ch in selectedchords)
                {

                    if (!ParalelFifth(ch, previous) && !ParalelOct(ch, previous) &&
                        !Jumps(ch, previous) && !Crossing(ch, previous) && !Augs(ch, previous) && !CrossAlter(ch, previous)) chosenchords.Add(ch);
                }
                if (chosenchords.Count < 1) throw new ImpossibleVoicing();
            }

            return chosenchords;
        }

        private void MelodyMatchFilter(List<ChordT> selectedchords, List<ChordT> intonalchords)
        {
            foreach (ChordT ch in intonalchords)
            {
                if (MelodyMatch(ch))
                    selectedchords.Add(ch);
            }
        }

        private static List<ChordT> TonalFilter(List<ChordT> allchords, Tonalities tonality, bool iffinal, bool testmode, List<ChordT> intonalchords)
        {
            if (iffinal)
            {
                //звуження вибору для фінального акорду
                foreach (ChordT ch in allchords)
                    if (ch.GetChordType == CHORDS.TRI && ch.Maintone.EqualDegree(tonality.Note()) && ch.GetMode() == tonality.Mode)
                        intonalchords.Add(ch);
                if (testmode)
                {
                    MessageL(8, intonalchords.Count() + " final chords selected");
                    Display(intonalchords, true, 8);
                }
                //ReadKey();
            }
            else intonalchords = Clone(allchords);
            return intonalchords;
        }

        public int Jumpness(ChordT other)
        {
            if (previous is null || other is null) return 0;
            int commonnotes = CommonNotes(other);
            int jumpness = 0;

            for (int i = 0; i < commonnotes; i++)
                jumpness += Math.Abs(previous.Notes[i].AbsPitch() - other.Notes[i].AbsPitch());

            return jumpness;
        }

        private bool MelodyMatch(ChordT chord)
        {
            return (melody.AbsPitch() == chord.Notes[^1].AbsPitch()) && (melody.Step == chord.Notes[^1].Step);
        }


        private bool ParalelFifth(ChordT chord1, ChordT chord2, bool testmode = false)
        { // паралельні квінти
            bool paralel5 = false;
            int commonnotes = CommonNotes(chord2);

            for (int i = 0; i < commonnotes; i++)
                for (int j = i; j < commonnotes; j++)
                {
                    if (pitchdiff(chord2.Notes[i].Pitch, chord2.Notes[j].Pitch) == 7 &&
                        pitchdiff(chord1.Notes[i].Pitch, chord1.Notes[j].Pitch) == 7)
                    {
                        paralel5 = true;
                        if (testmode) Write(".");
                        break;
                    }
                }
            return paralel5;
        }
        private bool ParalelOct(ChordT chord1, ChordT chord2, bool testmode = false)
        { // паралельні октави і пріми
            bool paralel = false;
            int commonnotes = CommonNotes(chord2);

            for (int i = 0; i < commonnotes; i++)
                for (int j = i + 1; j < commonnotes; j++)
                {
                    if (pitchdiff(chord2.Notes[i].Pitch, chord2.Notes[j].Pitch) == 0 &&
                        pitchdiff(chord1.Notes[i].Pitch, chord1.Notes[j].Pitch) == 0)
                    {
                        paralel = true;
                        if (testmode)
                        {
                            Color(8);
                            WriteLine("{0}:{1} // {2}", i, j, chord1);
                            WriteLine("{0}:{1} // {2}", i, j, chord2);
                            WriteLine("paralel = " + paralel);
                            Color(7);
                        }
                        break;
                    }
                }
            return paralel;
        }

        private bool Jumps(ChordT chord1, ChordT chord2, bool testmode = false)
        {// стрибки на інтервали більше квінти (крім мелодії)
            bool jumps = false;
            int commonnotes = CommonNotes(chord2);
            for (int i = 0; i < commonnotes - 1; i++)
            {
                if (chord2.Notes[i].AbsPitch() - chord1.Notes[i].AbsPitch() > 7 ||
                    chord1.Notes[i].AbsPitch() - chord2.Notes[i].AbsPitch() > 7)
                { jumps = true; if (testmode) Write("."); break; }
            }
            return jumps;
        }


        private bool Augs(ChordT chord1, ChordT chord2, bool testmode = false) // ходи на збільшені інтервали
        {
            bool augs = false;
            int commonnotes = CommonNotes(chord2);
            for (int i = 0; i < commonnotes - 1; i++)
            {
                Interval temp = new Interval(chord2.Notes[i], chord1.Notes[i]);
                if (temp.Quality == QUALITY.AUG || temp.Quality == QUALITY.AUG2)
                { if (testmode) Write("^"); return true; }

            }
            return false;
        }

        private bool Crossing(ChordT chord1, ChordT chord2, bool testmode = false) // заходження тонів
        {//заходження тонів
            bool crossing = false;
            int commonnotes = CommonNotes(chord2);
            for (int i = 0; i < commonnotes - 1; i++)
            {
                if (chord2.Notes[i].AbsPitch() > chord1.Notes[i + 1].AbsPitch() ||
                    chord1.Notes[i].AbsPitch() > chord2.Notes[i + 1].AbsPitch())

                {
                    crossing = true;
                    if (testmode)
                    {
                        Color(8);
                        WriteLine("{0}:{1} // {2}", i, i + 1, chord1);
                        WriteLine("{0}:{1} // {2}", i, i + 1, chord2);
                        WriteLine("crossing = " + crossing);
                        Color(7);
                    }
                    break;
                }
            }
            return crossing;
        }

        private bool CrossAlter(ChordT chord1, ChordT chord2, bool testmode = false) // перечення
        {
            bool crossalters = false;
            int commonnotes = CommonNotes(chord2);
            for (int i = 0; i < commonnotes; i++)
                for (int j = i + 1; j < commonnotes; j++)                    
                {
                    if (chord1.Notes[i].Step == chord2.Notes[j].Step && chord1.Notes[i].GetAlter() != chord2.Notes[j].GetAlter())
                    { if (testmode) MessageL(8, "crossalter cut"); return true; }
            }
            return false;
        }



    }
}
