using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Dsp;

namespace Music
{
    /// <summary>
    /// Provides mixed audio for a chord: all non-zero frequencies start at t=0 and sustain for the given duration.
    /// Each tone uses a simple ADSR envelope. Zero frequencies are treated as silence.
    /// </summary>
    public class ChordWaveProvider : WaveProvider32
    {
        private readonly double[] _phase;
        private readonly double[] _phaseInc;
        private readonly EnvelopeGenerator[] _envs;
        private readonly int _numTones;
        private int _samplesRemaining;
        private readonly int _sampleRate;
        // NEW: track tail emission to avoid infinite loop
        private int _tailSamplesRemaining = -1;

        // ADSR parameters (seconds / level)
        private float _attack = 0.01f;
        private float _decay = 0.12f;
        private float _sustain = 0.7f;
        private float _release = 0.18f;
        private TIMBRE _timbre;

        public float AttackSeconds
        {
            get => _attack;
            set
            {
                _attack = Math.Max(0, value);
                foreach (var e in _envs) e.AttackRate = _attack * _sampleRate;
            }
        }
        public float DecaySeconds
        {
            get => _decay;
            set
            {
                _decay = Math.Max(0, value);
                foreach (var e in _envs) e.DecayRate = _decay * _sampleRate;
            }
        }
        public float SustainLevel
        {
            get => _sustain;
            set
            {
                _sustain = Math.Clamp(value, 0f, 1f);
                foreach (var e in _envs) e.SustainLevel = _sustain;
            }
        }
        public float ReleaseSeconds
        {
            get => _release;
            set
            {
                _release = Math.Max(0, value);
                foreach (var e in _envs) e.ReleaseRate = _release * _sampleRate;
            }
        }

        /// <summary>
        /// frequencies: list of Hz values; use0 for rests. durationMs: total active (gate on) duration excluding release tail.
        /// </summary>
        public ChordWaveProvider(List<double> frequencies, int durationMs, TIMBRE timbre = TIMBRE.sin, int sampleRate = 44100)
        : base(sampleRate, 1)
        {
            _sampleRate = sampleRate;
            _numTones = frequencies?.Count ?? 0;
            _phase = new double[_numTones];
            _phaseInc = new double[_numTones];
            _envs = new EnvelopeGenerator[_numTones];
            _timbre = timbre;

            for (int i = 0; i < _numTones; i++)
            {
                double f = frequencies[i];
                // clamp frequency to audible range if >0
                if (f < 0) f = 0; else if (f > 0 && f < 20) f = 20; else if (f > 20000) f = 20000;
                _phase[i] = 0.0;
                _phaseInc[i] = f > 0 ? 2.0 * Math.PI * f / _sampleRate : 0.0;

                var env = new EnvelopeGenerator
                {
                    AttackRate = _attack * _sampleRate,
                    DecayRate = _decay * _sampleRate,
                    SustainLevel = _sustain,
                    ReleaseRate = _release * _sampleRate
                };
                if (f > 0) env.Gate(true); // start gate immediately
                _envs[i] = env;
            }

            _samplesRemaining = Math.Max(0, durationMs * sampleRate / 1000);

            float loudness = CalculateLoudness();

            Console.WriteLine($"RMS = {loudness} db");

        }


        public override int Read(float[] buffer, int offset, int count)
        {
            // If fully finished (including tail), signal end of stream
            if (_samplesRemaining < 0)
                return 0;

            // Active part finished: emit release tail exactly once
            if (_samplesRemaining == 0)
            {
                if (_tailSamplesRemaining == -1)
                {
                    foreach (var e in _envs) e.Gate(false);
                    _tailSamplesRemaining = (int)(_release * _sampleRate);
                }

                if (_tailSamplesRemaining <= 0)
                {
                    _samplesRemaining = -1; // mark complete
                    return 0;
                }

                int toWrite = Math.Min(count, _tailSamplesRemaining);
                for (int i = 0; i < toWrite; i++)
                {
                    double mixed = 0.0;
                    for (int t = 0; t < _numTones; t++)
                    {
                        if (_phaseInc[t] == 0.0) continue;
                        float amp = _envs[t].Process();
                        mixed += WaveSample(_phase[t], _timbre) * amp;
                        _phase[t] += _phaseInc[t];
                        if (_phase[t] > 2.0 * Math.PI) _phase[t] -= 2.0 * Math.PI;
                    }
                    buffer[offset + i] = (float)(mixed * (1.0 / Math.Max(1, _numTones)) * 0.9);
                }

                _tailSamplesRemaining -= toWrite;
                if (_tailSamplesRemaining <= 0)
                {
                    _samplesRemaining = -1; // done completely
                }
                return toWrite;
            }

            // Normal sustain rendering path
            int samplesToWrite = Math.Min(_samplesRemaining, count);
            double scale = 1.0 / Math.Max(1, _numTones);

            for (int i = 0; i < samplesToWrite; i++)
            {
                double mixed = 0.0;
                for (int t = 0; t < _numTones; t++)
                {
                    if (_phaseInc[t] == 0.0) continue; // silent tone
                    float amp = _envs[t].Process();
                    mixed += WaveSample(_phase[t], _timbre) * amp;
                    _phase[t] += _phaseInc[t];
                    if (_phase[t] > 2.0 * Math.PI) _phase[t] -= 2.0 * Math.PI;
                }
                buffer[offset + i] = (float)(mixed * scale * 0.9);
            }

            _samplesRemaining -= samplesToWrite;
            if (_samplesRemaining < 0) _samplesRemaining = 0; // normalize to zero to enter tail on next call
            return samplesToWrite;
        }

        private static double WaveSample(double phase, TIMBRE timbre)
        {
            double baseSample = timbre switch
            {
                TIMBRE.tri => 2.0 * Math.Asin(Math.Sin(phase)) / Math.PI,
                TIMBRE.saw => 2.0 * (phase / (2.0 * Math.PI)) - 1.0,
                TIMBRE.square => Math.Sin(phase) >= 0 ? 1.0 : -1.0,
                _ => Math.Sin(phase),
            };
            double inputGain = GetTimbreInputGain(timbre);

            return baseSample * inputGain;
        }

        private static double GetTimbreInputGain(TIMBRE timbre)
        {
            return timbre switch
            {
                TIMBRE.square => 0.20,
                TIMBRE.saw => 0.25,
                TIMBRE.tri => 0.90,
                _ => 1.0, // sin
            };
        }


        internal static string SaveWave(TIMBRE timbre, List<double> freqs, int activeMs, string fullPath)
        {
            var provider = new ChordWaveProvider(freqs, activeMs, timbre);

            using (var writer = new WaveFileWriter(fullPath, provider.WaveFormat))
            {
                int bufferSamples = provider.WaveFormat.SampleRate / 10; //0.1s buffer
                float[] buffer = new float[bufferSamples];
                int samplesRead;
                while ((samplesRead = provider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.WriteSamples(buffer, 0, samplesRead);
                }
            }

            Console.WriteLine($"WAV saved to {fullPath}");
            return fullPath;
        }

        private float CalculateLoudness()
        {
            // Estimate RMS (dBFS) of the generated chord given current settings.
            // Calculation reflects current mixing implementation:
            //  - per-waveform sample values are in [-1..1] (WaveSample)
            //  - envelope multiplies per-sample amplitude (0..1)
            //  - mixing sums tone samples, then multiplies by scale = 1.0 / N and fixed headroom 0.9
            //
            // Strategy:
            //  - compute waveform RMS for selected TIMBRE
            //  - numerically integrate envelope^2 over active + release seconds to get envelopeRms
            //  - singleToneRms = waveformRms * envelopeRms
            //  - combinedRms = singleToneRms * sqrt(N) * scale  (assuming uncorrelated phases)
            //  - convert to dBFS: 20*log10(rms). If rms == 0 -> return very low dB.
            if (_numTones <= 0) return -200f;

            // waveform RMS for full-scale (-1..1) wave
            double waveformRms = _timbre switch
            {
                TIMBRE.square => 1.0,
                TIMBRE.tri => 1.0 / Math.Sqrt(3.0),
                TIMBRE.saw => 1.0 / Math.Sqrt(3.0),
                _ => 1.0 / Math.Sqrt(2.0) // sin
            };

            // active seconds derived from samplesRemaining at constructor time
            double activeSeconds = (double)_samplesRemaining / _sampleRate;
            double attack = Math.Max(0.0, _attack);
            double decay = Math.Max(0.0, _decay);
            double sustain = Math.Clamp(_sustain, 0.0f, 1.0f);
            double release = Math.Max(0.0, _release);

            // total time to integrate: active + release (release tail contributes to energy)
            double totalSeconds = activeSeconds + release;
            int totalSamples = Math.Max(1, (int)Math.Round(totalSeconds * _sampleRate));

            // helper to compute envelope level at time t (seconds)
            double LevelAt(double t)
            {
                if (t < 0) return 0.0;

                // attack phase
                if (attack > 0 && t < attack)
                {
                    return t / attack;
                }
                else if (attack == 0 && t < 0.0) // unreachable, keep for safety
                {
                    return 1.0;
                }

                double tAfterAttack = t - attack;
                // decay phase
                if (tAfterAttack >= 0 && decay > 0 && tAfterAttack < decay)
                {
                    double frac = tAfterAttack / decay;
                    return 1.0 - frac * (1.0 - sustain);
                }

                // time within active (sustain) region
                double activeEnd = activeSeconds;
                if (t < activeEnd)
                {
                    // if activeEnds occurs during attack or decay, compute appropriate level
                    double tActive = t;
                    if (tActive <= attack)
                    {
                        return attack > 0 ? tActive / attack : 1.0;
                    }
                    else if (tActive <= attack + decay)
                    {
                        double td = tActive - attack;
                        return decay > 0 ? 1.0 - (td / decay) * (1.0 - sustain) : sustain;
                    }
                    else
                    {
                        return sustain;
                    }
                }

                // release phase (t >= activeEnd)
                if (release <= 0) return 0.0;

                // compute level at release start
                double levelAtRelease;
                if (activeSeconds <= attack)
                {
                    levelAtRelease = attack > 0 ? activeSeconds / attack : 1.0;
                }
                else if (activeSeconds <= attack + decay)
                {
                    double td = activeSeconds - attack;
                    levelAtRelease = decay > 0 ? 1.0 - (td / decay) * (1.0 - sustain) : sustain;
                }
                else
                {
                    levelAtRelease = sustain;
                }

                double tRel = t - activeSeconds;
                if (tRel >= release) return 0.0;
                // simple linear release to zero
                return levelAtRelease * (1.0 - (tRel / release));
            }

            // integrate envelope^2
            double sumSq = 0.0;
            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / _sampleRate;
                double lev = LevelAt(t);
                sumSq += lev * lev;
            }
            double envelopeRms = Math.Sqrt(sumSq / totalSamples);

            // single tone RMS after envelope and waveform shape
            double singleToneRms = waveformRms * envelopeRms;

            // mixing: code uses scale = 1.0 / N and sums N tones.
            // assuming uncorrelated phases, sum RMS scales by sqrt(N)
            double scale = 1.0 / Math.Max(1, _numTones);
            double combinedRms = singleToneRms * Math.Sqrt(_numTones) * scale;

            // account for fixed headroom multiplier 0.9 used in writes
            combinedRms *= 0.9;

            if (combinedRms <= 0) return -200f;

            double db = 20.0 * Math.Log10(combinedRms);
            return (float)db;
        }

    }
}
