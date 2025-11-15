using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    public class Scales : Scale
    {
        MODE mode;
        public Scales(Note nt, MODE mode)
        {
            List<Note> notes = new()
            {
                nt
            };
            AddNotes(nt, mode);
        }

        public Scales(string input, MODE mode)
        {
            List<Note> notes = new();
            Note nt = new(input);
            AddNotes(nt, mode);
        }

        private void AddNotes(Note nt, MODE mode)
        {
            switch (mode)
            {
                case MODE.dur:
                    this.mode = MODE.dur;
                    AddNote(nt);
                    AddNote(1, 1);
                    AddNote(2, 1);
                    AddNote(3, 0);
                    AddNote(4, 0);
                    AddNote(5, 1);
                    AddNote(6, 1);
                    break;
                case MODE.moll:
                    this.mode = MODE.dur;
                    AddNote(nt);
                    AddNote(1, 1);
                    AddNote(2, -1);
                    AddNote(3, 0);
                    AddNote(4, 0);
                    AddNote(5, -1);
                    AddNote(6, -1);
                    break;
                case MODE.chrome:
                    this.mode = MODE.dur;
                    AddNote(nt);
                    AddNote(0, 2);
                    AddNote(1, 1);
                    AddNote(2, -1);
                    AddNote(2, 1);
                    AddNote(3, 0);
                    AddNote(3, 2);
                    AddNote(4, 0);
                    AddNote(5, -1);
                    AddNote(5, 1);
                    AddNote(6, -1);
                    AddNote(6, 1);
                    break;
            }
        }
    }
}
