using Music;
using static Music.ChordPermutation;
using System.Numerics;
using System.Xml.Linq;

namespace Music
{
    public class Chord : Scale
    {
        // співзвуччя з довільної кількості звуків, відтворюються одночасно

        public Chord() : base() { }
        public Chord(List<Note> nt) : base(nt) { }

        public Chord(List<string> notes) : base(notes)
        { }

        public Chord(string input) : base(input)
        {
            Adjust();
        }

        public Chord(params Note[] nt) : base(nt) { }

        public Note this[int index]
        { get { return notes[index]; } set { this[index] = value; } }


        public Comparison<Chord> SortByRange()
        {
            return (first, second) => first.Range().CompareTo(second.Range());
        }

        public Comparison<Chord> SortBySharpness()
        {
            return (first, second) => first.Sharpness().CompareTo(second.Sharpness());
        }

        public Comparison<Chord> SortByBase()
        {
            return (first, second) => first.Notes[0].CompareTo(second.Notes[0]);
        }

        public new List<Note> GetNotes() { return notes; }

        public List<Chord> AllTonalities()
        {
            return Interval.AllTonalities(this);
        }

        public static List<Chord> AllTonalities(Chord chord)
        {
            return Interval.AllTonalities(chord);
        }

        public new void Inversion()
        {
            if (notes.Count > 1)
            {
                Note firstNote = notes[0]; // Зберігаємо посилання на перший елемент

                // Зсуваємо всі елементи вперед, від другого до останнього
                for (int i = 0; i < notes.Count - 1; i++)
                {
                    notes[i] = notes[i + 1];
                }

                // Останній елемент отримує значення першого елемента
                notes[^1] = firstNote;
                Adjust();
            }
        }

        public new int Octaves()
        { return Range() / 12; }


        public List<Chord> PermuteList(int octave = 0) // генерування усіх можливих розташувань
        {
            return PermuteToAdjustedList(this, octave);
        }

        public Chord[] PermuteArray(int octave = 0)
        {
            return PermuteToAdjustedArray(this, octave);
        }


        public new void RemoveNote(Note note) { notes.Remove(note); }


        public new void Reverse()
        { notes.Reverse(); }

        //transposed.Reverse(); return transposed;

        public new int Range()
        { return notes[^1].AbsPitch() - notes[^1].AbsPitch(); }


        public new int Size() { return notes.Count(); }

        public bool SaveMidi(string filepath = "output.mid")
        {
            return MidiFile0.SaveMidi(this, filepath);
        }

        public new void Transpose(INTERVALS interval, QUALITY quality, DIR dir)
        {
            foreach (Note note in notes)
                note.Transpose(interval, quality, dir);
        }

        public void TransposeToLowNote(Note note) // chord
        {
            DIR dir;
            if (note == Notes[0]) return;
            else if (note > Notes[0]) dir = DIR.UP;
            else dir = DIR.DOWN;

            Interval move = new(notes[0], note);

            foreach (Note nt in notes)
                nt.Transpose(move, dir);
        }


        public void TransposeToHighNote(Note note)
        {

            if (note == Notes[^1]) return;
            DIR dir = new();
            if (note > Notes[^1]) dir = DIR.UP;
            else dir = DIR.DOWN;

            Interval move = new(notes[^1], note);
            if (move.Interval_ == INTERVALS.PRIMA && move.Quality == QUALITY.DIM) { notes[^1].Display(); note.Display(); }
            foreach (Note nt in notes)
                nt.Transpose(move, dir);
            //if (notes[^1].Oct == 1) 
            //    Console.WriteLine(move.Interval_ + " " + move.Quality + " " + move.Octaves);
        }


        public static List<Chord> Transpose(List<Chord> original, INTERVALS interval, QUALITY quality, DIR dir)
        {
            List<Chord> transposed = Clone(original);
            foreach (Chord ch in transposed)
                ch.Transpose(interval, quality, dir);
            return transposed;
        }

        public List<List<Chord>> TransposePermute(int octave = -1)
        {
            List<List<Chord>> tonallist = new();
            List<Chord> permuted = PermuteList(octave);
            foreach (Chord ch in permuted)
                tonallist.Add(AllTonalities(ch));
            return tonallist;
        }

        public List<List<Chord>> PermuteTranspose(int octave = -1)
        {
            List<List<Chord>> tonallist = new();
            List<Chord> transposed = AllTonalities();
            foreach (Chord ch in transposed)
                tonallist.Add(ch.PermuteList());
            return tonallist;
        }

        public virtual List<Chord> PermuteTransposeList(int octave = -1)
        {
            List<Chord> tonallist = new();
            List<Chord> transposed = AllTonalities();
            foreach (Chord ch in transposed)
                tonallist.AddRange(ch.PermuteList(octave));
            return tonallist;
        }

        public virtual List<Chord> TransposePermuteList(int octave = -1)
        {
            List<Chord> tonallist = new();
            List<Chord> permuted = PermuteList(octave);
            foreach (Chord ch in permuted)
                tonallist.AddRange(AllTonalities(ch));
            return tonallist;
        }

        /// <summary>
        /// ////////////////TEST SECTION///////////////////
        /// </summary>



        public static void DisplayTable(List<Chord> list)
        {
            //foreach (Chord ch in list)
            //    ch.Display();
            StringOutput.Display(list);
        }

        public new void DisplayInline()
        {
            foreach (Note note in notes)
            {
                note.DisplayInline();
            }
        }

        public static void DisplayInline(List<Chord> list)
        {
            foreach (Chord ch in list)
            {
                ch.DisplayInline();
                Console.WriteLine();
            }

        }

        //public new void Play()
        //{
        //    Player.Play(this);
        //}


        //public new void Test()
        //{
        //    DisplayInline();
        //    Play();
        //}

        //public static void Test(List<Chord> list)
        //{
        //    foreach (Chord ch in list)
        //    {
        //        ch.Test();
        //    }
        //}

        public static Chord operator +(Chord A, Chord B)
        {
            Chord C = (Chord)A.Clone();
            foreach (Note note in B.Notes)
            {
                Note temp = (Note)note.Clone();
                C.AddNote(note);
            }
            C.Adjust();
            return C;
        }

        public static Chord operator +(Note A, Chord B)
        {
            Chord C = new();
            C.AddNote(A);
            foreach (Note note in B.Notes)
            {
                Note temp = (Note)note.Clone();
                C.AddNote(note);
            }
            C.Adjust();
            return C;
        }


        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Chord other = (Chord)obj;
            if (Notes.Count() != other.Notes.Count()) return false;
            for (int i = 0; i < Notes.Count; i++)
            {
                Note note = Notes[i];
                if (note != other.Notes[i]) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 0;
                foreach (Note note in Notes)
                {
                    hash = hash + note.GetHashCode();
                }
                //Console.WriteLine("HashCode = " + hash);
                return hash;
            }
        }

        public int CompareTo(object? obj)
        {
            if (obj is Chord other)
            {
                if (Notes.Count() < other.Notes.Count()) return 1;
                else if (Notes.Count() > other.Notes.Count()) return -1;
                else
                {
                    int non = Notes.Count();
                    if (non == 0) return 0;
                    for (int i = 0; i < non; i++)
                    {
                        int choice = (Notes[i].CompareTo(other.Notes[i]));
                        if (choice != 0) return choice;
                    }
                    return 0;
                }
            }
            else throw new ArgumentException("Object is not of type Note");
        }

        /// <summary>
        /// Клонування об'єктів
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            Chord clone = new();
            // Здійснюємо глибоке клонування для елементів Chord
            clone.notes = new List<Note>(this.notes.Count);
            foreach (Note note in this.notes)
            {
                clone.notes.Add((Note)note.Clone());
            }
            return clone;
        }

        public object Clone(Chord A)
        {
            Chord clone = new();
            // Здійснюємо глибоке клонування для елементів Chord
            clone.notes = new List<Note>(A.notes.Count);
            foreach (Note note in this.notes)
            {
                clone.notes.Add((Note)note.Clone());
            }
            return clone;
        }

        public static List<Chord> Clone(List<Chord> original)
        {
            List<Chord> clonedlist = new();
            foreach (Chord originalChord in original)
            {
                Chord clonedChord = (Chord)originalChord.Clone();
                clonedlist.Add(clonedChord);
            }
            return clonedlist;
        }

        public static Chord[] Clone(Chord[] original)
        {
            Chord[] cloned = new Chord[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                Chord clonedChord = (Chord)original[i].Clone();
                cloned[i] = clonedChord;
            }
            return cloned;
        }

        public static Chord CreateRandom(int voices = 3, int octaves = 4)
        {
            var rndChord = new Chord();
            if (voices <= 0) return rndChord;
            try
            {
                for (int i = 0; i < voices; i++)
                {
                    var n = Note.GenerateRandomNote(octaves);
                    rndChord.AddNote(n);
                }
                rndChord.Adjust(); // привести ноти в корректний порядок/октави
            }
            catch (Exception ex)
            {
                // зберегти існуючу поведінку логування у проекті
                Music.Messages.MessageL(12, "CreateRandom chord error: " + ex.Message);
            }
            return rndChord;
        }

        public static Chord CreateRandomFrom(Note root, CHORDS chordType, Random? rnd = null, bool randomVoicing = true)
        {
            rnd ??= new Random();
            var chord = new Chord();
            if (root is null) return chord;

            // add root note (clone to avoid mutating caller)
            var rootClone = (Note)root.Clone();
            chord.AddNote(rootClone);

            // helper to add transposed note relative to root
            void AddInterval(INTERVALS interval, QUALITY quality)
            {
                var n = (Note)root.Clone();
                var t = n.TransposeToNote(interval, quality);
                // apply small random octave shift (0 or1) to vary voicing
                if (randomVoicing && rnd.Next(2) == 0)
                    t.OctUp(1);
                chord.AddNote(t);
            }

            // small helper to choose triad quality randomly
            QUALITY RandomTriQuality()
            {
                var p = rnd.NextDouble();
                if (p < 0.4) return QUALITY.MAJ;
                if (p < 0.8) return QUALITY.MIN;
                if (p < 0.9) return QUALITY.AUG;
                return QUALITY.DIM;
            }

            // choose intervals depending on chordType with randomized qualities
            switch (chordType)
            {
                case CHORDS.TRI:
                    {
                        var triQ = RandomTriQuality();
                        if (triQ == QUALITY.AUG)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MAJ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.AUG);
                        }
                        else if (triQ == QUALITY.DIM)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MIN);
                            AddInterval(INTERVALS.QUINTA, QUALITY.DIM);
                        }
                        else
                        {
                            AddInterval(INTERVALS.TERZIA, triQ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.PERFECT);
                        }
                        break;
                    }

                case CHORDS.SEXT:
                    {
                        var triQ = RandomTriQuality();
                        var sixthQ = rnd.NextDouble() < 0.7 ? QUALITY.MAJ : QUALITY.MIN;
                        // triad
                        if (triQ == QUALITY.AUG)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MAJ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.AUG);
                        }
                        else if (triQ == QUALITY.DIM)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MIN);
                            AddInterval(INTERVALS.QUINTA, QUALITY.DIM);
                        }
                        else
                        {
                            AddInterval(INTERVALS.TERZIA, triQ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.PERFECT);
                        }
                        // add sixth
                        AddInterval(INTERVALS.SEKSTA, sixthQ);
                        break;
                    }

                case CHORDS.SEPT:
                    {
                        var triQ = RandomTriQuality();
                        var sevQ = rnd.NextDouble() < 0.75 ? QUALITY.MIN : QUALITY.MAJ; // mostly dom7
                        if (triQ == QUALITY.AUG)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MAJ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.AUG);
                        }
                        else if (triQ == QUALITY.DIM)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MIN);
                            AddInterval(INTERVALS.QUINTA, QUALITY.DIM);
                        }
                        else
                        {
                            AddInterval(INTERVALS.TERZIA, triQ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.PERFECT);
                        }
                        AddInterval(INTERVALS.SEPTYMA, sevQ);
                        break;
                    }

                case CHORDS.NONACORD:
                case CHORDS.CORD69:
                    {
                        var triQ = RandomTriQuality();
                        var sevQ = rnd.NextDouble() < 0.7 ? QUALITY.MIN : QUALITY.MAJ;
                        var ninthQ = rnd.NextDouble() < 0.7 ? QUALITY.MAJ : QUALITY.MIN;
                        // triad
                        if (triQ == QUALITY.AUG)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MAJ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.AUG);
                        }
                        else if (triQ == QUALITY.DIM)
                        {
                            AddInterval(INTERVALS.TERZIA, QUALITY.MIN);
                            AddInterval(INTERVALS.QUINTA, QUALITY.DIM);
                        }
                        else
                        {
                            AddInterval(INTERVALS.TERZIA, triQ);
                            AddInterval(INTERVALS.QUINTA, QUALITY.PERFECT);
                        }
                        // seventh
                        AddInterval(INTERVALS.SEPTYMA, sevQ);
                        // ninth expressed as secunda relative to octave
                        AddInterval(INTERVALS.SECUNDA, ninthQ);
                        if (chordType == CHORDS.CORD69)
                        {
                            var sixthQ = rnd.NextDouble() < 0.7 ? QUALITY.MAJ : QUALITY.MIN;
                            AddInterval(INTERVALS.SEKSTA, sixthQ);
                        }
                        break;
                    }

                default:
                    // fallback: leave root only
                    break;
            }

            // finalize: adjust and optionally apply random inversions
            chord.Adjust();
            if (randomVoicing)
            {
                int inversions = rnd.Next(0, Math.Max(1, chord.Size()));
                for (int i = 0; i < inversions; i++)
                    chord.Inversion();
            }

            return chord;
        }

        // Overload: create chord from root, chord type and exact number of voices
        public static Chord CreateRandomFrom(Note root, CHORDS chordType, int voices, Random? rnd = null, bool randomVoicing = true)
        {
            rnd ??= new Random();
            var chord = new Chord();
            if (root is null) return chord;

            // start with root
            var rootClone = (Note)root.Clone();
            chord.AddNote(rootClone);

            // local helper to add interval
            void AddIntervalLocal(INTERVALS interval, QUALITY quality)
            {
                var n = (Note)root.Clone();
                var t = n.TransposeToNote(interval, quality);
                if (randomVoicing && rnd.Next(2) == 0) t.OctUp(1);
                chord.AddNote(t);
            }

            // local random triad quality selector copied from existing method
            QUALITY RandomTriQualityLocal()
            {
                var p = rnd.NextDouble();
                if (p < 0.55) return QUALITY.MAJ;
                if (p < 0.9) return QUALITY.MIN;
                if (p < 0.95) return QUALITY.AUG;
                return QUALITY.DIM;
            }

            // Build base triad (if voices >=2)
            if (voices >= 2)
            {
                var triQ = RandomTriQualityLocal();
                if (triQ == QUALITY.AUG)
                {
                    AddIntervalLocal(INTERVALS.TERZIA, QUALITY.MAJ);
                    AddIntervalLocal(INTERVALS.QUINTA, QUALITY.AUG);
                }
                else if (triQ == QUALITY.DIM)
                {
                    AddIntervalLocal(INTERVALS.TERZIA, QUALITY.MIN);
                    AddIntervalLocal(INTERVALS.QUINTA, QUALITY.DIM);
                }
                else
                {
                    AddIntervalLocal(INTERVALS.TERZIA, triQ);
                    AddIntervalLocal(INTERVALS.QUINTA, QUALITY.PERFECT);
                }
            }

            // If more voices requested, add further chord tones in the following order:
            //4th voice -> SEPTYMA (7th),5th -> SECUNDA (9th),6th -> SEKSTA (6th/13th),7th -> OCTAVA+SEPTYMA etc.
            var additional = new INTERVALS[] { INTERVALS.SEPTYMA, INTERVALS.SECUNDA, INTERVALS.SEKSTA, INTERVALS.OCTAVA, INTERVALS.SECUNDA };

            int idx = 0;
            while (chord.Size() < voices && idx < additional.Length)
            {
                var iv = additional[idx++];
                // choose quality reasonably: mostly minor for7th, major/minor for9th/6th
                QUALITY q = QUALITY.MAJ;
                switch (iv)
                {
                    case INTERVALS.SEPTYMA:
                        q = rnd.NextDouble() < 0.75 ? QUALITY.MIN : QUALITY.MAJ;
                        break;
                    case INTERVALS.SECUNDA:
                        q = rnd.NextDouble() < 0.7 ? QUALITY.MAJ : QUALITY.MIN;
                        break;
                    case INTERVALS.SEKSTA:
                        q = rnd.NextDouble() < 0.7 ? QUALITY.MAJ : QUALITY.MIN;
                        break;
                    default:
                        q = QUALITY.PERFECT;
                        break;
                }
                AddIntervalLocal(iv, q);
            }

            // If still less than required voices, repeat adding octaved tertia-based tones
            while (chord.Size() < voices)
            {
                // add octave tercia (decima-like)
                AddIntervalLocal(INTERVALS.TERZIA, RandomTriQualityLocal());
            }

            chord.Adjust();
            if (randomVoicing)
            {
                int inversions = rnd.Next(0, Math.Max(1, chord.Size()));
                for (int i = 0; i < inversions; i++) chord.Inversion();
            }

            return chord;
        }

    }
}
