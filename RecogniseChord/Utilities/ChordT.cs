using static Music.ChordPermutation;
using static Music.Engine;
using static Music.Globals;
using static Music.Messages;
using static System.Console;

namespace Music
{
    public class ChordT : Chord 
    {
        private CHORDS chordtype;
        IntervalStructure intervals;
        IntervalStructure function_intervals;
        Note maintone;

        public ChordT(string input) : base(input)
        {
            AnalyzeChord();
        }

        public ChordT() : base() { /*chordtype = CHORDS.UNKNOWN; */AnalyzeChord(); }
        public ChordT(List<Note> nt) : base(nt) 
        {
            AnalyzeChord();
        }

        public ChordT(List<string> notes) : base(notes)
        {
            AnalyzeChord();
        }

        public ChordT(params Note[] nt) : base(nt)
        {
            AnalyzeChord();
        }

        private void AnalyzeChord()
        {
          intervals = new(this);
          SetChordType();
          function_intervals = new IntervalStructure(intervals, chordtype);
          if(function_intervals.prima is not null) maintone = new Note(notes[(int)function_intervals.prima]);
        }

        public CHORDS GetChordType
        {
            get { AnalyzeChord();  return chordtype; }
        }


        public Note Maintone
        { get { return maintone; } }

        public static Comparison<ChordT> Default()
        {
            return (x, y) => x.Notes.Count.CompareTo(y.Notes.Count); // Ваш метод порівняння за замовчуванням
        }

        public static new Comparison<ChordT> SortByRange() 
        {
            return (first, second) => first.Range().CompareTo(second.Range());
        }

        public static new Comparison<ChordT> SortBySharpness()
        {
            return (first, second) => first.Sharpness().CompareTo(second.Sharpness());
        }

        public static new Comparison<ChordT> SortByBase()
        {
            return (first, second) => first.Notes[0].CompareTo(second.Notes[0]);
        }


        protected void SetChordType()
        {

            if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta is null  && intervals.septyma > 0) chordtype = CHORDS.SEPT; // 7-акорд
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.TERZQ; // 3-4-акорд
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.QUINTS; // 5-6--акорд
            else if (intervals.secunda > 0 && intervals.terzia is null  && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.SEC; // 2--акорд
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quinta > 0 && intervals.quarta is null  && intervals.seksta is null  && intervals.septyma is null ) chordtype = CHORDS.TRI;// тризвук
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.SEXT; // секстакорд
            else if (intervals.secunda is null  && intervals.terzia is null  && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.QSEXT; // квартсекстакорд
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta is null  && intervals.seksta is null  && intervals.septyma > 0) chordtype = CHORDS.HSEPT; // неповний 7-акорд
            else if (intervals.secunda > 0 && intervals.terzia is null  && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta is null  && intervals.septyma is null ) chordtype = CHORDS.HSEC; // неповний 2-акорд
            else if (intervals.secunda is null  && intervals.terzia is null  && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.HQUINTS; // неповний 6-5-акорд
            else if (intervals.quinta > 0 && intervals.terzia > 0 && intervals.secunda > 0 && intervals.quarta is null  && intervals.seksta is null  && intervals.septyma is null ) chordtype = CHORDS.ADDSEC; // тризвук з доданою секундою
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta is null  && intervals.septyma > 0) chordtype = CHORDS.ADDQUARTA; // септакорд з доданою квартою
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta is null  && intervals.septyma > 0) chordtype = CHORDS.SUSQUARTA; // септакорд з затриманою квартою
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta is null  && intervals.septyma > 0) chordtype = CHORDS.NONACORD; // нонакорд
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.NONACORD_1i; // нонакорд в 1-му оберненні
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.NONACORD_2i; // нонакорд в 2-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.NONACORD_3i; // нонакорд в 3-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia is null  && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.NONACORD_4i; // нонакорд в 4-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.CORD69; // нонакорд із секстою
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta is null  && intervals.septyma > 0) chordtype = CHORDS.UNDECCORD; // ундецимакорд
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.UNDECCORD_1i; // ундецимакорд в 1-му оберненні
            else if (intervals.secunda is null  && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.UNDECCORD_2i; // ундецимакорд в 2-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma is null ) chordtype = CHORDS.UNDECCORD_3i; // ундецимакорд в 3-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.UNDECCORD_4i; // ундецимакорд в 4-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia is null  && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.UNDECCORD_5i; // ундецимакорд в 5-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta is null  && intervals.seksta is null  && intervals.septyma > 0) chordtype = CHORDS.HUNDECCORD; // неповний ундецимакорд (-квінта)
            else if (intervals.secunda > 0 && intervals.terzia is null  && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.HUNDECCORD_1i; // неповний ундецимакорд в 1-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia > 0 && intervals.quarta is null  && intervals.quinta is null  && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.HUNDECCORD_2i; // неповний ундецимакорд в 2-му оберненні
            else if (intervals.secunda > 0 && intervals.terzia is null  && intervals.quarta is null  && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.HUNDECCORD_3i; // неповний ундецимакорд в 3-му оберненні
            else if (intervals.secunda is null  && intervals.terzia is null  && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.HUNDECCORD_4i; // неповний ундецимакорд в 4-му оберненні
            else if (intervals.secunda - intervals.prima == 1 && intervals.terzia - intervals.secunda == 1 && intervals.quarta - intervals.terzia == 1 && intervals.quinta - intervals.quarta == 1 && intervals.seksta - intervals.quinta == 1 && intervals.septyma - intervals.seksta == 1
               ) chordtype = CHORDS.CLUSTER; // кластер

            else if (intervals. secunda > 0 && intervals.terzia > 0 && intervals.quarta > 0 && intervals.quinta > 0 && intervals.seksta > 0 && intervals.septyma > 0) chordtype = CHORDS.TERZDEC; // терцдецимакорд
            else
                chordtype = CHORDS.UNKNOWN;
        }

        public string ChordName()
        {

            string Chordname = "";
            
            
                if (chordtype == CHORDS.SEPT) Chordname = SEPT();
                else if (chordtype == CHORDS.TERZQ) Chordname = TERZQ();
                else if (chordtype == CHORDS.QUINTS) Chordname = QUINTS();
                else if (chordtype == CHORDS.SEC) Chordname = SEC();
                else if (chordtype == CHORDS.TRI) Chordname = TRI();
                else if (chordtype == CHORDS.SEXT) Chordname = SEXT();
                else if (chordtype == CHORDS.QSEXT) Chordname = QSEXT();
                else if (chordtype == CHORDS.HSEPT) Chordname = HSEPT();
                else if (chordtype == CHORDS.HSEC) Chordname = HSEC();
                else if (chordtype == CHORDS.HQUINTS) Chordname = HQUINTS();
                else if (chordtype == CHORDS.ADDSEC) Chordname = ADDSEC();
                else if (chordtype == CHORDS.ADDQUARTA) Chordname = ADDQUARTA();
                else if (chordtype == CHORDS.SUSQUARTA) Chordname = SUSQUARTA();
                else if (chordtype == CHORDS.NONACORD) Chordname = NONACORD();
                else if (chordtype == CHORDS.CORD69) Chordname = CORD69();
                else if (chordtype == CHORDS.NONACORD_1i) Chordname = NONACORD_1i();
                else if (chordtype == CHORDS.NONACORD_2i) Chordname = NONACORD_2i();
                else if (chordtype == CHORDS.NONACORD_3i) Chordname = NONACORD_3i();
                else if (chordtype == CHORDS.NONACORD_4i) Chordname = NONACORD_4i();
                else if (chordtype == CHORDS.UNDECCORD) Chordname = UNDECCORD();
                else if (chordtype == CHORDS.HUNDECCORD) Chordname = HUNDECCORD();
                else if (chordtype == CHORDS.HUNDECCORD_1i) Chordname = HUNDECCORD_1i();
                else if (chordtype == CHORDS.HUNDECCORD_2i) Chordname = HUNDECCORD_2i();
                else if (chordtype == CHORDS.HUNDECCORD_3i) Chordname = HUNDECCORD_3i();
                else if (chordtype == CHORDS.HUNDECCORD_4i) Chordname = HUNDECCORD_4i();
                else if (chordtype == CHORDS.UNDECCORD_1i) Chordname = UNDECCORD_1i();
                else if (chordtype == CHORDS.UNDECCORD_2i) Chordname = UNDECCORD_2i();
                else if (chordtype == CHORDS.UNDECCORD_3i) Chordname = UNDECCORD_3i();
                else if (chordtype == CHORDS.UNDECCORD_4i) Chordname = UNDECCORD_4i();
                else if (chordtype == CHORDS.UNDECCORD_5i) Chordname = UNDECCORD_5i();
                else if (chordtype == CHORDS.TERZDEC) Chordname = TERZDEC();
                else if (chordtype == CHORDS.CLUSTER) Chordname = CLUSTER();
                else Chordname = "акорд невідомої структури";

            try
            {


                if (function_intervals.quinta is not null)
                {
                    if (Q_Quinta() == QUALITY.AUG) Chordname = aug_int() + Chordname;
                    if (Q_Quinta() == QUALITY.DIM) Chordname = dim_int() + Chordname;
                    if (Q_Quinta() == QUALITY.PERFECT)
                    {
                        if (Q_Terzia() == QUALITY.MAJ) Chordname = major_ton() + Chordname;
                        else if (Q_Terzia() == QUALITY.MIN) Chordname = minor_ton() + Chordname;
                        else throw new IncorrectNote("interval determine error, prima = " + Notes[(int)function_intervals.prima].Pitch + ", terzia = " + Notes[(int)function_intervals.terzia].Pitch + ", quinta = " + Notes[(int)function_intervals.quinta].Pitch);
                    }
                }

                if (function_intervals.septyma is not null)
                {
                    if (Q_Septyma() == QUALITY.MAJ) Chordname = major_int() + Chordname;
                    else if (Q_Septyma() == QUALITY.MIN) Chordname = minor_int() + Chordname;
                    else if (Q_Septyma() == QUALITY.DIM) Chordname = dim_int() + Chordname;


                    if (function_intervals.secunda is not null)
                    {
                        if (Q_Nona() == QUALITY.MAJ) Chordname = major_int() + Chordname;
                        else if (Q_Nona() == QUALITY.MIN) Chordname = minor_int() + Chordname;
                        else if (Q_Nona() == QUALITY.AUG) Chordname = aug_int() + Chordname;
                    }
                }
            }
            catch (IncorrectNote e)
            {
                Message(12, e.Message);
            }
            return Chordname;
        }


        public string ChordSymbols()
        {
            string Chordsymbol = DefineChordSymbol();
            if (maintone is not null) Chordsymbol = maintone.Key(notation) + Chordsymbol;

            string newChordSymbol = "";
            newChordSymbol += Chordsymbol.ElementAt(0);
            newChordSymbol = newChordSymbol.ToUpper();
            newChordSymbol += Chordsymbol.Substring(1);

            return newChordSymbol;
        }

        public string ChordSymbols(Tonalities tonality)
        {
            string Chordsymbol = DefineChordSymbol();
            string degreesymbol = "";

            Interval degree = new Interval(maintone, tonality.GetNote());

            switch ((int)degree.Interval_)
            {
                default:
                    degreesymbol = ""; break;
                case 0: degreesymbol = "I"; break;
                case 1: degreesymbol = "II"; break;
                case 2: degreesymbol = "III"; break;
                case 3: degreesymbol = "S"; break;
                case 4: degreesymbol = "D"; break;
                case 5: degreesymbol = "VI"; break;
                case 6: degreesymbol = "VII"; break;
            }
            if (degree.Quality == QUALITY.DIM) degreesymbol+= "b";
            else if (degree.Quality == QUALITY.AUG) degreesymbol += "#";

            Chordsymbol = degreesymbol + Chordsymbol;
            return Chordsymbol;
        }


            private string DefineChordSymbol()
        {
            string Chordsymbol = "";


            if (chordtype == CHORDS.SEPT) Chordsymbol = "7";
            if (chordtype == CHORDS.SEPT) Chordsymbol = "7";
            else if (chordtype == CHORDS.TERZQ) Chordsymbol = "4/3";
            else if (chordtype == CHORDS.QUINTS) Chordsymbol = "6/5";
            else if (chordtype == CHORDS.SEC) Chordsymbol = "2";
            else if (chordtype == CHORDS.TRI) Chordsymbol = " ";// ніяк не позначається
            else if (chordtype == CHORDS.SEXT) Chordsymbol = "6";
            else if (chordtype == CHORDS.QSEXT) Chordsymbol = "6/4";
            else if (chordtype == CHORDS.HSEPT) Chordsymbol = "7*";
            else if (chordtype == CHORDS.HSEC) Chordsymbol = "2*";
            else if (chordtype == CHORDS.HQUINTS) Chordsymbol = "5/6*";
            else if (chordtype == CHORDS.ADDSEC) Chordsymbol = "add2";
            else if (chordtype == CHORDS.ADDQUARTA) Chordsymbol = "add4";
            else if (chordtype == CHORDS.SUSQUARTA) Chordsymbol = "sus4";
            else if (chordtype == CHORDS.NONACORD) Chordsymbol = "9";
            else if (chordtype == CHORDS.CORD69) Chordsymbol = "6/9";
            else if (chordtype == CHORDS.NONACORD_1i) Chordsymbol = "9/(1)";
            else if (chordtype == CHORDS.NONACORD_2i) Chordsymbol = "9/(2)";
            else if (chordtype == CHORDS.NONACORD_3i) Chordsymbol = "9/(3)";
            else if (chordtype == CHORDS.NONACORD_4i) Chordsymbol = "9/(4)";
            else if (chordtype == CHORDS.UNDECCORD) Chordsymbol = "11";
            else if (chordtype == CHORDS.HUNDECCORD) Chordsymbol = "3/11";
            else if (chordtype == CHORDS.HUNDECCORD_1i) Chordsymbol = "11*/(1)";
            else if (chordtype == CHORDS.HUNDECCORD_2i) Chordsymbol = "11*/(2)";
            else if (chordtype == CHORDS.HUNDECCORD_3i) Chordsymbol = "11*/(3)";
            else if (chordtype == CHORDS.HUNDECCORD_4i) Chordsymbol = "11*/(4)";
            else if (chordtype == CHORDS.UNDECCORD_1i) Chordsymbol = "11/(1)";
            else if (chordtype == CHORDS.UNDECCORD_2i) Chordsymbol = "11/(2)";
            else if (chordtype == CHORDS.UNDECCORD_3i) Chordsymbol = "11/(3)";
            else if (chordtype == CHORDS.UNDECCORD_4i) Chordsymbol = "11/(4)";
            else if (chordtype == CHORDS.UNDECCORD_5i) Chordsymbol = "11/(5)";
            else if (chordtype == CHORDS.TERZDEC) Chordsymbol = "13";
            else if (chordtype == CHORDS.CLUSTER) Chordsymbol = "**";
            else Chordsymbol = "?";

            string altersymbol = "";
            try
            {
                
                if (function_intervals.quinta is not null)
                {
                    if (Q_Quinta() == QUALITY.AUG) altersymbol = "+"; //збільшений
                    if (Q_Quinta() == QUALITY.DIM)
                    {
                        if (Q_Terzia() == QUALITY.MIN && Q_Septyma() == QUALITY.DIM)//зменшений
                            return "o";
                        else altersymbol = "b5";
                    }

                    if (Q_Quinta() == QUALITY.PERFECT)
                    {
                        if (Q_Terzia() == QUALITY.MAJ) ;// нічого не додаємо;
                        else if (Q_Terzia() == QUALITY.MIN) Chordsymbol = "m" + Chordsymbol;
                        else throw new IncorrectNote("interval determine error, prima = " + Notes[(int)function_intervals.prima].Pitch + ", terzia = " + Notes[(int)function_intervals.terzia].Pitch + ", quinta = " + Notes[(int)function_intervals.quinta].Pitch);
                    }
                }

                if (function_intervals.septyma is not null)
                {
                    if (Q_Septyma() == QUALITY.MAJ) Chordsymbol = "^" + Chordsymbol;
                    else if (Q_Septyma() == QUALITY.MIN) ;
                    else altersymbol = "alt";


                    if (function_intervals.secunda is not null)
                    {
                        if (Q_Nona() == QUALITY.MAJ) ;
                        else if (Q_Nona() == QUALITY.MIN) altersymbol = "(b9)"; 
                        else if (Q_Nona() == QUALITY.AUG) altersymbol = "(#9)";
                    }
                }
            }
            catch (IncorrectNote e)
            {
                Message(12, e.Message);
            }

            return Chordsymbol + altersymbol;
        }


        public MODE GetMode()
        {
            if (Q_Terzia() == QUALITY.MAJ) return MODE.dur;
            else if (Q_Terzia() == QUALITY.MIN) return MODE.moll;
            else throw new IncorrectNote("imossible to discover mode");
        }

        public QUALITY Q_Terzia()
        {
            if (function_intervals.prima == null) throw new IncorrectNote("No terzia found");
            else
            {
                if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.terzia].Pitch) == 4) return QUALITY.MAJ;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.terzia].Pitch) == 3) return QUALITY.MIN;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.terzia].Pitch) == 2) return QUALITY.DIM;
                else throw new IncorrectNote("interval determine error, prima = " + Notes[(int)function_intervals.prima].Pitch + ", terzia = " + Notes[(int)function_intervals.terzia].Pitch);
            }
        }

        public QUALITY Q_Quinta()
        {
            if (function_intervals.quinta == null) throw new IncorrectNote("No quinta found");
            else
            {
                if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.quinta].Pitch) == 8) return QUALITY.AUG;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.quinta].Pitch) == 7) return QUALITY.PERFECT;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.quinta].Pitch) == 6) return QUALITY.DIM;
                else throw new IncorrectNote("interval determine error, prima = " + Notes[(int)function_intervals.prima].Pitch + ", quinta = " + Notes[(int)function_intervals.quinta].Pitch);
            }
        }

        public QUALITY Q_Septyma()
        {//визначає якість септими
            if (function_intervals.septyma == null) throw new IncorrectNote("No septyma found");
            else
            {
                if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.septyma].Pitch) == 11) return QUALITY.MAJ;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.septyma].Pitch) == 10) return QUALITY.MIN;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.septyma].Pitch) == 9) return QUALITY.DIM;
                else throw new IncorrectNote("interval determine error, prima = " + Notes[(int)function_intervals.prima].Pitch + ", septyma = " + Notes[(int)function_intervals.septyma].Pitch);
            }
        }

        public QUALITY Q_Nona()
        {//визначає якість септими
            if (function_intervals.septyma == null || function_intervals.secunda == null) throw new IncorrectNote("Not a ninth chord");

            else
            {
                if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.secunda].Pitch) == 2) return QUALITY.MAJ;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.secunda].Pitch) == 1) return QUALITY.MIN;
                else if (pitchdiff(Notes[(int)function_intervals.prima].Pitch, Notes[(int)function_intervals.secunda].Pitch) == 3) return QUALITY.AUG;
                else throw new IncorrectNote("interval determine error, prima = " + Notes[(int)function_intervals.prima].Pitch + ", secunda = " + Notes[(int)function_intervals.secunda].Pitch);
            }
        }



        protected void Constructor(int num, Note nt)
        {// створює заготовку з кількох однакових нот
            for (int i = 0; i < 3; i++)
            {
                Note note = (Note)nt.Clone();
                AddNote(note);
            }
        }

        public void DetermineChord()
        {
            if (true) chordtype = CHORDS.TRI;
        }
        public override void Display(bool octtriger = true, int color = 14)
        {
            base.Display();
            AnalyzeChord();
            WriteLine(ChordSymbols());
        }

        public void Display(Tonalities tonality, bool octtriger = true, int color = 14)
        {
            base.Display();
            AnalyzeChord();
            Write(ChordSymbols(tonality));
        }


        public int? If_note_in_Chord(INTERVALS interval) // перевіряє наявність в акорді заданого інтервалу. Якщо не знаходить, повертає null
        {            
            for (int i = 0; i < Notes.Count; i++)
            {
                if (stepdiff(Notes[0].Step, Notes[i].Step) == (int)interval) // виявлення заданого інтервалу в одному з голосів
                {
                    return i;
                }
            }
            //cout << "повертаємо значення для position: " << position << endl;
            return null;
        }


        public new List<ChordT> PermuteList(int octave = 0) // генерування усіх можливих розташувань
        {
            return PermuteToAdjustedList(this, octave);
        }

        public static void Display(List<ChordT> scale, bool octtrigger = true, int color = 14) 
        {
            StringOutput.Display(scale, octtrigger, color);

            WriteLine("type T");
        }

        public void Construct(params int[] alter)
        {// генерує акорди терцієвої структури, якщо основний тон акорду вже задано.
             for (int i=0; i<alter.Length; i++)
            {
                //додає ноти по терціях
                try { 
                AddNote(2*(i+1), alter[i]);
                }
                catch (IncorrectNote e) { MessageL(12, "incorrect note " + (i+1) + " was omitted"); }
            }
        }

        public void Construct(Note nt, params int[] alter)
        {// генерує акорди терцієвої структури від заданої ноти
            Note note = (Note)nt.Clone();
            AddNote(note);
            Construct(alter);
        }


        public void Construct(string input, params int[] alter)
        {// генерує акорди терцієвої структури від заданої ноти
            Note note = new(input);
            AddNote(note);
            Construct(alter);
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }





        public void MajorTriad(string input)
        {
            try
            {
                Note nt = new(input);
                MajorTriad(nt);
            }
            catch 
            { Message(12, "impossible to create Maj");}

        }
        public void MajorTriad(Note nt)
        {
            Constructor(3, nt);
            notes[1].Transpose(2, 1);
            notes[2].Transpose(4, 0);
            chordtype = CHORDS.TRI;
        }

        public ChordT MajorTriadChord(Note nt)
        {
            Constructor(3, nt);
            notes[1].Transpose(2, 1);
            notes[2].Transpose(4, 0);
            chordtype = CHORDS.TRI;
            return this;
        }

        public ChordT MajorTriadChord(string input)
        {
            try
            {
                Note nt = new(input);
                MajorTriad(nt);
            }
            catch
            { Message(12, "impossible to create Maj"); }
            return this;
        }

        public void MinorTriad(string input)
        {
            Note nt = new(input);
            MinorTriad(nt);
        }

        public void MinorTriad(Note nt)
        {
            Constructor(3, nt);
            notes[1].Transpose(2, -1);
            notes[2].Transpose(4, 0);
            chordtype = CHORDS.TRI;
        }

        public void SeventhChord(Note nt, SEPTS mode = SEPTS.MAJMIN)
        {
            switch (mode)
            {
                default:
                case SEPTS.MAJMIN: Construct(nt, 1, 0, -1); break;
                case SEPTS.MAJAUG: Construct(nt, 1, 2, 1); break;
                case SEPTS.MAJMAJ: Construct(nt, 1, 0, 1); break;
                case SEPTS.MINMAJ: Construct(nt, -1, 0, 1); break;
                case SEPTS.MINMIN: Construct(nt, -1, 0, -1); break;
                case SEPTS.MINDIM: Construct(nt, -1, -2, -1); break;
                case SEPTS.DIMDIM: Construct(nt, -1, -2, -2); break;
                case SEPTS.ALTQUINT: Construct(nt, 1, -2, -1); break;
                case SEPTS.ALTPRIM: Construct(nt, -2, -2, -2); break;
            }
            chordtype = CHORDS.SEPT;
        }



        public new List<ChordT> AllTonalities()
        {
            return Interval.AllTonalities(this);
        }

        public static List<ChordT> AllTonalities(ChordT chord)
        {
            return Interval.AllTonalities(chord);
        }

        public new List<ChordT> PermuteTransposeList(int octave = -1)
        {
            List<ChordT> tonallist = new();
            List<ChordT> transposed = AllTonalities();
            foreach (ChordT ch in transposed)
                tonallist.AddRange(ch.PermuteList(octave));
            return tonallist;
        }

        public new List<ChordT> TransposePermuteList(int octave = -1)
        {
            List<ChordT> tonallist = new();
            List<ChordT> permuted = PermuteList(octave);
            foreach (ChordT ch in permuted)
                tonallist.AddRange(AllTonalities(ch));
            return tonallist;
        }

        public static ChordT operator +(ChordT A, ChordT B)
        {
            ChordT C = (ChordT)A.Clone();
            foreach (Note note in B.Notes)
            {
                Note temp = (Note)note.Clone();
                C.AddNote(temp);
            }
            C.Adjust();
            return C;
        }

        public static ChordT operator +(Note A, ChordT B)
        {
            ChordT C = new();
            C.AddNote(A);
            foreach (Note note in B.Notes)
            {
                Note temp = (Note)note.Clone();
                C.AddNote(temp);
            }
            C.Adjust();
            return C;
        }

        public override object Clone()
        {
            ChordT clone = new();
            // Здійснюємо глибоке клонування для елементів Chord
            clone.notes = new List<Note>(this.notes.Count);
            foreach (Note note in this.notes)
            {
                clone.notes.Add((Note)note.Clone());
            }
            return clone;
        }

        public object Clone(ChordT A)
        {
            ChordT clone = new();
            // Здійснюємо глибоке клонування для елементів ChordT
            clone.notes = new List<Note>(A.notes.Count);
            foreach (Note note in this.notes)
            {
                clone.notes.Add((Note)note.Clone());
            }
            return clone;
        }

        public static List<ChordT> Clone(List<ChordT> original)
        {
            List<ChordT> clonedlist = new();
            foreach (ChordT originalChordT in original)
            {
                ChordT clonedChordT = (ChordT)originalChordT.Clone();
                clonedlist.Add(clonedChordT);
            }
            return clonedlist;
        }

        public static ChordT[] Clone(ChordT[] original)
        {
            ChordT[] cloned = new ChordT[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                ChordT clonedChordT = (ChordT)original[i].Clone();
                cloned[i] = clonedChordT;
            }
            return cloned;
        }



    }
}
