using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Music.Engine;
using static System.Net.Mime.MediaTypeNames;

namespace Music
{
    public class IntervalStructure
    {
        // позиції інтервалів в акорді
        public int? prima { set; get; }
        public int? secunda { set; get; }
        public int? terzia { set; get; }
        public int? quarta { set; get; }
        public int? quinta { set; get; }
        public int? seksta { set; get; }
        public int? septyma { set; get; }

        public IntervalStructure(ChordT chord)
        {

            prima = chord.If_note_in_Chord(INTERVALS.PRIMA);
            secunda = chord.If_note_in_Chord(INTERVALS.SECUNDA);
            terzia = chord.If_note_in_Chord(INTERVALS.TERZIA);
            quarta = chord.If_note_in_Chord(INTERVALS.QUARTA);
            quinta = chord.If_note_in_Chord(INTERVALS.QUINTA);
            seksta = chord.If_note_in_Chord(INTERVALS.SEKSTA);
            septyma = chord.If_note_in_Chord(INTERVALS.SEPTYMA);
        }

        public IntervalStructure(IntervalStructure intervals, CHORDS Chordtype)
        {
            if (Chordtype == CHORDS.UNKNOWN) intervals.MemberwiseClone();
            Iprima(intervals, Chordtype);
            Isecunda(intervals, Chordtype);
            Iterzia(intervals, Chordtype);
            Iquarta(intervals, Chordtype);
            Iquinta(intervals, Chordtype);
            Iseksta(intervals, Chordtype);
            Iseptyma(intervals, Chordtype);
        }

        public void Iprima(IntervalStructure ints, CHORDS Chordtype)
        {

            if (Chordtype == CHORDS.SEPT) prima = ints.prima;
            else if (Chordtype == CHORDS.TERZQ) prima = ints.quarta;
            else if (Chordtype == CHORDS.QUINTS) prima = ints.seksta;
            else if (Chordtype == CHORDS.SEC) prima = ints.secunda;
            else if (Chordtype == CHORDS.TRI) prima = ints.prima;
            else if (Chordtype == CHORDS.SEXT) prima = ints.seksta;
            else if (Chordtype == CHORDS.QSEXT) prima = ints.quarta;
            else if (Chordtype == CHORDS.HSEPT) prima = ints.prima;
            else if (Chordtype == CHORDS.HSEC) prima = ints.secunda;
            else if (Chordtype == CHORDS.HQUINTS) prima = ints.seksta;
            else if (Chordtype == CHORDS.ADDSEC) prima = ints.prima;
            else if (Chordtype == CHORDS.ADDQUARTA) prima = ints.prima;
            else if (Chordtype == CHORDS.NONACORD) prima = ints.prima;
            else if (Chordtype == CHORDS.NONACORD_1i) prima = ints.seksta;
            else if (Chordtype == CHORDS.NONACORD_2i) prima = ints.quarta;
            else if (Chordtype == CHORDS.NONACORD_3i) prima = ints.secunda;
            else if (Chordtype == CHORDS.NONACORD_4i) prima = ints.septyma;
            else if (Chordtype == CHORDS.UNDECCORD) prima = ints.prima;
            else if (Chordtype == CHORDS.UNDECCORD_1i) prima = ints.seksta;
            else if (Chordtype == CHORDS.UNDECCORD_2i) prima = ints.quarta;
            else if (Chordtype == CHORDS.UNDECCORD_3i) prima = ints.secunda;
            else if (Chordtype == CHORDS.UNDECCORD_4i) prima = ints.septyma;
            else if (Chordtype == CHORDS.UNDECCORD_5i) prima = ints.quinta;
            else prima = null;
        }

        public void Isecunda(IntervalStructure ints, CHORDS Chordtype)
        {
            if (Chordtype == CHORDS.NONACORD) secunda = ints.secunda;
            else if (Chordtype == CHORDS.ADDSEC) secunda = ints.secunda;
            else if (Chordtype == CHORDS.NONACORD_1i) secunda = ints.septyma;
            else if (Chordtype == CHORDS.NONACORD_2i) secunda = ints.quinta;
            else if (Chordtype == CHORDS.NONACORD_3i) secunda = ints.terzia;
            else if (Chordtype == CHORDS.NONACORD_4i) secunda = ints.prima;
            else if (Chordtype == CHORDS.UNDECCORD) secunda = ints.secunda;
            else if (Chordtype == CHORDS.UNDECCORD_1i) secunda = ints.septyma;
            else if (Chordtype == CHORDS.UNDECCORD_2i) secunda = ints.quinta;
            else if (Chordtype == CHORDS.UNDECCORD_3i) secunda = ints.terzia;
            else if (Chordtype == CHORDS.UNDECCORD_4i) secunda = ints.prima;
            else if (Chordtype == CHORDS.UNDECCORD_5i) secunda = ints.seksta;
            else secunda = null;
        }

        public void Iterzia(IntervalStructure ints, CHORDS Chordtype)
        {
            if (Chordtype == CHORDS.SEPT) terzia = ints.terzia;
            else if (Chordtype == CHORDS.TERZQ) terzia = ints.seksta;
            else if (Chordtype == CHORDS.QUINTS) terzia = ints.prima;
            else if (Chordtype == CHORDS.SEC) terzia = ints.quarta;
            else if (Chordtype == CHORDS.TRI) terzia = ints.terzia;
            else if (Chordtype == CHORDS.SEXT) terzia = ints.prima;
            else if (Chordtype == CHORDS.QSEXT) terzia = ints.seksta;
            else if (Chordtype == CHORDS.   HSEPT) terzia = ints.terzia;
            else if (Chordtype == CHORDS.HSEC) terzia = ints.quarta;
            else if (Chordtype == CHORDS.HQUINTS) terzia = ints.prima;
            else if (Chordtype == CHORDS.ADDSEC) terzia = ints.terzia;
            else if (Chordtype == CHORDS.ADDQUARTA) terzia = ints.terzia;
            else if (Chordtype == CHORDS.NONACORD) terzia = ints.terzia;
            else if (Chordtype == CHORDS.NONACORD_1i) terzia = ints.prima;
            else if (Chordtype == CHORDS.NONACORD_2i) terzia = ints.seksta;
            else if (Chordtype == CHORDS.NONACORD_3i) terzia = ints.quarta;
            else if (Chordtype == CHORDS.NONACORD_4i) terzia = ints.secunda;
            else if (Chordtype == CHORDS.UNDECCORD) terzia = ints.terzia;
            else if (Chordtype == CHORDS.UNDECCORD_1i) terzia = ints.prima;
            else if (Chordtype == CHORDS.UNDECCORD_2i) terzia = ints.seksta;
            else if (Chordtype == CHORDS.UNDECCORD_3i) terzia = ints.quarta;
            else if (Chordtype == CHORDS.UNDECCORD_4i) terzia = ints.secunda;
            else if (Chordtype == CHORDS.UNDECCORD_5i) terzia = ints.septyma;
            else terzia = ints.terzia;
        }

        public void Iquarta(IntervalStructure ints, CHORDS Chordtype)
        {
            if (Chordtype == CHORDS.UNDECCORD) quarta = ints.quarta;
            else if (Chordtype == CHORDS.ADDQUARTA) quarta = ints.quarta;
            else if (Chordtype == CHORDS.UNDECCORD_1i) quarta = ints.secunda;
            else if (Chordtype == CHORDS.UNDECCORD_2i) quarta = ints.septyma;
            else if (Chordtype == CHORDS.UNDECCORD_3i) quarta = ints.quinta;
            else if (Chordtype == CHORDS.UNDECCORD_4i) quarta = ints.terzia;
            else if (Chordtype == CHORDS.UNDECCORD_5i) quarta = ints.prima;
            else quarta = null;
        }

        public void Iquinta(IntervalStructure ints, CHORDS Chordtype)
        {
            if (Chordtype == CHORDS.SEPT) quinta = ints.quinta;
            else if (Chordtype == CHORDS.TERZQ) quinta = ints.prima;
            else if (Chordtype == CHORDS.QUINTS) quinta = ints.terzia;
            else if (Chordtype == CHORDS.SEC) quinta = ints.seksta;
            else if (Chordtype == CHORDS.TRI) quinta = ints.quinta;
            else if (Chordtype == CHORDS.SEXT) quinta = ints.terzia;
            else if (Chordtype == CHORDS.QSEXT) quinta = ints.prima;
            else if (Chordtype == CHORDS.HSEPT) quinta = null;
            else if (Chordtype == CHORDS.HSEC) quinta = null;
            else if (Chordtype == CHORDS.HQUINTS) quinta = null;
            else if (Chordtype == CHORDS.ADDSEC) quinta = ints.quinta;
            else if (Chordtype == CHORDS.ADDQUARTA) quinta = ints.quinta;
            else if (Chordtype == CHORDS.NONACORD) quinta = ints.quinta;
            else if (Chordtype == CHORDS.NONACORD_1i) quinta = ints.terzia;
            else if (Chordtype == CHORDS.NONACORD_2i) quinta = ints.prima;
            else if (Chordtype == CHORDS.NONACORD_3i) quinta = ints.seksta;
            else if (Chordtype == CHORDS.NONACORD_4i) quinta = ints.quarta;
            else if (Chordtype == CHORDS.UNDECCORD) quinta = ints.quinta;
            else if (Chordtype == CHORDS.UNDECCORD_1i) quinta = ints.terzia;
            else if (Chordtype == CHORDS.UNDECCORD_2i) quinta = ints.prima;
            else if (Chordtype == CHORDS.UNDECCORD_3i) quinta = ints.seksta;
            else if (Chordtype == CHORDS.UNDECCORD_4i) quinta = ints.quarta;
            else if (Chordtype == CHORDS.UNDECCORD_5i) quinta = ints.secunda;
            else quinta = ints.quinta;
        }

        public void Iseksta(IntervalStructure ints, CHORDS Chordtype)
        {
            if (Chordtype == CHORDS.TERZDEC) seksta = ints.seksta;
            if (Chordtype == CHORDS.CLUSTER) seksta = ints.seksta;
            if (Chordtype == CHORDS.TERZDEC) seksta = ints.seksta;
            else seksta = null;
        }

        public void Iseptyma(IntervalStructure ints, CHORDS Chordtype)
        {
            if (Chordtype == CHORDS.SEPT) septyma = ints.septyma;
            else if (Chordtype == CHORDS.TERZQ) septyma = ints.terzia;
            else if (Chordtype == CHORDS.QUINTS) septyma = ints.quinta;
            else if (Chordtype == CHORDS.SEC) septyma = ints.prima;
            else if (Chordtype == CHORDS.TRI) septyma = null;
            else if (Chordtype == CHORDS.SEXT) septyma = null;
            else if (Chordtype == CHORDS.QSEXT) septyma = null;
            else if (Chordtype == CHORDS.HSEPT) septyma = ints.septyma;
            else if (Chordtype == CHORDS.HSEC) septyma = ints.prima;
            else if (Chordtype == CHORDS.HQUINTS) septyma = ints.quinta;
            else if (Chordtype == CHORDS.ADDSEC) septyma = ints.septyma;
            else if (Chordtype == CHORDS.ADDQUARTA) septyma = ints.septyma;
            else if (Chordtype == CHORDS.NONACORD) septyma = ints.septyma;
            else if (Chordtype == CHORDS.NONACORD_1i) septyma = ints.quinta;
            else if (Chordtype == CHORDS.NONACORD_2i) septyma = ints.terzia;
            else if (Chordtype == CHORDS.NONACORD_3i) septyma = ints.prima;
            else if (Chordtype == CHORDS.NONACORD_4i) septyma = ints.seksta;
            else if (Chordtype == CHORDS.UNDECCORD) septyma = ints.septyma;
            else if (Chordtype == CHORDS.UNDECCORD_1i) septyma = ints.quinta;
            else if (Chordtype == CHORDS.UNDECCORD_2i) septyma = ints.terzia;
            else if (Chordtype == CHORDS.UNDECCORD_3i) septyma = ints.prima;
            else if (Chordtype == CHORDS.UNDECCORD_4i) septyma = ints.seksta;
            else if (Chordtype == CHORDS.UNDECCORD_5i) septyma = ints.quarta;
            else septyma = ints.septyma;
        }



    }
}
