using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using NAudio.Wave;

namespace Music
{
    /// <summary>
    /// Loads a single piano reference sample (A4) from several possible locations and provides
    /// resampling/mixing helpers. Designed to be registered as a singleton and receive
    /// IWebHostEnvironment via constructor injection.
    /// </summary>
    public class RemoteSamplePiano
    {
        // DI-initialized instance accessible for legacy static callers
        public static RemoteSamplePiano? Instance { get; private set; }
        public static void InitializeInstance(RemoteSamplePiano inst) => Instance = inst;

        private readonly IWebHostEnvironment _env;

        // Configurable path or URL (webroot-relative by default)
        public string BaseSamplePath { get; set; } = "wwwroot/sound/a4sample.wav";

        private static readonly string CacheFolder = Path.Combine(Path.GetTempPath(), "RecogniseChord_Samples");
        private static readonly string CachedFileName = "base_A4_sample.wav";
        private static readonly object _lock = new();

        private float[]? _baseSamples;
        private int _baseSampleRate;

        public RemoteSamplePiano(IWebHostEnvironment env)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            // do not auto-load here to avoid startup blocking in some hosts; caller may call EnsureBaseSampleLoaded
            // but it's safe to attempt load now
            try
            {
                EnsureBaseSampleLoaded();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoteSamplePiano: deferred load failed at ctor: {ex.Message}");
            }
        }

        public RemoteSamplePiano()
        {
        }

        /// <summary>
        /// Ensure the base A4 sample is loaded into memory. Prefer <webroot>/sound/a4sample.wav;
        /// if not found, try a few local candidates (including configured BaseSamplePath).
        /// Writes the list of checked paths to the console for diagnostics.
        /// </summary>
        public void EnsureBaseSampleLoaded()
        {
            if (_baseSamples != null) return;

            lock (_lock)
            {
                if (_baseSamples != null) return;

                // determine webroot (injected) or fallback to conventional wwwroot under current directory
                string webRoot = !string.IsNullOrEmpty(_env?.WebRootPath)
                    ? _env.WebRootPath
                    : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                Console.WriteLine($"webRoot: {webRoot}");

                // build candidate list (order matters: prefer webroot sound file first)
                var candidates = new List<string>
                {
                    Path.Combine(webRoot, "sound", "a4sample.wav"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sound", "a4sample.wav"),
                    Path.Combine(AppContext.BaseDirectory, "wwwroot", "sound", "a4sample.wav")
                };

                if (!string.IsNullOrWhiteSpace(BaseSamplePath))
                    candidates.Add(BaseSamplePath);

                // also try ResolveLocalSamplePath (it logs tried locations itself)
                try
                {
                    var resolved = ResolveLocalSamplePath(BaseSamplePath ?? string.Empty);
                    if (!string.IsNullOrEmpty(resolved))
                        candidates.Insert(0, resolved);
                }
                catch { /* ignore resolve errors, we'll still log candidates */ }

                // log checked locations
                Console.WriteLine("RemoteSamplePiano: searching sample in:");
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in candidates)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    var full = Path.GetFullPath(c);
                    if (seen.Add(full))
                        Console.WriteLine("  " + full);
                }

                // find first existing candidate
                string? found = null;
                foreach (var c in candidates)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    try
                    {
                        if (File.Exists(c))
                        {
                            found = c;
                            break;
                        }
                    }
                    catch { /* ignore invalid paths */ }
                }

                if (found == null)
                    throw new FileNotFoundException($"Sample not found. Checked candidate locations (see console output).");

                // copy to cache for consistent decoding
                Directory.CreateDirectory(CacheFolder);
                string cachedPath = Path.Combine(CacheFolder, CachedFileName);
                if (!string.Equals(Path.GetFullPath(found), Path.GetFullPath(cachedPath), StringComparison.OrdinalIgnoreCase))
                    File.Copy(found, cachedPath, overwrite: true);

                // Decode to mono float array
                using var afr = new AudioFileReader(cachedPath);
                _baseSampleRate = afr.WaveFormat.SampleRate;
                var list = new List<float>();
                var buffer = new float[afr.WaveFormat.Channels * 4096];
                int read;
                while ((read = afr.Read(buffer, 0, buffer.Length)) > 0)
                {
                    int frames = read / afr.WaveFormat.Channels;
                    for (int f = 0; f < frames; f++)
                    {
                        float sum = 0f;
                        for (int ch = 0; ch < afr.WaveFormat.Channels; ch++)
                            sum += buffer[f * afr.WaveFormat.Channels + ch];
                        list.Add(sum / afr.WaveFormat.Channels);
                    }
                }

                _baseSamples = list.ToArray();
                Console.WriteLine($"RemoteSamplePiano: loaded base sample ({_baseSampleRate} Hz, {_baseSamples.Length} frames).");
            }
        }

        private void DownloadToCache(string url, string cachedPath)
        {
            try
            {
                using var http = new HttpClient();
                var resp = http.GetAsync(url).GetAwaiter().GetResult();
                resp.EnsureSuccessStatusCode();
                var bytes = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                File.WriteAllBytes(cachedPath, bytes);
                Console.WriteLine($"RemoteSamplePiano: downloaded sample from {url} to cache.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download sample from {url}: {ex.Message}", ex);
            }
        }

        private string? ResolveLocalSamplePath(string configuredPath)
        {
            // Normalize configuredPath (remove leading ~/)
            var path = configuredPath.Trim();
            if (path.StartsWith("~/")) path = path.Substring(2);
            if (path.StartsWith("/")) path = path.Substring(1);

            var tried = new List<string>();

            // If absolute path provided
            if (Path.IsPathRooted(configuredPath))
            {
                tried.Add(configuredPath);
                if (File.Exists(configuredPath)) return configuredPath;
            }

            // Candidate roots:
            var contentRoot = Directory.GetCurrentDirectory(); // typically content root in ASP.NET Core
            var appBase = AppContext.BaseDirectory; // may differ in published app

            var possibleRoots = new List<string>();

            // prefer WebRootPath from injected env
            if (!string.IsNullOrEmpty(_env?.WebRootPath))
                possibleRoots.Add(_env.WebRootPath!);

            possibleRoots.Add(contentRoot);
            possibleRoots.Add(Path.Combine(contentRoot, "wwwroot"));
            possibleRoots.Add(appBase);
            possibleRoots.Add(Path.Combine(appBase, "wwwroot"));
            possibleRoots.Add(Path.Combine(appBase, "..", "wwwroot"));

            foreach (var root in possibleRoots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                try
                {
                    var candidate = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
                    tried.Add(candidate);
                    if (File.Exists(candidate)) return candidate;
                }
                catch { /* ignore invalid path combine */ }
            }

            // As last resort, try the raw relative path from current directory
            tried.Add(path);
            if (File.Exists(path)) return Path.GetFullPath(path);

            // Log tried locations for diagnostics
            Console.WriteLine("RemoteSamplePiano: failed to locate sample. Tried:");
            foreach (var t in tried) Console.WriteLine("  " + t);
            return null;
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

            double inputIndexScale = (_baseSampleRate / (double)outSampleRate) / ratio;
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
            try
            {
                if (_baseSamples == null) throw new InvalidOperationException("Base sample not loaded.");

                int outSamples = Math.Max(1, (int)Math.Round(sampleRate * ((activeMs + 50) / 1000.0)));
                var mix = new float[outSamples];

                foreach (var f in freqs)
                {
                    if (f <= 0) continue;
                    var buf = GetResampledNote(f, activeMs + 50, sampleRate);
                    int len = Math.Min(buf.Length, mix.Length);
                    for (int i = 0; i < len; i++) mix[i] += buf[i];
                }

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
                Console.WriteLine($"RemoteSamplePiano.TryRenderChordToWav failed: {ex.Message}");
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
