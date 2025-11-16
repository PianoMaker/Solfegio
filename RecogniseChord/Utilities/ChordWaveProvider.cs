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
        public ChordWaveProvider(List<double> frequencies, int durationMs, int sampleRate = 44100)
        : base(sampleRate, 1)
        {
            _sampleRate = sampleRate;
            _numTones = frequencies?.Count ?? 0;
            _phase = new double[_numTones];
            _phaseInc = new double[_numTones];
            _envs = new EnvelopeGenerator[_numTones];

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
                        mixed += Math.Sin(_phase[t]) * amp;
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
                    mixed += Math.Sin(_phase[t]) * amp;
                    _phase[t] += _phaseInc[t];
                    if (_phase[t] > 2.0 * Math.PI) _phase[t] -= 2.0 * Math.PI;
                }
                buffer[offset + i] = (float)(mixed * scale * 0.9);
            }

            _samplesRemaining -= samplesToWrite;
            if (_samplesRemaining < 0) _samplesRemaining = 0; // normalize to zero to enter tail on next call
            return samplesToWrite;
        }
    }
}
