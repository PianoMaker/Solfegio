//vexflow_ext.js

// ----------------------
// Функція для створення нотного стану (stave) з ключем, розміром такту та знаками при ключі
// повертає об'єкт Vex.Flow.Stave
// ----------------------
function setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow = false, timeSignatureChanged = false, keySignatureChanged = false, keySigName = null) {
    console.log("FOO: midiRenderer.js - setStave");
    const stave = new Vex.Flow.Stave(Xposition, Yposition, STAVE_WIDTH);

    // Додаємо ключ для першого такту взагалі або для першого такту в кожному рядку
    if (index === 0 || isFirstMeasureInRow) {
        stave.addClef("treble");
        console.log(`Adding clef to measure ${index + 1}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
    }

    // Додаємо знаки при ключі (key signature) якщо доступні і це перший такт, або перший у рядку, або вона змінилась
    if (keySigName && (index === 0 || keySignatureChanged || isFirstMeasureInRow)) {
        try {            
            stave.addKeySignature(keySigName);
            console.debug(`Adding key signature ${keySigName} to measure ${index + 1}`);
        } catch (e) {
            console.warn(`Failed to add key signature ${keySigName} to measure ${index + 1}`, e);
        }
    }

    // Додаємо розмір такту для першого такту або коли розмір змінюється
    if (index === 0 || timeSignatureChanged) {
        stave.addTimeSignature(`${currentNumerator}/${currentDenominator}`);
        console.log(`Adding time signature ${currentNumerator}/${currentDenominator} to measure ${index + 1}, timeSignatureChanged: ${timeSignatureChanged}`);
    }

    return stave;
}

// ----------------------
// Мапінг sf/mi у рядок для VexFlow addKeySignature
// sf: кількість бемолів (від'ємні) або дієзів (додатні), mi: 0=major,1=minor
// повертає рядок, наприклад "C", "G", "D", "A", "E", "B", "F#", "C#" для мажорних тональностей і "Am", "Em", "Bm", "F#m", "C#m", "G#m", "D#m", "A#m" для мінорних тональностей
// ----------------------
function mapKeySignatureName(sf, mi) {
    // sf: -7..+7 (кількість бемолів (від'ємні) або дієзів (додатні)), mi: 0=major,1=minor
    const majors = ['Cb', 'Gb', 'Db', 'Ab', 'Eb', 'Bb', 'F', 'C', 'G', 'D', 'A', 'E', 'B', 'F#', 'C#'];
    const minors = ['Abm', 'Ebm', 'Bbm', 'Fm', 'Cm', 'Gm', 'Dm', 'Am', 'Em', 'Bm', 'F#m', 'C#m', 'G#m', 'D#m', 'A#m'];
    const idx = sf + 7;
    if (idx < 0 || idx >= majors.length) return null;
    return mi === 0 ? majors[idx] : minors[idx];
}
// ----------------------
// Побудова карти знаків альтерації для заданої тональності 
// параметр sf: кількість бемолів (від'ємні) або дієзів (додатні) у тональності
// повертає об'єкт (словник), де ключі - ноти (a-g), а значення - '#' або 'b' для знаків альтерації
// ----------------------
function buildKeySignatureMap(sf) {
    console.log("FOO: midiRenderer.js - buildKeySignatureMap");
    const sharpOrder = ['f', 'c', 'g', 'd', 'a', 'e', 'b'];
    const flatOrder = ['b', 'e', 'a', 'd', 'g', 'c', 'f'];
    const map = {};
    if (sf > 0) {
        for (let i = 0; i < sf && i < sharpOrder.length; i++) {
            map[sharpOrder[i]] = '#';
        }
    } else if (sf < 0) {
        const count = Math.min(-sf, flatOrder.length);
        for (let i = 0; i < count; i++) {
            map[flatOrder[i]] = 'b';
        }
    }
    return map;
}




// ----------------------
// ФУНКЦІЯ ДЛЯ СТВОРЕННЯ НОТИ З ВИКОРИСТАННЯМ VEXFLOW
// Параметри:
// - durationCode: Код тривалості ноти (наприклад, 'q', 'h', '8', '16r' для паузи).
// - key: Висота ноти у форматі VexFlow (наприклад, 'C/4', 'D#/5').
// - accidental: Знак альтерації ('#', 'b', 'n') або null.
// Повертає: Об'єкт Vex.Flow.StaveNote або null у разі помилки.
// ----------------------
function processNoteElement(durationCode, key, accidental) {

    const isArray = Array.isArray(key);

    if (!isArray) {
        key = key.replace('/', '');

        const note = createNote(key, durationCode);

        if (note && accidental) {
            note.addAccidental(
                0,
                new Vex.Flow.Accidental(accidental)
            );
        }

        applyAutoStem(note, durationCode);

        return note;
    }

    const keys = key.map(k => k);

    const accidentals = Array.isArray(accidental)
        ? accidental
        : keys.map(() => null);

    const isTriplet =
        typeof durationCode === "string" &&
        durationCode.endsWith("t");

    const isDotted =
        typeof durationCode === "string" &&
        durationCode.endsWith(".");

    const baseDuration =
        (isTriplet || isDotted)
            ? durationCode.slice(0, -1)
            : durationCode;

    const vexDuration =
        normalizeVexFlowDuration(baseDuration);

    const note = new Vex.Flow.StaveNote({
        keys: keys,
        duration: vexDuration
    });

    accidentals.forEach((acc, keyIndex) => {
        if (acc) {
            note.addModifier(
                new Vex.Flow.Accidental(acc),
                keyIndex
            );
        }
    });

    if (isDotted && !isTriplet) {
        keys.forEach((_, keyIndex) => note.addDot(keyIndex));
    }

    if (isTriplet) {
        note.__isTriplet = true;
        note.__tripletBase = baseDuration;
        note.__durationCode = durationCode;
    }

    applyAutoStem(note, durationCode);

    if (keys.length > 1) {
        note.__isChord = true;
        note.__chordKeys = keys.slice();
    }

    note.__durationCode = durationCode;

    return note;
}


// ----------------------
// ФУНКЦІЯ ДЛЯ НОРМАЛІЗАЦІЇ КОДУ ТРИВАЛОСТІ НОТИ ДЛЯ VEXFLOW
// Видаляє закінчення 't' (тріоль) або '.' (дотована) для отримання базового коду тривалості.
// Параметри:
// - durationCode: Рядок коду тривалості ноти (наприклад, '8t', '16.', 'q').
// Повертає: Нормалізований рядок коду тривалості (наприклад, '8', '16', 'q').
// ----------------------
function normalizeVexFlowDuration(durationCode) {
    if (typeof durationCode !== "string") {
        return durationCode;
    }

    if (durationCode.endsWith("t")) {
        return durationCode.slice(0, -1);
    }

    if (durationCode.endsWith(".")) {
        return durationCode.slice(0, -1);
    }

    return durationCode;
}

// ----------------------
// ФУНКЦІЯ ДЛЯ РОЗРАХУНКУ ПОТРІБНОЇ ВИСОТИ НОТНОГО РЯДКУ В КАНВАСІ
// Параметри:
// - measuresCount: Кількість тактів.
// - GENERALWIDTH: Загальна ширина канвасу.
// - BARWIDTH: Ширина одного такту.
// - HEIGHT: Висота одного рядка нотного стану.
// - TOPPADDING: Верхній відступ (за замовчуванням 20).
// - CLEFZONE: Додаткова ширина для першого такту в рядку (за замовчуванням 60).
// - Xmargin: Лівий відступ (за замовчуванням 10).
// Повертає: Загальну висоту канвасу, необхідну для розміщення всіх тактів.
// ----------------------
function calculateRequiredHeight(measuresCount, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING = 20, CLEFZONE = 60, Xmargin = 10) {
    
    let Xposition = Xmargin;
    let rows = 1;
    for (let i = 0; i < measuresCount; i++) {
        let STAVE_WIDTH = BARWIDTH;
        if (i === 0) STAVE_WIDTH += CLEFZONE;
        if (Xposition > GENERALWIDTH - BARWIDTH) {
            rows++;
            Xposition = Xmargin;
        }
        Xposition += STAVE_WIDTH;
    }
    return TOPPADDING + HEIGHT * rows;
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ПОЗИЦІЇ X,Y ДЛЯ ПОЧАТКУ НОВОГО ТАКТУ
// ----------------------
function getXYposition(Xmargin, TOPPADDING, GENERALWIDTH, BARWIDTH, HEIGHT, index, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - getXYposition");
    let Xposition = Xmargin;
    let Yposition = TOPPADDING;
    if (Xposition > GENERALWIDTH - BARWIDTH) {
        Yposition += HEIGHT; Xposition = Xmargin;
        console.log("Yposition updated:", Yposition);
    }
    else {
        console.log(`General ${GENERALWIDTH} - ${BARWIDTH} vs ${Xposition}`);
    }
    logPositions(`Processing measure ${index + 1}: X=${Xposition}  Y=${Yposition}`);
    return { Xposition, Yposition };
}
// ----------------------
// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ШИРИНИ ТАКТУ
// ----------------------
function getStaveWidth(BARWIDTH, index, CLEFZONE) {
    console.log("FOO: midiRenderer.js - getStaveWidth");
    let STAVE_WIDTH = BARWIDTH;
    if (index === 0) {
        STAVE_WIDTH += CLEFZONE;
        console.log("First stave width:", STAVE_WIDTH);
    }
    return STAVE_WIDTH;
}
// ----------------------
// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ШИРИНИ ТАКТУ З УРАХУВАННЯМ ЗМІНИ РОЗМІРУ ТАКТУ
// ----------------------
function adjustStaveWidth(BARWIDTH, index, CLEFZONE, isFirstMeasureInRow = false, timeSignatureChanged = false) {
    console.log("FOO: midiRenderer.js - adjustStaveWidth");
    let STAVE_WIDTH = BARWIDTH;

    // Додаємо CLEFZONE для першого такту взагалі або для першого такту в кожному рядку
    if (index === 0 || isFirstMeasureInRow) {
        STAVE_WIDTH += CLEFZONE;
        console.log(`Adding CLEFZONE to measure ${index + 1}, total width: ${STAVE_WIDTH}, isFirstMeasureInRow: ${isFirstMeasureInRow}`);
    }

    // Додаємо додатковий простір для розміру такту, якщо він змінився (але не для першого такту, бо там вже врахований CLEFZONE)
    if (timeSignatureChanged && index > 0 && !isFirstMeasureInRow) {
        STAVE_WIDTH += 30; // Додатковий простір для розміру такту
        console.log(`Adding extra space for time signature change to measure ${index + 1}, total width: ${STAVE_WIDTH}`);
    }
	logPositions(`Measure ${index + 1}: STAVE_WIDTH=${STAVE_WIDTH}`);
    return STAVE_WIDTH;
}
// ----------------------
// ФУНКЦІЯ ДЛЯ ОНОВЛЕННЯ ПОЗИЦІЇ X,Y ПРИ РОЗМІЩЕННІ НОВОГО ТАКТУ
// ----------------------
function adjustXYposition(Xposition, GENERALWIDTH, BARWIDTH, Yposition, HEIGHT, Xmargin, index, barStartAbsTime) {
    console.log("FOO: midiRenderer.js - adjustXYposition");
    if (Xposition > GENERALWIDTH - BARWIDTH) {
        Yposition += HEIGHT; Xposition = Xmargin;
        console.log("Yposition updated:", Yposition);
    } else {
        console.log(`General width ${GENERALWIDTH} vs ${BARWIDTH} + ${Xposition}`);
    }
    logPositions(`Processing measure ${index + 1} starting from tick: ${barStartAbsTime} X=${Xposition}  Y=${Yposition}`);
    return { Xposition, Yposition };
}

//  ----------------------
// ФУНКЦІЯ ПЕРЕТВОРЕННЯ MIDI НОТИ У ФОРМАТ VEXFLOW
// Параметри:
// - midiNote: Ціле число MIDI ноти (0-127).
// - currentKeySig: Поточний об'єкт тональності {sf: -7..+7, mi: 0|1} або null.
// Повертає: Об'єкт {key: 'C/4', accidental: '#', 'b', 'n' або null}.
// ----------------------

function midiNoteToVexFlowWithKey(midiNote, currentKeySig, clef = 'treble') {
    console.debug("MR: FOO: midiRenderer.js - midiNoteToVexFlowWithKey, midiNote =", midiNote, "currentKeySig=", currentKeySig);

    const m = Number.isFinite(Number(midiNote)) ? Number(midiNote) : 0;
    const sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    const flatNames = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
    const pc = ((m % 12) + 12) % 12;
    let outOctave = Math.floor(m / 12) - 1;

    // Safe key-signature extraction
    const sf = currentKeySig && typeof currentKeySig.sf === 'number' ? currentKeySig.sf : 0;
    const mi = currentKeySig && typeof currentKeySig.mi === 'number' ? currentKeySig.mi : 0; // 0=major,1=minor

    // Minor tonic pitch-class (optional)
    let tonicPc = null;
    try { tonicPc = (typeof getMinorTonicPc === 'function') ? getMinorTonicPc(currentKeySig) : null; } catch (e) { tonicPc = null; }

    // Defaults
    let chosenSharp = sharpNames[pc];
    let chosenFlat = flatNames[pc];
    let chosen = (sf < 0) ? chosenFlat : chosenSharp;

	// Minor-key enharmonic preferences (preserve previous heuristics) // IV#, VI#, VII#, bII
    if (mi === 1 && tonicPc != null) {
        const raised4 = (tonicPc + 6) % 12;  // #IV
        const raised6 = (tonicPc + 9) % 12;  // #VI
        const raised7 = (tonicPc + 11) % 12; // #VII
        const lowered2 = (tonicPc + 1) % 12; // bII

        if (pc === raised7) chosen = chosenSharp;
        if (pc === raised4 || pc === raised6) chosen = chosenSharp;
        if (pc === lowered2) chosen = chosenFlat;

        // Extreme edge cases preserved
        if (sf <= -6 && pc === 11) { chosen = 'Cb'; outOctave = outOctave + 1; }
        else if (sf <= -6 && pc === 4) { chosen = 'Fb'; }
        else if (sf >= 6 && pc === 5) { chosen = 'E#'; }
        else if (sf >= 6 && pc === 0) { chosen = 'B#'; outOctave = outOctave - 1; }
        else if (sf === 3 && mi === 1 && pc === 5) { chosen = 'E#'; }
    }

    // Additional general-edge adjustments (matching legacy behaviour)
    if (sf <= -6 && pc === 11) { chosen = 'Cb'; outOctave = outOctave + 1; }
    else if (sf <= -6 && pc === 4) { chosen = 'Fb'; }
    else if (sf >= 6 && pc === 5) { chosen = 'E#'; }
    else if (sf >= 6 && pc === 0) { chosen = 'B#'; outOctave = outOctave - 1; }

    // Robust accidental detection:
    let spelledAcc = null;
    if (typeof chosen === 'string') {
        if (chosen.indexOf('#') !== -1) spelledAcc = '#';
        else if (chosen.indexOf('b') !== -1) spelledAcc = 'b';
    }

    // Fallback: if chosen has no explicit accidental, prefer flat/sharp based on key-signature preference
    if (!spelledAcc) {
        // If key signature strongly suggests flats, and flatNames has 'b' for this pc, prefer 'b'
        if (sf < 0 && flatNames[pc].indexOf('b') !== -1) spelledAcc = 'b';
        // Else if key signature non-negative and sharpNames has '#', prefer '#'
        else if (sf >= 0 && sharpNames[pc].indexOf('#') !== -1) spelledAcc = '#';
        // else leave null (natural)
    }

    // Ensure we return letter (without accidental) + octave as key string for createNote
    const letterMatch = (typeof chosen === 'string') ? chosen.match(/^([A-G])/i) : null;
    const letterPart = letterMatch ? letterMatch[1].toUpperCase() : 'C';
    const key = `${letterPart}/${outOctave}`;

    console.debug(`midiNoteToVexFlowWithKey -> chosen=${chosen}, key=${key}, spelledAcc=${spelledAcc}, clef=${clef}`);
    return { key, accidental: spelledAcc };
}

function logvexflowNoteDetails(note, index) {
    if (!note) {
        logVexFlow(`[${index}] NULL`);
        return;
    }

    const keys = note.getKeys ? note.getKeys().join("+") : "?";
    const duration = note.__durationCode || (note.getDuration ? note.getDuration() : "?");

    const modifiers = note.getModifiers ? note.getModifiers() : [];
    const accidentals = modifiers
        .filter(m => m instanceof Vex.Flow.Accidental)
        .map(m => m.type)
        .join(",") || "none";

    const dotted = typeof duration === "string" && duration.endsWith(".");
    const triplet = typeof duration === "string" && duration.endsWith("t");


    logVexFlow(
        `[${index}] ${keys} | ` +
        `acc=${accidentals} | ` +
        `dur=${duration} | ` +
        `dotted=${dotted ? "yes" : "no"} | ` +
        `triplet=${triplet ? "yes" : "no"} | ` 
    );
}


function logVexFlow(msg) {
    const vexRead = document.getElementById("vexRead");
    if (!vexRead) {
        return;
	}
    vexRead.innerHTML += `<div>${msg}</div>`;    
}

function logPositions(msg) {
    const vexPositions = document.getElementById("vexPositions");
	if (!vexPositions) {
        return;
    }
    vexPositions.innerHTML += `<div>${msg}</div>`;
	console.debug(msg);
}