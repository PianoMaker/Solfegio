using NAudio.Wave;
using RecogniseChord.Pages;

namespace Music
{
    /// <summary>
    /// Loads a single piano reference sample (A4) from several possible locations and provides
    /// resampling/mixing helpers. Designed to be registered as a singleton and receive
    /// IWebHostEnvironment via constructor injection.
    /// </summary>
    public class SamplePiano
    {
        // Configurable path or URL (webroot-relative by default)
        public string BaseSamplePath { get; set; } 

        private static readonly string CacheFolder = Path.Combine(Path.GetTempPath(), "RecogniseChord_Samples");
        private static readonly string CachedFileName = "base_A4_sample.wav";
        private static readonly object _lock = new();

        private float[]? _baseSamples;
        private int _baseSampleRate;


        public SamplePiano(string root)
        {
            BaseSamplePath = root;
            LoadBaseSample();
        }


        public void LoadBaseSample()
        {
            if (!File.Exists(BaseSamplePath))
                throw new FileNotFoundException($"Base sample not found: {BaseSamplePath}");

            using var reader = new AudioFileReader(BaseSamplePath);
            _baseSampleRate = reader.WaveFormat.SampleRate;

            var allSamples = new List<float>();
            var buffer = new float[_baseSampleRate * reader.WaveFormat.Channels];
            int read;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (reader.WaveFormat.Channels == 1)
                {
                    for (int i = 0; i < read; i++)
                        allSamples.Add(buffer[i]);
                }
                else
                {
                    for (int i = 0; i < read; i += reader.WaveFormat.Channels)
                    {
                        float sum = 0;
                        int ch = 0;
                        for (int c = 0; c < reader.WaveFormat.Channels && i + c < read; c++)
                        {
                            sum += buffer[i + c];
                            ch++;
                        }
                        allSamples.Add(sum / ch);
                    }
                }
            }

            _baseSamples = allSamples.ToArray();

            if (_baseSamples.Length == 0)
                throw new InvalidOperationException("Base sample file was loaded, but contains no audio data.");
        }

        /// <summary>
        /// Linear resampling + looping sustain of base sample to target frequency/duration.
        /// base sample assumed to be A4 (440 Hz).
        /// </summary>
        public float[] GetResampledNote(double targetFreq, int durationMs, int outSampleRate)
        {
            if (_baseSamples == null) throw new InvalidOperationException("Base sample not loaded.");
            double ratio = targetFreq / 440.0;
            int outSamples = Math.Max(1, (int)Math.Round(outSampleRate * (durationMs / 1000.0)));
            var outBuf = new float[outSamples];

            double inputIndexScale = 1 / ratio;
            int inputLen = _baseSamples.Length;

            for (int i = 0; i < outSamples; i++)
            {
                double inIdx = i * inputIndexScale;
                double pos = inIdx % inputLen;
                if (pos < 0) pos += inputLen;
                int i0 = (int)Math.Floor(pos);
                int i1 = i0 + 1;
                if (i1 >= inputLen) i1 = 0;
                double frac = pos - i0;
                outBuf[i] = (float)(_baseSamples[i0] * (1.0 - frac) + _baseSamples[i1] * frac);
            }

            // small release tail
            int tail = Math.Min((int)(0.02 * outSampleRate), outSamples);
            for (int t = 0; t < tail; t++)
            {
                double g = 1.0 - (t / (double)tail);
                outBuf[outSamples - 1 - t] *= (float)g;
            }

            return outBuf;
        }

        /// <summary>
        /// Mix notes (in Hz) and write WAV to fullPath. Returns true on success.
        /// </summary>
        public bool TryRenderChordToWav(List<double> freqs, int activeMs, string fullPath, int sampleRate = 44100)
        {
            Console.WriteLine("TryRenderChordToWav is runnning");
            try
            {
                //if (_baseSamples == null) throw new InvalidOperationException("Base sample not loaded.");

                int outSamples = Math.Max(1, (int)Math.Round(sampleRate * ((activeMs + 50) / 1000.0)));
                var mix = new float[outSamples];

                foreach (var f in freqs)
                {
                    Console.Write(f  + " ");
                    if (f <= 0) continue;
                    var buf = GetResampledNote(f, activeMs + 50, sampleRate);
                    int len = Math.Min(buf.Length, mix.Length);
                    for (int i = 0; i < len; i++) mix[i] += buf[i];
                }
                Console.WriteLine();

                // normalize with headroom
                float max = 0f;
                for (int i = 0; i < mix.Length; i++)
                {
                    var v = Math.Abs(mix[i]);
                    if (v > max) max = v;
                }

                if (max > 0f)
                {
                    float scale = max > 1f ? (1f / max) * 0.95f : 0.95f;
                    for (int i = 0; i < mix.Length; i++) mix[i] *= scale;
                }

                var wf = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
                using var writer = new WaveFileWriter(fullPath, wf);
                writer.WriteSamples(mix, 0, mix.Length);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SamplePiano.TryRenderChordToWav failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear cached sample (dev helper).
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _baseSamples = null;
                _baseSampleRate = 0;
                try
                {
                    var t = Path.Combine(CacheFolder, CachedFileName);
                    if (File.Exists(t)) File.Delete(t);
                }
                catch { }
            }
        }
    }
}
