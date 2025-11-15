using static Music.Messages;
using static Music.Engine;
using static System.Console;
using static System.Convert;

namespace Music
{
    public class StringOutput
    {
        private List<string> chords;

        public StringOutput(List<string> chords)
        { this.chords = chords; }

        public List<string> Chords
        {
            get { return chords; }
            set { chords = value; }
        }

        public void Display()
        {
            foreach (string note in chords)
            {
                Message(14, key_to_notename(note) + " ");
            }
        }

        public static void Display(List<string> chords, int color = 14)
        {
            foreach (string note in chords)
            {
                Message(color, key_to_notename(note) + " ");
            }
        }


        public static void Display(Note note, bool octtrigger = true, int color = 7)
        {
            ForegroundColor = (ConsoleColor)color;
            if(octtrigger) Write($"{note.GetName() + " (" + ToInt32(note.Oct) + ") ",-10}");
            else Write($"{note.GetName(),-7}");
            ResetColor();
        }

        public static void Display(List<Note> notes, bool octtrigger = true, int color = 14)
        {
            foreach (Note note in notes)
            {
                Display(note, octtrigger, color);
            }
        }

        public static void Display<T>(T scale, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (Note note in scale.Notes)
            {
                Display(note, octtrigger, color);
            }
            WriteLine();            
        }

        public static void Display<T>(T[] scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (T scale in scales)
            {
                Display(scale, octtrigger, color);
            }
            WriteLine();
        }

        public static void Display<T>(T scale) where T : Scale
        {
            foreach (Note note in scale.Notes)
                note.Display();
        }

        public static void Display<T>(List<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (T scale in scales)
            {
                Display(scale, octtrigger, color);
            }
            WriteLine();
        }


        public static void Display<T>(Queue<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (T scale in scales)
            {
                Display(scale, octtrigger, color);
            }
            WriteLine();
        }
        

            public static void Display<T>(LinkedList<T> chords, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (T chord in chords)
            {
                chord.Display();
            }
            WriteLine();
        }


        public static void Display<T>(List<List<T>> chords, bool octtrigger = true, int color = 14) where T : Scale
        {
            int counter = 0;
            foreach (List<T> chordList in chords)
            {

                foreach (T chord in chordList)
                {
                    counter++;
                    Message(6, counter + ":");
                    Display(chord, octtrigger, color);
                }
            }
        }

        public static void DisplaySh<T>(T scale, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (Note note in scale.Notes)
            {
                Display(note, octtrigger, color);
            }
            float sharpness = scale.Sharpness();
            if (sharpness > 0) 
            { WriteLine("+{0:f2}", ToSingle(scale.Sharpness())); }
            else
            { WriteLine("{0:f2}", ToSingle(scale.Sharpness())); }            
        }

        public static void DisplayR_Sh<T>(T scale, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (Note note in scale.Notes)
            {
                Display(note, octtrigger, color);
            }
            float sharpness = scale.Sharpness();
            if (sharpness > 0)
            { WriteLine("\t+{0:f2}\t{1}", ToSingle(scale.Sharpness()), scale.Range()); }
            else
            { WriteLine("\t{0:f2}\t{1}", ToSingle(scale.Sharpness()), scale.Range()); }
        }

        public static void DisplaySh<T>(List<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (T scale in scales)
            { DisplaySh(scale, octtrigger, color); }
        }

        public static void DisplayR_Sh<T>(List<T> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (T scale in scales)
            { DisplayR_Sh(scale, octtrigger, color); }
        }


        public static void DisplaySh<T>(List<List<T>> scales, bool octtrigger = true, int color = 14) where T : Scale
        {
            foreach (List<T> scale in scales)
            { DisplaySh(scale, octtrigger, color); WriteLine(); }
        }

        public static void DisplayInline(Note note)
        { WriteLine(note.GetName() + ", pitch = " + ToInt32(note.Pitch) + ", octave = " + ToInt32(note.Oct) + ", duration = " + note.AbsDuration() + ", freq = " + ToInt32(Pitch_to_hz(note.AbsPitch()))); }

        public static void DisplayInline<T>(T scale) where T : Scale
        {
            foreach (Note note in scale.Notes)
                DisplayInline(note);
        }
        public static void DisplayInline<T>(List<T> scales) where T : Scale
        {
            foreach (T scale in scales)
            { DisplayInline(scale); WriteLine();  }

        }
        public static void DisplayInline<T>(T[] scales) where T : Scale
        {
            foreach (T scale in scales)
            { DisplayInline(scale); WriteLine(); }
        }

        public static void DisplayTable(Note note)
        { WriteLine(note.GetName() + "\npitch = " + ToInt32(note.Pitch) + "\noctave = " + ToInt32(note.Oct) + "\nduration = " + note.Duration + "\nfreq = " + ToInt32(Pitch_to_hz(note.AbsPitch()))); }

        public static void DisplayTable<T>(T scale) where T : Scale
        {
            foreach (Note note in scale.Notes)
                DisplayTable(note);
        }

        public static void DisplayTable<T>(List<T> scales) where T : Scale
        {
            foreach (T scale in scales)
                DisplayTable(scale);
        }
        public static void DisplayTable<T>(T[] scales) where T : Scale
        {
            foreach (T scale in scales)
                DisplayTable(scale);
        }

    }


}

