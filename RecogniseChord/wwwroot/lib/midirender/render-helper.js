function processNoteElement(durationCode, key, accidental, clef = 'treble') {
    let normalized = (typeof key === 'string') ? key.replace('/', '') : key;
    normalized = (typeof normalized === 'string') ? normalized.toLowerCase() : normalized;

    const note = createNote(normalized, durationCode, clef);

    if (note && accidental) {
        note.addAccidental(0, new Vex.Flow.Accidental(accidental));
    }

    applyAutoStem(note, durationCode);
    return note;
}