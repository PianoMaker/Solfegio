using System.Data;
using System.Numerics;
using static System.Activator;
using static Music.Interval;

namespace Music
{
    public class PermutationsGenerator<T>
    {
        public List<List<T>> GeneratePermutations(List<T> originalList)
        {
            List<List<T>> result = new();
            Permute(originalList, 0, originalList.Count - 1, result);
            return result;
        }

        private void Permute(List<T> list, int left, int right, List<List<T>> result)
        {
            if (left == right)
            {
                result.Add(new List<T>(list));
            }
            else
            {
                for (int i = left; i <= right; i++)
                {
                    PermutationsGenerator<T>.Swap(list, left, i);
                    Permute(list, left + 1, right, result);
                    PermutationsGenerator<T>.Swap(list, left, i); // Відновлення порядку для наступних ітерацій
                }
            }
        }

        private static void Swap(List<T> list, int i, int j)
        {
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }


    /// <summary>
    /// //////////IMPLEMENTATOR////////////
    /// </summary>
    public static class ChordPermutation
    {
        public static List<T> Permute<T>(T chord) where T : Scale//просто спрощений запис
        {
            return PermuteToAdjustedList(chord);
        }


        public static List<T> PermuteToList<T>(T chord) where T : Scale // генерування усіх можливих розташувань
        {
            if (chord.Notes.Count == 0) throw new ArgumentNullException("No chord entered while trying to create permutations");

            PermutationsGenerator<Note> generator = new();

            var permutations = generator.GeneratePermutations(chord.Notes);

            List<T> list = new();
            foreach (List<Note> notes in permutations)
            {
                T? newchord = CreateInstance(typeof(T), new object[] { notes }) as T;
                //newchord.Adjust(0);
                if (newchord.Notes.Count > 0) list.Add(newchord);
            }
            return list;
        }

        public static T[] PermuteToArray<T>(T scale) where T : Scale // генерування усіх можливих розташувань
        {
            PermutationsGenerator<Note> generator = new();

            List<List<Note>> permutations = generator.GeneratePermutations(scale.Notes);

            T[] list = new T[permutations.Count];
            for (int i = 0; i < permutations.Count; i++)
            {
                T? newchord = CreateInstance(typeof(T), new object[] { permutations[i] }) as T;
                //newchord.Adjust(0);
                if (newchord != null) list[i] = newchord;
            }
            return list;
        }

        public static List<T> PermuteToAdjustedList<T>(T scale, int octave = 0) where T : Scale  // генерування усіх можливих розташувань
        {

            List<T> permuted = PermuteToList(scale);
            List<T> clonedPermuted = new(permuted.Count);

            // Клонуємо об'єкти у список клонів
            foreach (T originalChord in permuted)
            {
                T clonedChord = (T)originalChord.Clone();
                clonedPermuted.Add(clonedChord);
            }

            // Застосовуємо зміни до клонів
            foreach (T chord in clonedPermuted)
                chord.Adjust(octave);
            return clonedPermuted;
        }

        public static T[] PermuteToAdjustedArray<T>(T scale, int octave = 0) where T : Scale
        {
            T[] permuted = PermuteToArray(scale);
            T[] clonedPermuted = new T[permuted.Length];

            for (int i = 0; i < permuted.Length; i++)
            {
                T clonedChord = (T)permuted[i].Clone();
                clonedPermuted[i] = clonedChord;
            }

            foreach (T chord in clonedPermuted)
                chord.Adjust(octave);

            return clonedPermuted;
        }

        public static List<T> TransposePermute<T>(T chord) where T : Chord
        {
            List<T> chords = new();
            T temp = (T)chord.Clone();
            List<T> permuted = PermuteToList(temp);
            foreach (T ch in permuted)
                chords.AddRange(AllTonalities(ch));
            return chords;
        }



        /*
                public static List<T> Permute<T>(T chord) where T : Scale, new()// генерування усіх можливих розташувань
                {
                    PermutationsGenerator<Note> generator = new PermutationsGenerator<Note>();

                    List<List<Note>> permutations = generator.GeneratePermutations(chord.Notes);

                    List<T> list = new List<T>();
                    foreach (List<Note> ch in permutations)
                    {
                        T newchord = new T();
                        //newchord.Adjust(0);
                        list.Add(newchord);
                    }
                    return list;
                }
        */
    }

}


