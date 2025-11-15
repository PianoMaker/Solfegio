using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    public delegate void Sorting(List<ChordT> chords, Comparison<ChordT> conparison);
    public delegate void Comparer(ChordT first, ChordT second);
    public static class Sort
    {
        public static Sorting SortChords = (chords, comparison) =>
        chords.Sort(comparison);

        public static Comparison<ChordT> SortByRange()
        {
            return (first, second) => first.Range().CompareTo(second.Range());
        }

        public static Comparison<ChordT> SortBySharpness()
        {
            return (first, second) => first.Sharpness().CompareTo(second.Sharpness());
        }

        public static Comparison<ChordT> SortBySharpness_Desc()
        {
            return (first, second) => second.Sharpness().CompareTo(first.Sharpness());
        }

        public static Comparison<ChordT> SortByBase()
        {
            return (first, second) => first.Notes[0].CompareTo(second.Notes[0]);
        }
    }
}
