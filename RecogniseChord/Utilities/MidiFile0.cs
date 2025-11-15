using NAudio.Midi;
using static Music.Messages;
using static Music.Menues;
using System.Diagnostics;

namespace Music
{


    public class MidiFile0 : MidiFile
    {
        public int MidiFileType { get; set; }
        public int PPQN { get; set; }
        public int TrackNumber { get; set; }
        public int ChannelNumber { get; set; }
        public int TempoBpm { get; set; }

        public new MidiEventCollection Events { get; set; }

        public MidiFile0(string filename)
        : base(filename, strictChecking: true)
        {
            MidiFileType = 0;
            PPQN = 480;
            TrackNumber = 0;
            ChannelNumber = 1;
            TempoBpm = 120;
            Events = new MidiEventCollection(MidiFileType, PPQN);
        }

        public MidiFile0(Melody melody, string filename="ouptut.mid")
        : base(filename, strictChecking: true)
        {
            MidiFileType = 0;
            PPQN = 480;            
            ChannelNumber = 1;            
            Events = new MidiEventCollection(MidiFileType, PPQN);
            MelodyToTrack(melody, ChannelNumber, Events);
        }


        public MidiFile0(Chord chord, string filename = "ouptut.mid")
        : base(filename, strictChecking: true)
        {
            MidiFileType = 0;
            PPQN = 480;
            ChannelNumber = 1;
            Events = new MidiEventCollection(MidiFileType, PPQN);
            ChordToTrack(chord, ChannelNumber, Events);
        }

        public MidiFile0(IEnumerable<Chord> chords, string filename = "ouptut.mid")
        : base(filename, strictChecking: true)
        {
            MidiFileType = 0;
            PPQN = 480;
            ChannelNumber = 1;
            Events = new MidiEventCollection(MidiFileType, PPQN);
            ChordProgressionToTrack(chords, ChannelNumber, Events);
        }



        public void Import(string filePath)
        {
            var midiFile = new MidiFile(filePath, true);
            Events = midiFile.Events;
            MidiFileType = midiFile.FileFormat;
            TrackNumber = midiFile.Tracks;
            PPQN = midiFile.DeltaTicksPerQuarterNote;
        }

        public static void OpenMidi(string filename)
        {
            MessageL(COLORS.blue, $"midi saved as {filename}");
            var ifopen = (EnterBool("Open Midi?"));
            if (ifopen)
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = filename,
                    UseShellExecute = true // Use the default program to open the file
                };
                Process.Start(psi);
            }
        }



        public void Play()
        {
            var midiOut = new MidiOut(0);  
            long previousTime = 0;

            try
            {
                // All tracks
                for (int i = 0; i < Events.Tracks; i++)
                {
                    // All events
                    foreach (var midiEvent in Events.GetTrackEvents(i))
                    {
                        // Різниця часу між поточною подією та попередньою
                        long timeDelta = midiEvent.AbsoluteTime - previousTime;
                        previousTime = midiEvent.AbsoluteTime;

                        // Чекаємо, якщо є час між подіями
                        if (timeDelta > 0)
                        {
                            Thread.Sleep((int)timeDelta);  // Спочатку спимо, щоб відтворити з затримкою
                        }

                        // Відправляємо подію в MIDI-порт
                        midiOut.Send(midiEvent.GetAsShortMessage()); // Відправка події
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Закриття MidiOut після використання
                midiOut?.Dispose();
            }
        }


        public static void Play(MidiEventCollection events)
        {
            var midiOut = new MidiOut(0);  // Використовуємо перший MIDI-порт (можна налаштувати на інший порт)
            long previousTime = 0;

            try
            {
                // Проходимо по кожному треку
                for (int i = 0; i < events.Tracks; i++)
                {
                    // Проходимо по кожній події в колекції
                    foreach (var midiEvent in events.GetTrackEvents(i))
                    {
                        // Різниця часу між поточною подією та попередньою
                        long timeDelta = midiEvent.AbsoluteTime - previousTime;
                        previousTime = midiEvent.AbsoluteTime;

                        // Чекаємо, якщо є час між подіями
                        if (timeDelta > 0)
                        {
                            Thread.Sleep((int)timeDelta);  // Спочатку спимо, щоб відтворити з затримкою
                        }

                        // Відправляємо подію в MIDI-порт
                        midiOut.Send(midiEvent.GetAsShortMessage()); // Відправка події
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Закриття MidiOut після використання
                midiOut?.Dispose();
            }
        }




        public static void Play(Note note)
        {
            var midiOut = new MidiOut(0);
            try
            {
                int PPQN = Globals.PPQN;
                int channel = Globals.ChannelNumber;
                var noteOnEvent = new NoteOnEvent(0, channel, note.MidiNote, 127, 0);
                var noteOffEvent = new NoteEvent(note.MidiDur, channel, MidiCommandCode.NoteOff, note.MidiNote, 0);
                midiOut.Send(MidiMessage.StartNote(noteOnEvent.NoteNumber, noteOnEvent.Velocity, noteOnEvent.Channel).RawData);
                Thread.Sleep((int)note.MidiDur);
                midiOut.Send(MidiMessage.StopNote(noteOffEvent.NoteNumber, noteOffEvent.Velocity, noteOffEvent.Channel).RawData);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Закриття MidiOut після використання
                midiOut?.Dispose();
            }
        }

        public static void Play(Melody melody)
        {
            var midiOut = new MidiOut(0);
            try
            {
                int PPQN = Globals.PPQN;
                int channel = Globals.ChannelNumber;
                foreach (var note in melody)
                {
                    var noteOnEvent = new NoteOnEvent(0, channel, note.MidiNote, 127, 0);
                    var noteOffEvent = new NoteEvent(note.MidiDur, channel, MidiCommandCode.NoteOff, note.MidiNote, 0);
                    midiOut.Send(MidiMessage.StartNote(noteOnEvent.NoteNumber, noteOnEvent.Velocity, noteOnEvent.Channel).RawData);
                    Thread.Sleep(note.MidiDur);
                    midiOut.Send(MidiMessage.StopNote(noteOffEvent.NoteNumber, noteOffEvent.Velocity, noteOffEvent.Channel).RawData);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Закриття MidiOut після використання
                midiOut?.Dispose();
            }
        }

        public static void Play(Chord chord)
        {

            var midiOut = new MidiOut(0);
            try
            {
                int PPQN = Globals.PPQN;
                int channel = Globals.ChannelNumber;
                foreach (var note in chord)
                {
                    var noteOnEvent = new NoteOnEvent(0, channel, note.MidiNote, 127, 0);
                    midiOut.Send(MidiMessage.StartNote(noteOnEvent.NoteNumber, noteOnEvent.Velocity, noteOnEvent.Channel).RawData);
                }
                Thread.Sleep(chord[0].MidiDur);
                foreach (var note in chord)
                {
                    var noteOffEvent = new NoteEvent(note.MidiDur, channel, MidiCommandCode.NoteOff, note.MidiNote, 0);
                    midiOut.Send(MidiMessage.StartNote(noteOffEvent.NoteNumber, noteOffEvent.Velocity, noteOffEvent.Channel).RawData);
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Закриття MidiOut після використання
                midiOut?.Dispose();
            }
        }

        public static void ShowEvents(MidiFile midiFile)
        {
            var colection = midiFile.Events;

            Console.WriteLine("Midi Events List");
            for (int trackIndex = 0; trackIndex < colection.Count(); trackIndex++)
            {
                Console.WriteLine($"Track {trackIndex + 1}:");
                foreach (MidiEvent midiEvent in colection[trackIndex])
                {
                    Console.WriteLine(midiEvent.ToString());
                }
            }
        }


        private static void ChordToTrack(Chord chord, int channel, MidiEventCollection events, int startTime = 0)
        {
            foreach (var note in chord)
            {
                var noteOnEvent = new NoteOnEvent(startTime, channel, note.MidiNote, 127, 0);
                events.AddEvent(noteOnEvent, 1);
            }

            foreach (var note in chord)
            {
                var noteOffEvent = new NoteEvent(startTime + note.MidiDur, channel, MidiCommandCode.NoteOff, note.MidiNote, 0);
                events.AddEvent(noteOffEvent, 1);
            }
        }


        private static void ChordProgressionToTrack<T>(IEnumerable<T> chords, int channel, MidiEventCollection events, int startTime = 0) 
            where T:Chord
        {            
            foreach (var chord in chords)
            {

                foreach (var note in chord)
                {
                    var noteOnEvent = new NoteOnEvent(startTime, channel, note.MidiNote, 127, 0);
                    events.AddEvent(noteOnEvent, 1);
                }

                foreach (var note in chord)
                {
                    var noteOffEvent = new NoteEvent(startTime + note.MidiDur, channel, MidiCommandCode.NoteOff, note.MidiNote, 0);
                    events.AddEvent(noteOffEvent, 1);
                }
                startTime += chord.Notes[0].MidiDur;
            }
        }

        private static void MelodyToTrack(Melody melody, int channel, MidiEventCollection events)
        {
            // Записуємо NoteOn події
            int noteOnTime = 0;
            foreach (var note in melody)
            {
                if (!note.Rest)//якщо не пауза
                {
                    var noteOnEvent = new NoteOnEvent(noteOnTime, channel, note.MidiNote, 127, note.MidiDur);
                    events.AddEvent(noteOnEvent, 1);
                }
                noteOnTime += note.MidiDur;
                
            }

            // Записуємо NoteOff події
            int noteOffTime = 0;
            foreach (var note in melody)
            {
                noteOffTime += note.MidiDur;
                var noteOffEvent = new NoteEvent(noteOffTime, channel, MidiCommandCode.NoteOff, note.MidiNote, 0);
                events.AddEvent(noteOffEvent, 1);
            }
        }

        //
        // Summary
        //
        //  saving to midiFile
        public static bool SaveMidi(Melody melody, string fileName = "output.mid")
        {
            int channel;
            MidiEventCollection collection;
            Initialize(out channel, out collection);
            MelodyToTrack(melody, channel, collection);
            try
            {
                collection.PrepareForExport();
                Export(fileName, collection);
                Console.WriteLine($"file is being saved as {Path.GetFullPath(fileName)}");
                return true;
            }
            catch
            {
                Messages.ErrorMessage("Failed to save file");
                return false;
            }

        }

        public static bool SaveMidi(Chord chord, string fileName = "output.mid")
        {

            int channel;
            MidiEventCollection events;
            Initialize(out channel, out events);

            ChordToTrack(chord, channel, events);

            try
            {
                events.PrepareForExport();
                Export(fileName, events);
                Console.WriteLine($"file is being saved as {Path.GetFullPath(fileName)}");
                return true;
            }
            catch
            {
                Messages.ErrorMessage("Failed to save file");
                return false;
            }
        }


        public static bool SaveMidi<T>(List<T> chords, string fileName = "output.mid")
            where T:Chord
        {

            int channel;
            MidiEventCollection events;
            Initialize(out channel, out events);

            ChordProgressionToTrack(chords, channel, events);

            try
            {
                events.PrepareForExport();
                Export(fileName, events);
                Console.WriteLine($"file is being saved as {Path.GetFullPath(fileName)}");
                return true;
            }
            catch
            {
                Messages.ErrorMessage("Failed to save file");
                return false;
            }
        }


        private static void Initialize(out int channel, out MidiEventCollection events)
        {
            long absoluteTime = 0;
            channel = 1;
            int beatsPerMinute = 120;
            int patchNumber = 0;
            events = new MidiEventCollection(Globals.MidiFileType, Globals.PPQN);
            events.AddEvent(new TextEvent("C# generated stream", MetaEventType.TextEvent, absoluteTime), Globals.TrackNumber);
            ++absoluteTime;
            events.AddEvent(new TempoEvent(CalculateMicrosecondsPerQuaterNote(beatsPerMinute), absoluteTime), Globals.TrackNumber);
            events.AddEvent(new PatchChangeEvent(0, Globals.ChannelNumber, patchNumber), Globals.TrackNumber);
        }

        private static int CalculateMicrosecondsPerQuaterNote(int bpm)
        {
            return 60 * 1000 * 1000 / bpm;
        }

    }


}




