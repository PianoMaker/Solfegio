using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Music
{
    public static class AudioDiagnostics
    {
        public record AnalysisResult(double Peak, double PeakDb, double Rms, double RmsDb, long SampleCount);

        // Analyze existing WAV (path) — returns peak and RMS (linear and dBFS)
        public static AnalysisResult AnalyzeWav(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            using var reader = new AudioFileReader(path); // provides float samples
            return AnalyzeSampleProvider(reader);
        }

        // Analyze any IWaveProvider (e.g., new ChordWaveProvider(...))
        public static AnalysisResult AnalyzeProvider(IWaveProvider provider)
        {
            // convert to ISampleProvider for float samples
            var sp = provider.ToSampleProvider();
            return AnalyzeSampleProvider(sp);
        }

        private static AnalysisResult AnalyzeSampleProvider(ISampleProvider sp)
        {
            const int bufferSize = 8192;
            float[] buffer = new float[bufferSize];
            long totalSamples = 0;
            double maxAbs = 0.0;
            double sumSquares = 0.0;

            int read;
            while ((read = sp.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    var s = buffer[i];
                    var abs = Math.Abs(s);
                    if (abs > maxAbs) maxAbs = abs;
                    sumSquares += (double)s * (double)s;
                }
                totalSamples += read;
                // safety: avoid extremely long loop for streaming providers — you can break after desired duration
            }

            double rms = totalSamples > 0 ? Math.Sqrt(sumSquares / totalSamples) : 0.0;
            double peakDb = maxAbs > 0 ? 20.0 * Math.Log10(maxAbs) : double.NegativeInfinity;
            double rmsDb = rms > 0 ? 20.0 * Math.Log10(rms) : double.NegativeInfinity;

            return new AnalysisResult(maxAbs, peakDb, rms, rmsDb, totalSamples);
        }
    }
}