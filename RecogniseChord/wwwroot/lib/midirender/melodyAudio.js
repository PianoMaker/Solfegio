// melodyAudio.js

console.log("melodyAudio.js is working");

document.addEventListener("DOMContentLoaded", function () {

    const DEFAULT_BPM = 100;

    let audioContext = null;
    let activeSources = [];
    let isPlaying = false;

    // ------------------------------------------------------------
    // Ініціалізація AudioContext
    // ------------------------------------------------------------
    function getAudioContext() {
        if (!audioContext) {
            audioContext = new (
                window.AudioContext ||
                window.webkitAudioContext
            )();
        }

        return audioContext;
    }

    // ------------------------------------------------------------
    // Перетворення назви ноти в MIDI note number
    //
    // c4  = 60
    // cis4 / c#4 = 61
    // des4 / db4 = 61
    // ------------------------------------------------------------
    function noteToMidi(noteName) {

        if (!noteName) return null;

        let text = noteName.trim().toLowerCase();

        // Українське h у Вашому pattern означає B natural
        // (це відповідає німецькій системі позначень).
        const match = text.match(/^([a-gh])([#bisu]*)(['']*)(-?\d+)?$/);

        if (!match) {
            return null;
        }

        const letter = match[1];
        const accidentalText = match[2] || "";
        const octaveMarks = match[3] || "";
        let octave = match[4] !== undefined
            ? parseInt(match[4], 10)
            : 4;

        const base = {
            c: 0,
            d: 2,
            e: 4,
            f: 5,
            g: 7,
            a: 9,
            h: 11,
            b: 10
        };

        if (base[letter] === undefined) {
            return null;
        }

        let semitone = base[letter];

        // Альтерації
        for (const ch of accidentalText) {

            if (ch === '#') {
                semitone += 1;
            }

            // b = бемоль
            else if (ch === 'b') {
                semitone -= 1;
            }

            // is = дієз
            else if (ch === 'i' || ch === 's') {
                // Обробляємо is як одну альтерацію нижче
            }
        }

        // Явно обробляємо стандартні позначення
        if (accidentalText.includes("is")) {
            semitone += 1;
        }

        if (accidentalText.includes("es")) {
            semitone -= 1;
        }

        // Апострофи піднімають октаву.
        // У Вашому pattern:
        // c' = C наступної октави
        // c'' = ще на октаву вище
        octave += octaveMarks.length;

        // Нормалізація, якщо semitone вийшов за межі октави
        while (semitone < 0) {
            semitone += 12;
            octave--;
        }

        while (semitone >= 12) {
            semitone -= 12;
            octave++;
        }

        return (octave + 1) * 12 + semitone;
    }

    // ------------------------------------------------------------
    // MIDI → Hz
    // ------------------------------------------------------------
    function midiToFrequency(midi) {
        return 440 * Math.pow(2, (midi - 69) / 12);
    }

    // ------------------------------------------------------------
    // Розбір одного токена
    //
    // Приклади:
    // a4
    // g8
    // a4.
    // r2
    // r4.
    // c'8
    // cis4
    // ------------------------------------------------------------
    function parseToken(token) {

        token = token.trim();

        if (!token) {
            return null;
        }

        // Розмір/тривалість завжди наприкінці
        const durationMatch = token.match(/(1|2|4|8|16|32|64|128)(\.)?$/);

        if (!durationMatch) {
            console.warn("Cannot determine duration:", token);
            return null;
        }

        const durationCode = durationMatch[1];
        const dotted = !!durationMatch[2];

        const pitchPart =
            token.substring(
                0,
                token.length - durationMatch[0].length
            );

        const isRest =
            pitchPart.toLowerCase() === "r";

        let midi = null;

        if (!isRest) {
            midi = noteToMidi(pitchPart);

            if (midi === null) {
                console.warn("Cannot parse note:", token);
                return null;
            }
        }

        return {
            token,
            pitch: pitchPart,
            midi,
            frequency: midi !== null
                ? midiToFrequency(midi)
                : 0,
            durationCode,
            dotted,
            isRest
        };
    }

    // ------------------------------------------------------------
    // Тривалість ноти в мілісекундах
    //
    // 100 BPM:
    // quarter = 600 ms
    // half    = 1200 ms
    // eighth  = 300 ms
    // ------------------------------------------------------------
    function durationToMs(durationCode, dotted, bpm) {

        const quarterMs = 60000 / bpm;

        const factors = {
            "1": 4,
            "2": 2,
            "4": 1,
            "8": 0.5,
            "16": 0.25,
            "32": 0.125,
            "64": 0.0625,
            "128": 0.03125
        };

        let duration =
            quarterMs * (factors[durationCode] || 1);

        if (dotted) {
            duration *= 1.5;
        }

        return duration;
    }

    // ------------------------------------------------------------
    // Парсинг pattern
    // ------------------------------------------------------------
    function parsePattern(pattern, bpm = DEFAULT_BPM) {

        if (!pattern) {
            return [];
        }

        const tokens =
            pattern
                .trim()
                .split(/[\s_]+/)
                .filter(Boolean);

        return tokens
            .map(token => parseToken(token))
            .filter(Boolean)
            .map(note => {

                note.durationMs =
                    durationToMs(
                        note.durationCode,
                        note.dotted,
                        bpm
                    );

                return note;
            });
    }

    // ------------------------------------------------------------
    // Створення осцилятора
    // ------------------------------------------------------------
    function createOscillator(
        ctx,
        frequency,
        startTime,
        duration,
        waveform = "sine"
    ) {

        const oscillator =
            ctx.createOscillator();

        const gain =
            ctx.createGain();

        oscillator.type = waveform;
        oscillator.frequency.setValueAtTime(
            frequency,
            startTime
        );

        // Envelope
        const attack = Math.min(
            0.015,
            duration / 1000 * 0.1
        );

        const release = Math.min(
            0.05,
            duration / 1000 * 0.2
        );

        const endTime =
            startTime + duration / 1000;

        const releaseStart =
            Math.max(
                startTime + attack,
                endTime - release
            );

        gain.gain.setValueAtTime(
            0,
            startTime
        );

        gain.gain.linearRampToValueAtTime(
            0.25,
            startTime + attack
        );

        gain.gain.setValueAtTime(
            0.25,
            releaseStart
        );

        gain.gain.linearRampToValueAtTime(
            0,
            endTime
        );

        oscillator.connect(gain);
        gain.connect(ctx.destination);

        oscillator.start(startTime);
        oscillator.stop(endTime + 0.01);

        activeSources.push(oscillator);
    }

    // ------------------------------------------------------------
    // Зупинка поточного відтворення
    // ------------------------------------------------------------
    function stopMelody() {

        activeSources.forEach(source => {
            try {
                source.stop();
            } catch {
                // oscillator вже міг завершитися
            }
        });

        activeSources = [];
        isPlaying = false;
    }

    // ------------------------------------------------------------
    // Відтворення pattern
    // ------------------------------------------------------------
    async function playPattern(pattern, bpm = DEFAULT_BPM) {

        stopMelody();

        if (!pattern) {
            return;
        }

        const ctx = getAudioContext();

        if (ctx.state === "suspended") {
            await ctx.resume();
        }

        const notes =
            parsePattern(pattern, bpm);

        if (notes.length === 0) {
            console.warn("Pattern contains no playable notes:", pattern);
            return;
        }

        isPlaying = true;

        let currentTime =
            ctx.currentTime + 0.05;

        for (const note of notes) {

            const durationSeconds =
                note.durationMs / 1000;

            if (!note.isRest) {

                createOscillator(
                    ctx,
                    note.frequency,
                    currentTime,
                    note.durationMs,
                    "sine"
                );
            }

            currentTime += durationSeconds;
        }

        const totalDuration =
            (currentTime - ctx.currentTime) * 1000;

        setTimeout(() => {
            if (isPlaying) {
                isPlaying = false;
                activeSources = [];
            }
        }, totalDuration + 100);
    }

    // ------------------------------------------------------------
    // Клік по melody-card
    // ------------------------------------------------------------
    document
        .querySelectorAll(".melody-card")
        .forEach(card => {

            card.addEventListener("click", function () {

                const notation =
                    card.querySelector(".melody-notation");

                if (!notation) {
                    console.warn(
                        "melody-card has no .melody-notation"
                    );
                    return;
                }

                const pattern =
                    notation.dataset.pattern;

                if (!pattern) {
                    console.warn(
                        "melody-card has empty data-pattern"
                    );
                    return;
                }

                console.log(
                    "Playing melody:",
                    pattern
                );

                playPattern(
                    pattern,
                    DEFAULT_BPM
                );
            });
        });

});
