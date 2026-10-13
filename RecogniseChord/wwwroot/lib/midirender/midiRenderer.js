// midiRenderer.js

let info = "";
const ActiveNotesReadElement = document.getElementById("activeNotesRead");

/**
 * (AUTO-STEM HELPER)
 * -------------------------------------------------
 * Added helper to optionally apply VexFlow auto stem logic to every created note
 * without modifying or removing existing functions or comments. Rest durations
 * (codes ending with 'r') are skipped. Safe to call multiple times.
 */
function applyAutoStem(note, durationCode) {
	try {
		if (!note) return;
		if (typeof durationCode === 'string' && /r$/.test(durationCode)) return; // skip rests
		if (typeof note.autoStem === 'function') {
			note.autoStem();
		}
	} catch (e) {
		console.warn('applyAutoStem failed:', e);
	}
}

/**
 * drawScore
 * ----------
 * Renders a musical score from a MIDI file and displays it in the specified HTML element.
 *
 * @param {File} file - The MIDI file to render.
 * @param {string} ELEMENT_FOR_RENDERING - The ID of the HTML element where the score SVG will be rendered.
 * @param {string} ELEMENT_FOR_COMMENTS - The ID of the HTML element where status or error messages will be displayed.
 * @param {number} [GENERALWIDTH=1200] - The width of the rendered score in pixels.
 * @param {number} [HEIGHT=200] - The height of each stave row in pixels.
 * @param {number} [TOPPADDING=20] - The top padding for the score in pixels.
 * @param {number} [BARWIDTH=250] - The width of each measure/bar in pixels.
 * @param {number} [CLEFZONE=60] - The width reserved for the clef and time signature zone in pixels.
 * @param {number} [Xmargin=10] - The left margin for the score in pixels.
 *
 * Reads the provided MIDI file, parses its contents, and renders the musical notation using VexFlow.
 * Stores the resulting SVG and comments in sessionStorage for later retrieval.
 * Displays success or error messages in the comments element.
 */


function drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH = 1200, HEIGHT = 200, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10) {
	console.log(`FOO: midiRenderer.js - drawScore, NAME = ${file.name}`);
	logPositions(`drawScore called with file ${file.name}, Height=${HEIGHT}, generalwidth=${GENERALWIDTH}, barwidth=${BARWIDTH}`); 
	const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
	const notationDiv = document.getElementById(ELEMENT_FOR_RENDERING);

	if (!file) {
		console.error("drawScore: No file provided");
		if (commentsDiv) {
			commentsDiv.innerHTML = "Error: No MIDI file provided.";
		}
		return;
	}
	if (!ELEMENT_FOR_RENDERING) {
		console.error("drawScore: No ELEMENT_FOR_RENDERING provided");
		const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
		if (commentsDiv) {
			commentsDiv.innerHTML = "Error: No target element for rendering provided.";
		}
		return;
	}


	if (file) {
		console.log("drawScore: File selected:", file);
		const reader = new FileReader();
		notationDiv.innerHTML = "";
		commentsDiv.innerHTML = "";
		reader.onload = function (e) {
			console.log("drawScore: File read successfully");
			const uint8 = new Uint8Array(e.target.result);


			try {
				renderMidiFile(uint8, ELEMENT_FOR_RENDERING, GENERALWIDTH, HEIGHT, TOPPADDING, BARWIDTH, CLEFZONE, Xmargin, commentsDiv);
				const svg = notationDiv.querySelector("svg");
				if (svg) {
					sessionStorage.setItem("notationSVG", svg.outerHTML);
				}
				commentsDiv.innerHTML += `File rendered successfully`;
				sessionStorage.setItem("comment", commentsDiv.innerHTML)
			}
			catch (error) {
				console.error("drawScore: Error rendering MIDI file:", error);
				commentsDiv.innerHTML = `Error rendering MIDI file ${error}`;
				sessionStorage.setItem("comment", commentsDiv.innerHTML);
			}
		};
		reader.readAsArrayBuffer(file);

	}
}

/**
 * renderMidiFromUrl
 * -----------------
 * Convenience helper that loads a MIDI file by URL (fetch), wraps it into a File
 * object (so existing drawScore logic & FileReader path can be reused), then calls drawScore.
 * All layout parameters mirror drawScore and have the same defaults.
 *
 * @param {string} midiUrl - URL (relative or absolute) to .mid file
 * @param {string} [ELEMENT_FOR_RENDERING='notation'] - target element id for notation
 * @param {string} [ELEMENT_FOR_COMMENTS='comments'] - target element id for messages
 * @param {number} [GENERALWIDTH=1200]
 * @param {number} [HEIGHT=200]
 * @param {number} [TOPPADDING=20]
 * @param {number} [BARWIDTH=250]
 * @param {number} [CLEFZONE=60]
 * @param {number} [Xmargin=10]
 * @returns {Promise<void>}
 *
 * Usage example:
 *   await renderMidiFromUrl('/Uploads/example.mid');
 */

async function renderMidiFromUrl(
	midiUrl,
	ELEMENT_FOR_RENDERING = 'notation',
	ELEMENT_FOR_COMMENTS = 'comments',
	GENERALWIDTH = 1200,
	HEIGHT = 150,
	TOPPADDING = 20,
	BARWIDTH = 250,
	CLEFZONE = 60,
	Xmargin = 10
) {
	try {
		if (!midiUrl) throw new Error('midiUrl is required');
		const resp = await fetch(midiUrl);
		if (!resp.ok) throw new Error(`Failed to fetch MIDI (${resp.status})`);
		const blob = await resp.blob();
		const filename = midiUrl.split('/').pop() || 'remote.mid';
		const file = new File([blob], filename, { type: blob.type || 'audio/midi' });
		file.name = filename; // ensure name is set
		drawScore(file, ELEMENT_FOR_RENDERING, ELEMENT_FOR_COMMENTS, GENERALWIDTH, HEIGHT, TOPPADDING, BARWIDTH, CLEFZONE, Xmargin);
	} catch (err) {
		console.error('renderMidiFromUrl error:', err);
		const commentsDiv = document.getElementById(ELEMENT_FOR_COMMENTS);
		if (commentsDiv) {
			commentsDiv.innerHTML = `Error loading MIDI: ${err.message}`;
		}
	}
}

/**
 * createmeasureMap
 * ----------------
 * Generates a mapping of measure indexes to the starting ticks for each measure.
 *
 * @param {Array} midiEvents - Array of MIDI event objects (each should have an "absTime" property).
 * @param {number} ticksPerBeat - The number of ticks per quarter note.
 * @returns {Object} An object where each key is a measure index (starting at 0) and the value is the start tick.
 *
 * The function handles time signature changes by processing all time signature events.
 * If no time signature event is found at tick 0, a default 4/4 is inserted.
 */
function createMeasureMap(midiEvents, ticksPerBeat) {
	console.log("FOO: midiRenderer.js - createmeasureMap");
	const midiRead = document.getElementById("midiRead");
	if (midiRead) midiRead.innerHTML = `<p>Step Read</p><br/>`;
	const vexRead = document.getElementById("vexRead");
	if (vexRead) vexRead.innerHTML = `<p>Vex Read</p><br/>`;
	const measure_map = document.getElementById("measureMap");
	if (measure_map) measure_map.innerHTML = `<p>Measure Map</p><br/>`;
	const logMeasure = (msg) => {
		if (measure_map) measure_map.innerHTML += msg + "<br>";
		console.log(msg);
	};

	if (midiEvents.length === 0) return {};

	// Ensure time signature at tick 0
	midiEvents = AddStartTimeSignature(midiEvents);

	const timeSignatureEvents = midiEvents.filter(e => e.type === 0xFF && e.metaType === 0x58);
	const maxAbsTime = midiEvents.reduce((m, e) => e.absTime > m ? e.absTime : m, 0);

	const measureMap = {};
	let barIndex = 0;

	for (let i = 0; i < timeSignatureEvents.length; i++) {
		const ts = timeSignatureEvents[i];
		const numerator = ts.data?.[0] ?? 4;
		const denominator = ts.data?.[1] ? Math.pow(2, ts.data[1]) : 4;
		const ticksPerMeasure = ticksPerBeat * numerator * 4 / denominator;

		const segmentStart = ts.absTime;
		const segmentEnd = (i + 1 < timeSignatureEvents.length)
			? timeSignatureEvents[i + 1].absTime
			: maxAbsTime;

		for (let tick = segmentStart; tick < segmentEnd; tick += ticksPerMeasure) {
			measureMap[barIndex] = tick;
			barIndex++;
			logMeasure(`Measure ${barIndex} starts at tick ${tick}`);
		}


		// Keep reference to last segment properties for trailing sentinel
		if (i === timeSignatureEvents.length - 1) {
			const lastStart = measureMap[barIndex - 1];
			const sentinelStart = lastStart + ticksPerMeasure; // always add sentinel
			measureMap[barIndex] = sentinelStart;
			console.log(`Sentinel (end) start added at tick ${sentinelStart} (index ${barIndex})`);
			logMeasure(`Sentinel measure start at tick ${sentinelStart}`);
		}


	}
	console.log("TS: Final measureMap:", measureMap);

	return measureMap;
}

// ----------------------
// Helper: ensure EndTrack event exists
// ----------------------
function groupEventsByMeasure(allEvents, measureMap) {
	
	const measures = [];
	const indices = Object.keys(measureMap)
		.map(k => parseInt(k))
		.sort((a, b) => a - b);
	// Last index is sentinel (start of measure after last real), so iterate to length-1
	for (let i = 0; i < indices.length - 1; i++) {
		const startTick = measureMap[indices[i]];
		const nextTick = measureMap[indices[i + 1]];
		let measure = allEvents.filter(ev => ev.absTime >= startTick && ev.absTime < nextTick);
		measure._startTime = startTick;
		measure._endTime = nextTick;
		measure._index = i;
		console.log(`GROUP MEASURE ${i}:`, measure.map(ev => ({ absTime: ev.absTime, type: ev.type, data: ev.data })));
		measures.push(measure);
	}
	return measures;
}

// Helper: check a measure for any Note On with velocity > 0
function hasNoteOn(measure) {
	
	return measure.some(ev => ev.type === 0x9 && ev.data && ev.data.length > 1 && ev.data[1] !== 0);
}


// ФУНКЦІЯ ВІЗУАЛІЗАЦІЇ MIDI ФАЙЛУ
// ----------------------
// Приймає Uint8Array з MIDI файлом, ID елемента для рендерингу, ширину і висоту нотного стану та інші параметри.
// Використовує бібліотеки MidiParser і VexFlow для парсингу MIDI та рендерингу нот.
// Рендерить нотний стан у вказаний HTML елемент.
// Параметри:
// - uint8: Uint8Array з MIDI файлом.
// - ELEMENT_FOR_RENDERING: ID HTML елемента для рендерингу нот.
// - GENERALWIDTH: Ширина нотного стану в пікселях.
// - HEIGHT: Висота кожного рядка нотного стану в пікселях.
// - TOPPADDING: Верхній відступ нотного стану в пікселях.
// - BARWIDTH: Ширина кожного такту в пікселях.
// - CLEFZONE: Ширина зони для ключа та розміру такту в пікселях.
// - Xmargin: Лівий відступ нотного стану в пікселях.
// Повертає: void
// ----------------------
// Використовує SetEventsAbsoluteTime, createmeasureMap, groupEventsByMeasure та інші допоміжні функції.
// Рендеринг відбувається асинхронно з використанням setTimeout для уникнення блокування UI.
// ---------------------

function renderMidiFile(uint8, ELEMENT_FOR_RENDERING, GENERALWIDTH, HEIGHT = 200, TOPPADDING = 20, BARWIDTH = 250, CLEFZONE = 60, Xmargin = 10, commentsDiv) {
	console.log("FOO: midiRenderer.js - renderMidiFileToNotation");
	if (!ELEMENT_FOR_RENDERING) {
		throw new Error(`Element with id ${ELEMENT_FOR_RENDERING} not found.`);
	}
	const midiFormat = getMidiFormat(uint8);
	if (midiFormat === null) {
		throw new Error("Unable to determine MIDI format.");
	}
	const midiFormatString = midiFormat === 0 ? "0" : (midiFormat === 1 ? "1" : "1");
	commentsDiv.innerHTML += `MIDI format: ${midiFormatString}. `;

	const parsed = parseMidiPreferMIDIFile(uint8, midiFormat);
	const midiData = parsed.midiObj;
	
	let ticksPerBeat = Array.isArray(midiData.timeDivision) ? 480 : midiData.timeDivision;
	
	let allEvents = SetEventsAbsoluteTime(midiData);	
	
	// Перевірка наявності EndTrack
	ensureEndEvent(allEvents);

	const measureMap = createMeasureMap(allEvents, ticksPerBeat);

	console.log("ALL EVENTS (before measures):", allEvents.map(e => ({ absTime: e.absTime, type: e.type, data: e.data })));
	let measures = groupEventsByMeasure(allEvents, measureMap);
	const timeSignatureMap = createTimeSignatureMap(allEvents, measureMap);
	
	measures = ProvideMeasuresWithTimeSignatures(measures, timeSignatureMap);
	measures = ProvideStartAndEndTimesForMeasures(measures, measureMap);//delete

	measures.forEach((measure, index) => {
		console.log(`TS: Measure ${index + 1} time signature: ${measure._timeSignature.numerator}/${measure._timeSignature.denominator} start time: ${measure._startTime}`);
	});

	GENERALHEIGHT = calculateRequiredHeight(measures.length, GENERALWIDTH, BARWIDTH, HEIGHT, TOPPADDING, CLEFZONE, Xmargin);
	logPositions(`Calculated GENERALHEIGHT: ${GENERALHEIGHT} for ${measures.length} measures`);

	setTimeout(() => {
		const factory = new Vex.Flow.Factory({
			renderer: {
				elementId: ELEMENT_FOR_RENDERING,
				width: GENERALWIDTH,
				height: GENERALHEIGHT
			}
		});
		const context = factory.getContext();
		
		renderMeasures(measureMap, measures, ticksPerBeat, context, Xmargin, TOPPADDING, BARWIDTH, CLEFZONE, HEIGHT, GENERALWIDTH, commentsDiv);
	}, 0);
}
// ФУНКЦІЯ РЕНДЕРИНГУ ТАКТІВ
// ----------------------
// Приймає мапу тактів, масив тактів, кількість тіксів на біт, об'єкт score і context з VexFlow та інші параметри.
// Рендерить кожен такт у нотний стан з урахуванням активних нот, пауз, лігатур та інших музичних елементів.
// Параметри:
// - measureMap: Об'єкт, що відображає індекси тактів у початкові тіки.
// - measures: Масив тактів, де кожен такт — це масив MIDI-подій.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - score: Об'єкт score з VexFlow для створення нот.
// - context: Контекст рендерингу з VexFlow.
// - Xmargin: Лівий відступ нотного стану в пікселях.
// - TOPPADDING: Верхній відступ нотного стану в пікселях.
// - BARWIDTH: Ширина кожного такту в пікселях.
// - CLEFZONE: Ширина зони для ключа та розміру такту в пікселях.
// - HEIGHT: Висота кожного рядка нотного стану в пікселях.
// - GENERALWIDTH: Загальна ширина нотного стану в пікселях.
// Повертає: void
// ----------------------
function renderMeasures(measureMap, measures, ticksPerBeat, context, Xmargin, TOPPADDING, BARWIDTH, CLEFZONE, HEIGHT, GENERALWIDTH, commentsDiv) {
	console.log("FOO: midiRenderer.js - renderMeasures ");
	let Xposition = Xmargin;
	let Yposition = TOPPADDING;
	let thresholdGap = ticksPerBeat / 8;


	// Початковий розмір такту (за замовчуванням 4/4)
	let currentNumerator = 4;
	let currentDenominator = 4;
	const activeNotes = {};
	const crossBarTies = {};

	// Флаг для відстеження першого такту на кожному рядку
	let isFirstMeasureInRow = true;
	// Поточний key signature (оновлюється при meta подіях 0x59)
	let currentKeySig = null;

	measures.forEach((m, i) => console.log("MEASURE", i, "TYPES:", m.map(e => ({ type: e.type, absTime: e.absTime, data: e.data }))));

	// Prune trailing measures that contain no Note On events (only offs / meta etc.)
	while (measures.length > 0 && !hasNoteOn(measures[measures.length - 1])) {
		console.log("Deleting trailing empty measure without Note On events");
		measures.pop();
	}

	// Перебирає усі такти
	// ----------------------
	// Для кожного такту:
	// - Оновлює розмір такту, якщо є відповідна подія.
	// - Визначає початковий час такту з measureMap.
	// - Встановлює початковий час для активних нот з попереднього такту.
	// - Налаштовує позицію X та Y для нотного стану.
	// - Створює нотний стан (stave) з ключем та розміром такту.
	// - Обробляє кожну подію в такті:
	//   - Для Note On: додає ноту, обробляє паузи між нотами, перевіряє наявність активних нот.
	//   - Для Note Off: завершує ноту, додає лігатури, видаляє ноту з активних.
	// - Якщо є активні ноти в кінці такту, домалюємо їх до кінця такту.
	// - Якщо немає активних нот, перевіряє наявність пропущених пауз і додає їх.
	// - Рендерить такт у нотний стан.
	// - Оновлює позицію X для наступного такту.
	// ----------------------
	measures.forEach((measure, index) => {
		measure = normalizeMetaEvents(measure);


		if (measure._startTime === undefined) {
			measure._startTime = measureMap[index] !== undefined ? measureMap[index] : index * CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator);
		}

		if (measure._endTime === undefined) {
			measure._endTime = measureMap[index + 1] !== undefined ? measureMap[index + 1] : measure._startTime + CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator);	
		}		

		let keySignatureChanged;
		let keySigName;
		({ keySignatureChanged, keySigName, currentKeySig } = getKeySignatureChanges(measure, currentKeySig));


		logMeasureEvents(measure);

		// Зберігаємо попередній розмір такту для порівняння
		const previousNumerator = currentNumerator;
		const previousDenominator = currentDenominator;

		({ currentNumerator, currentDenominator } = adjustTimeSignature(measure, currentNumerator, currentDenominator));
		let ticksPerMeasure = CalculateTicksPerMeasure(currentNumerator, ticksPerBeat, currentDenominator);
		measure._ticksPerMeasure = ticksPerMeasure;

		// Перевіряємо, чи змінився розмір такту
		const timeSignatureChanged = (currentNumerator !== previousNumerator || currentDenominator !== previousDenominator);
		measure._timeSignatureChanged = timeSignatureChanged;

		// Use precise start from measureMap if present, else fallback
		
		const hasActiveNotesAtBarStart = Object.keys(activeNotes).length > 0;
		console.log(`Measure ${index + 1}: startTime=${measure._startTime}, endTime=${measure._endTime}, timeSignature=${currentNumerator}/${currentDenominator}, ticksPerMeasure=${ticksPerMeasure}, activeNotesAtStart=${hasActiveNotesAtBarStart}`);

		// --- Встановлюємо startTime для активних нот на початку такту ---
		
		processActiveNotesFromPreviousBar(activeNotes, index, measure._startTime);
		

		// Перевіряємо, чи потрібно перейти на новий рядок
		const oldYposition = Yposition;
		({ Xposition, Yposition } = adjustXYposition(Xposition, GENERALWIDTH, BARWIDTH, Yposition, HEIGHT, Xmargin, index, measure._startTime));
		console.log(`Measure ${index + 1}: Xposition=${Xposition}, Yposition=${Yposition}`);
		measure._xposition = Xposition;
		measure._yposition = Yposition;

		// Якщо Y позиція змінилась, значить почався новий рядок
		if (Yposition !== oldYposition) {
			isFirstMeasureInRow = true;
			console.log(`New row started at measure ${index + 1}, Y position: ${Yposition}`);
		}

		let STAVE_WIDTH = adjustStaveWidth(BARWIDTH, index, CLEFZONE, isFirstMeasureInRow, timeSignatureChanged);

		// Створюємо нотний стан (stave)
		// Додаємо ключ та розмір такту, якщо потрібно (тепер також і key signature)
		console.log(`Creating stave for measure ${index + 1} at position (${Xposition}, ${Yposition}) with width ${STAVE_WIDTH}, time signature ${currentNumerator}/${currentDenominator}, key signature ${keySigName || 'none'}`);
		const stave = setStave(Xposition, Yposition, STAVE_WIDTH, index, currentNumerator, currentDenominator, isFirstMeasureInRow, timeSignatureChanged, keySignatureChanged, keySigName);

		// Малюємо нотний стан
		console.log(`Drawing stave for measure ${index + 1}`);
		stave.setContext(context).draw();

		const notes = [];
		const ties = [];
		let lastNoteOffTime = 0;
		let lastNoteOnTime = -1;

		// NEW: поточний стан показаних альтерацій у межах такту (letter+octave -> '#','b','n')
		const measureAccState = {};

		// почергово обробляємо всі події в такті
		console.log(`Processing events in measure ${index + 1}`);

		// Формує масив notes[] та ties[] для поточного такту
		renderMeasure();

		// Очистка crossBarTies для нот, які не продовжуються в поточному такті
		Object.keys(crossBarTies).forEach(pitch => {
			if (crossBarTies[pitch].measureIndex !== index - 1) {
				delete crossBarTies[pitch];
			}
		});

		console.debug(
			`MR: before drawActiveNotes, measure ${index + 1}, notes:`,
			notes.map((n, i) => `${i}: duration "${n.duration}", noteType "${n.noteType}", keys ${JSON.stringify(n.keys)}`).join(" | ")
		);


		// Якщо є "активні" ноти (activeNotes), домалюємо їх до кінця такту
		if (Object.keys(activeNotes).length > 0) {
			const nextBoundary = measureMap[index + 1];
			const measureEndTick = (nextBoundary !== undefined) ? nextBoundary : measure._startTime + ticksPerMeasure; // fallback
			console.log(`AN: bar ${index + 1}, measureEndTick = ${measureEndTick}`)
			console.log(
				"AN: BEFORE drawActiveNotes:",
				JSON.stringify(activeNotes)
			);

			drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState, crossBarTies);
		}

		//console.log(
		//    `MR: before AddMissingRest, measure ${index + 1}, notes:`,
		//    notes.map((n, i) => `${i}: duration "${n.duration}", noteType "${n.noteType}", keys ${JSON.stringify(n.keys)}`).join(" | ")
		//);


		// Якщо остання нота не доходить до кінця такту, додаємо паузу        
		if (Object.keys(activeNotes).length === 0) {
			console.log(`check for missing rests in measure ${index + 1}`);
			const relativeLastNoteOffTime = lastNoteOffTime - measure._startTime;
			addMissingRests(relativeLastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat);
		}

		console.log(
			`MR: after AddissingRest, measure ${index + 1}, notes:`,
			notes.map((n, i) => `${i}: duration "${n.duration}", noteType "${n.noteType}", keys ${JSON.stringify(n.keys)}`).join(" | ")
		);


		// Перевіряємо, чи потрібно скоротити останню ноту/паузу, якщо вона виходить за межі такту
		correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat);

		console.log(
			`MR: start to draw measure ${index + 1}, notes:`,
			notes.map((n, i) => `${i}: duration "${n.duration}", noteType "${n.noteType}", keys ${JSON.stringify(n.keys)}`).join(" | ")
		);



		// Передаємо ticksPerBeat в drawMeasure
		drawMeasure(notes, BARWIDTH, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat);

		Xposition += STAVE_WIDTH;

		// Скидаємо флаг після обробки першого такту в рядку
		isFirstMeasureInRow = false;


		// Трансформує MIDI-події у нотний текст
		// ----------------------
		// // Для кожної події:
		// - Якщо це Note On з velocity > 0:
		//   - Додає паузу на початку такту, якщо це перша нота і немає активних нот.
		//   - Додає паузи між останньою Note Off і поточною Note On.
		//   - Перевіряє, чи збігається час з останньою Note On.
		//   - Додає ноту до активних нот.
		// - Якщо це Note Off або Note On з velocity = 0:
		//   - Завершує ноту, обчислює її тривалість.
		//   - Додає ліги між нотами.
		//   - Видаляє ноту з активних нот.
		// - Додає обчислені ноти і паузи до масиву notes[]        
		// ----------------------
		function renderMeasure() {

			let isFirstNoteInMeasure = true;

			const isNoteOn = (ev) =>
				ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] > 0;

			const isNoteOff = (ev) =>
				ev.type === 0x8 || (ev.type === 0x9 && Array.isArray(ev.data) && ev.data[1] === 0);

			// Сортуємо події:
			// 1. за абсолютним часом
			// 2. на одному тіку спочатку NoteOff, потім NoteOn
			const sorted = [...measure].sort((a, b) => {
				if (a.absTime !== b.absTime) {
					return a.absTime - b.absTime;
				}

				const rank = (ev) => isNoteOff(ev) ? 0 : (isNoteOn(ev) ? 1 : 2);
				return rank(a) - rank(b);
			});

			// Захист від дубльованих NoteOff
			const closedAtTick = new Set();

			/*
			 СКЛАДЕНІ ТРИВАЛОСТІ:
			 */
			const noteGroups = new Map();


			sorted.forEach((event) => {
				// --------------------------------------------------
				// NOTE ON
				// --------------------------------------------------
				if (isNoteOn(event)) {

					const pitch = event.data[0];				
					console.debug(`MR NO: Note On for pitch ${pitch} at absTime ${event.absTime}`);

					/* Пауза на початку такту */
					if (isFirstNoteInMeasure) {
						AddStartRest(event, ticksPerBeat, thresholdGap, notes, measure._startTime, activeNotes, hasActiveNotesAtBarStart);

						isFirstNoteInMeasure = false;
					}
					/* Пауза між нотами */
					if (event.absTime !== lastNoteOnTime) {
						addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes, measure._startTime);
					}

					activeNotes[pitch] = { startTime: event.absTime, endTime: null };

					lastNoteOnTime = event.absTime;
				}

				// --------------------------------------------------
				// NOTE OFF
				// --------------------------------------------------
				else if (isNoteOff(event)) {
										
					const pitch = event.data[0];
					const key = `${pitch}@${event.absTime}`;
					console.debug(`MR: Note Off for pitch ${pitch} at absTime ${event.absTime}, key=${key}`);
					if (closedAtTick.has(key)) {
						console.debug(`MR: already processed`);
						return;
					}
					
					activeNotes[pitch].endTime = event.absTime;					

					closedAtTick.add(key);
					console.debug(`MR NO: Added key ${key} to closedAtTick set`);
					const startTime = activeNotes[pitch].startTime;
					
					if (startTime !== undefined) {						
						const durationTicks = event.absTime - startTime;						

						if (durationTicks > 0) {

							const durationsCode = getDurationFromTicks(durationTicks, ticksPerBeat);
							const { key: vexKey, accidental } = midiNoteToVexFlowWithKey(pitch, currentKeySig);
							const nominalTicksArr = durationsCode.map(dc => calculateTicksFromDuration(dc, ticksPerBeat));
							const nominalSum = nominalTicksArr.reduce((a, b) => a + b, 0) || 1;
							console.debug(`MR: processing pitch ${pitch}, startTime=${startTime}, endTime=${event.absTime}, durationTicks=${durationTicks}, durationsCode=${JSON.stringify(durationsCode)}, nominalTicksArr=${JSON.stringify(nominalTicksArr)}, nominalSum=${nominalSum}`);

							/*
							 * Кожна частина складної тривалості створюється окремо.
							 *
							 * Наприклад:
							 * 960 ticks -> ["h", "h"]
							 *
							 * Для акорду кожна така частина також
							 * повинна бути окремим StaveNote.
							 */							
							durationsCode.forEach(
								(durationCode, pieceIdx) => {
									
									const accToDraw = decideAccidentalForNote(vexKey, accidental, currentKeySig, measureAccState, pieceIdx);
									const nominalTicks = nominalTicksArr[pieceIdx];
									const allocatedTicks = Math.round(durationTicks * (nominalTicks / nominalSum));																
									let pieceStartTime = startTime;

									for (let i = 0; i < pieceIdx; i++) {
										pieceStartTime += nominalTicksArr[i];
									}

									/*
									 * Групуємо ноти за:час початку + тривалість
									 */
									const groupKey = `${pieceStartTime}@${allocatedTicks}`;
									console.debug(`MR DC: processing pieceIdx=${pieceIdx} for pitch=${pitch}, vexKey=${vexKey}, accToDraw=${accToDraw}, pieceStartTime=${pieceStartTime}, allocatedTicks=${allocatedTicks}, durationCode=${durationCode}, groupKey=${groupKey}`);

									if (!noteGroups.has(groupKey)) {
										noteGroups.set(groupKey, {
											startTime: pieceStartTime,
											durationTicks: allocatedTicks,
											durationCode: durationCode,
											notes: []
										});
									}

									const group = noteGroups.get(groupKey);
									console.log(`MR DC: adding note to group ${groupKey}: pitch=${pitch}, vexKey=${vexKey}, accToDraw=${accToDraw}`);

									group.notes.push({
										pitch: pitch,
										vexKey: vexKey,
										accidental: accToDraw
									});
								}
							);
							console.log(`MR: DC before=${JSON.stringify(durationsCode)}`);
						}

						lastNoteOffTime = Math.max(lastNoteOffTime, event.absTime);
					}
					else {
						console.debug(`MR: Note Off for pitch ${pitch} at absTime ${event.absTime} has no corresponding Note On (startTime undefined)`);
					}

					if (activeNotes[pitch] !== undefined && event.absTime >= measure._startTime) {
						console.debug(`MR: removing pitch ${pitch} from activeNotes`);
						delete activeNotes[pitch];
					}
				}
			});

			/*
			 * ------------------------------------------------------
			 * Створюємо АКОРДИ
			 * ------------------------------------------------------
			 * Одна нота:keys: ["c/4"]
			 * Акорд: keys: ["c/4", "e/4", "g/4"]             
			 */

			console.log(`MR DC: noteGroups for measure ${index + 1}:`, Array.from(noteGroups.entries()).map(([k, v]) => `${k} -> [${v.notes.map(n => n.vexKey).join(", ")}]`).join(" | "));

			const groups = Array.from(noteGroups.values())
				.sort((a, b) => {
					if (a.startTime !== b.startTime) {
						return a.startTime - b.startTime;
					}

					return a.durationTicks - b.durationTicks;
				});

			/*
			 * Для ліг зберігаємо останню VexFlow-ноту
			 * окремо для кожної MIDI-висоти.
			 *
			 * Це важливо, тому що тепер одна VexFlow-нота
			 * може містити декілька keys.
			 */
			const previousPiecesByPitch = {};


			groups.forEach(group => {
				const chordNotes = group.notes;
				/*
				 * Захист від дублювання однієї й тієї самої MIDI-висоти в групі.
				 */
				const uniqueNotes = [];
				const usedPitches = new Set();

				chordNotes.forEach(noteData => {
					if (!usedPitches.has(noteData.pitch)) {
						usedPitches.add(noteData.pitch);
						uniqueNotes.push(noteData);
					}
				});

				if (uniqueNotes.length === 0) {
					return;
				}

				/*
				 * Сортуємо keys від нижньої ноти до верхньої.
				 */
				uniqueNotes.sort((a, b) => {
					return a.pitch - b.pitch;
				});

				const keys = uniqueNotes.map(noteData => noteData.vexKey);
				console.log(`MR DC: keys for group at startTime=${group.startTime}: ${keys.join(", ")}`);

				const durationCode = group.durationCode;
				console.log(`MR DC: durationCode for group at startTime=${group.startTime}: ${durationCode}`);

				const accidentals = uniqueNotes.map(noteData => noteData.accidental);

				const note = processNoteElement(durationCode, keys, accidentals);

				if (!note) {
					return;
				}

				note.__srcTicks = group.durationTicks;
				note.__startTime = group.startTime - measure._startTime;

				// -------------------------------------------------
				// Cross-bar tie:
				// якщо ця MIDI-нота продовжується з попереднього такту,
				// зв'язуємо попередню VexFlow-ноту з поточною.
				// -------------------------------------------------


				uniqueNotes.forEach(noteData => {
					const previousCrossBar = crossBarTies[noteData.pitch];

					if (previousCrossBar && previousCrossBar.note && group.startTime === measure._startTime) {
						AddTie(previousCrossBar.note, ties, note, previousCrossBar.keyIndex, uniqueNotes.indexOf(noteData));
						logTies(`MR: Added cross-bar tie for pitch ${noteData.pitch} from previous measure to current measure`);
						delete crossBarTies[noteData.pitch];
					}
				});

				console.log(`MR: adding note to notes array: measure ${index + 1} duration=${durationCode}, keys=${keys.join(", ")}`);
				notes.push(note);


				uniqueNotes.forEach(noteData => {
					previousPiecesByPitch[noteData.pitch] = note;
				});

				uniqueNotes.forEach(noteData => {
					previousPiecesByPitch[noteData.pitch] = note;
				});

				notes.sort((a, b) =>
					(a.__startTime ?? 0) - (b.__startTime ?? 0)
				);

				// -------------------------------------------------
			});
		}
	});

};

// ----------------------
// Допоміжні функції для обробки ключових знаків (key signatures)
// повертає об'єкт { keySignatureChanged, keySigName, currentKeySig }
// ----------------------
function getKeySignatureChanges(measure, currentKeySig) {
	let ks = updateKeySignatureFromEvents(measure);
	if (ks) { console.log(`ks: Tonality: ${ks.sf}, Mode: ${ks.mi}`); }
	let keySignatureChanged = false;
	if (ks) {
		if (!currentKeySig || currentKeySig.sf !== ks.sf || currentKeySig.mi !== ks.mi) {
			currentKeySig = ks;
			keySignatureChanged = true;
			console.log(`Key signature changed -> sf:${ks.sf} mi:${ks.mi}`);
		}
	}
	const keySigName = currentKeySig ? mapKeySignatureName(currentKeySig.sf, currentKeySig.mi) : null;
	return { keySignatureChanged, keySigName, currentKeySig };
}

// ----------------------
// Допоміжні функції для рендерингу нотного стану з MIDI файлу
// Скорочення нот, якщо виходять за межі такту
// повертає void
// ----------------------

function correctExtraNotes(notes, ticksPerMeasure, ticksPerBeat) {
	console.log("FOO: midiRenderer.js - correctExtraNotes");
	if (notes.length === 0) return;

	// Обчислюємо загальну тривалість всіх нот у тіках
	let totalTicks = 0;
	notes.forEach(note => {
		// Отримуємо тривалість ноти/Паузи
		const duration = note.getDuration();
		const isRest = duration.endsWith('r');
		const baseDuration = isRest ? duration.slice(0, -1) : duration;
		const dotted = /\./.test(duration);
		const noteTicks = calculateTicksFromDuration(baseDuration, ticksPerBeat);
		totalTicks += noteTicks;
	});

	console.log(`AN: Total measure ticks: ${totalTicks}, allowed: ${ticksPerMeasure}`);

	// Якщо загальна тривалість перевищує такт, скорочуємо останню ноту
	if (totalTicks > ticksPerMeasure && notes.length > 0) {
		const lastNote = notes[notes.length - 1];
		const lastNoteDuration = lastNote.getDuration();
		const isRest = lastNoteDuration.endsWith('r');
		const baseDuration = isRest ? lastNoteDuration.slice(0, -1) : lastNoteDuration;
		const dotted = /\./.test(lastNoteDuration);
		const lastNoteTicks = calculateTicksFromDuration(baseDuration, ticksPerBeat);

		// Обчислюємо скільки тіків залишається для останньої ноти
		const excessTicks = totalTicks - ticksPerMeasure;
		const remainingTicks = lastNoteTicks - excessTicks;

		console.log(`Last note excess: ${excessTicks}, remaining: ${remainingTicks}`);

		if (remainingTicks > 0) {
			// Знаходимо найближчу підходящу тривалість для залишкових тіків
			const newDurations = getDurationFromTicks(remainingTicks, ticksPerBeat);
			if (newDurations.length > 0) {
				const newDuration = newDurations[0]; // Беремо першу (найбільшу) тривалість
				const finalDuration = isRest ? newDuration + 'r' : newDuration;

				// Створюємо нову ноту з скороченою тривалістю
				if (isRest) {
					const newRest = createRest(newDuration);
					if (newRest) {
						notes[notes.length - 1] = newRest;
						console.log(`Shortened last rest to: ${finalDuration}`);
					}
				} else {
					// Для нот потрібно зберегти висоту та знаки альтерації
					const keys = lastNote.getKeys();
					const newNote = processNoteElement(newDuration, keys[0], null);
					if (newNote) {
						// Копіюємо знаки альтерації з оригінальної ноти
						const modifiers = lastNote.getModifiers();
						modifiers.forEach(modifier => {
							if (modifier.constructor.name === 'Accidental') {
								newNote.addModifier(modifier);
							}
						});
						// APPLY AUTO STEM for shortened note
						applyAutoStem(newNote, newDuration);
						notes[notes.length - 1] = newNote;
						console.log(`AN: Shortened last note to: ${newDuration}`);
					}
				}
			}
		} else {
			// Якщо залишкових тіків недостатньо, видаляємо останню ноту
			console.log("AN: Removing last note - no remaining ticks");
			notes.pop();
		}
	}
}

// ----------------------
// Функція для обробки активних нот з попереднього такту
// повертає їх у масив notes[] та ties[] для подальшого рендерингу
// повертає void
// ----------------------
function processActiveNotesFromPreviousBar(activeNotes, index, barStartAbsTime) {

	//призначаємо startTime для активних нот з попереднього такту на початок поточного такту
	if (Object.keys(activeNotes).length > 0 && index > 0) {
		
		Object.keys(activeNotes).forEach(pitch => {
			activeNotes[pitch].startTime = barStartAbsTime;
		});
		logactivenotes(index);
	} 
	function logactivenotes(index) {
		const activeNoteList = Object.entries(activeNotes)
			.map(([pitch, { startTime, endTime }]) => `pitch=${pitch} (start=${startTime}, end=${endTime})`)
			.join(", ");
		logActiveNotes(`AN: active notes from previous bar ${index}: ${activeNoteList}`);				
	}
}

// ----------------------
// Функція для малювання активних нот у кінці такту
// повертаємо їх у масив notes[] та ties[] для подальшого рендерингу
// повертає void
// ----------------------
function drawActiveNotes(activeNotes, measureEndTick, ticksPerBeat, notes, ties, currentKeySig, measureAccState, crossBarTies) {

	logActiveNotes(
		`\n------------------------\nAN: activeNotes.count = ${Object.keys(activeNotes).length}`
	);

	if (!activeNotes || typeof activeNotes !== "object") {
		console.warn("drawActiveNotes: activeNotes is not an object");
		return;
	}
	if (typeof measureEndTick !== "number" || measureEndTick < 0) {
		console.warn("drawActiveNotes: measureEndTick is not a valid number");
		return;
	}

	// ---------------------------------------------------------
	// Формуємо групу активних нот
	// ---------------------------------------------------------
	const activeGroup = [];

	Object.keys(activeNotes).forEach(pitch => {

		/* визначаємо висоту і тривалість активних нот */
		const midiPitch = Number(pitch);
		const startTime = activeNotes[pitch].startTime;
		const endTime = activeNotes[pitch].endTime;
		const durationTicks = endTime != null ? endTime - startTime : measureEndTick - startTime;
				
		if (durationTicks < 0) {
			console.warn(
				`AN: could not process activeNote, ` +
				`duration: ${durationTicks} ticks`
			);
			return;
		}
		/* переписуємо в формат нота + знак альтерації */
		const { key, accidental } = midiNoteToVexFlowWithKey(midiPitch, currentKeySig);

		logActiveNotes(`AN: found activeNote ${midiPitch} ` + `(${key}${accidental || ""})`);

		/* усі додаємо в групу*/
		activeGroup.push({
			pitch: midiPitch,
			key: key,
			accidental: accidental,
			startTime: startTime,
			endTime: endTime,
			durationTicks: durationTicks
		});
	});

	if (activeGroup.length === 0) {
		return;
	}

	/* сортуємо за висотою */
	activeGroup.sort(
		(a, b) => a.pitch - b.pitch
	);

	/* визначаємо тривалість та час початку за першою нотою */
	const durationTicks = activeGroup[0].durationTicks;
	const startTime = activeGroup[0].startTime;
	let durationsCode = getDurationFromTicks(durationTicks,ticksPerBeat);
	logActiveNotes(`AN: durationTicks = ${durationTicks}, ` + ` durationsCode = ${durationsCode.join(", ")}`);
	
	/* якщо код з кількох тривалостей працюємо з синкопами */
	durationsCode = adjustDurationCode(durationsCode, startTime, durationTicks, ticksPerBeat);
	logActiveNotes(`AN: adjusted durationTicks = ${durationTicks}, ` + ` durationsCode = ${durationsCode.join(", ")}`);

	const nominalTicksArr = durationsCode.map(dc => calculateTicksFromDuration(dc, ticksPerBeat));

	const nominalSum =nominalTicksArr.reduce((a, b) => a + b, 0) || 1;
	logActiveNotes(`AN: nominalTicksArr = ${nominalTicksArr.join(", ")}, nominalSum = ${nominalSum}`);

	// ---------------------------------------------------------
	// Кожен шматок ноти / акорду
	// ---------------------------------------------------------

	durationsCode.forEach(
		(durationCode, pieceIdx) => {

			const isTriplet = typeof durationCode === "string" && durationCode.endsWith("t");

			const isDotted = typeof durationCode === "string" && durationCode.endsWith(".");
			const baseDuration = (isTriplet || isDotted) ? durationCode.slice(0, -1): durationCode;

			// -------------------------------------------------
			// Формуємо keys акорду
			// -------------------------------------------------

			const keys = [];
			const accidentals = [];
					

			activeGroup.forEach(noteData => {

				const accToDraw = decideAccidentalForNote(noteData.key, noteData.accidental, currentKeySig, measureAccState, pieceIdx);

				keys.push(noteData.key);
				accidentals.push(accToDraw);
			});

			// -------------------------------------------------
			// Створюємо StaveNote
			// -------------------------------------------------

			const note =
				new Vex.Flow.StaveNote({
					keys: keys,
					duration: baseDuration
				});

			// -------------------------------------------------
			// Accidentals
			// -------------------------------------------------

			accidentals.forEach(
				(accidental, keyIndex) => {

					if (accidental) {
						note.addAccidental(keyIndex, new Vex.Flow.Accidental(accidental));
					}
				}
			);

			// -------------------------------------------------
			// Dotted
			// -------------------------------------------------

			if (isDotted && !isTriplet) {
				keys.forEach((_, keyIndex) => note.addDot(keyIndex));
				console.log(`MR: added dot to note with keys: ${keys.join(", ")}`);
			}

			// -------------------------------------------------
			// Triplet
			// -------------------------------------------------

			if (isTriplet) {
				note.__isTriplet = true;
				note.__tripletBase = baseDuration;
				note.__durationCode = durationCode;
			}

			logActiveNotes(`AN: creating StaveNote for pieceIdx = ${pieceIdx} keys: ${keys.join(", ")}, durationCode: ${durationCode}, baseDuration: ${baseDuration}, accidentals: ${accidentals.join(", ")}, dot: ${isDotted}, triplet: ${isTriplet}`
			);

			applyAutoStem(note,durationCode);

			// -------------------------------------------------
			// Реальна тривалість цього шматка
			// -------------------------------------------------

			const allocatedTicks = Math.round(durationTicks *  (nominalTicksArr[pieceIdx] /nominalSum));

			logActiveNotes(`AN: allocatedTicks = ${allocatedTicks}`);

			note.__srcTicks = allocatedTicks;
			note.__startTime = activeGroup[0].startTime + nominalTicksArr.slice(0, pieceIdx).reduce((a, b) => a + b, 0);
			// -------------------------------------------------
			// Інформація про ноти акорду
			// -------------------------------------------------

			note.__midiPitches = activeGroup.map(noteData => noteData.pitch);

			if (activeGroup.length > 1) {
				note.__isChord = true;
				note.__chordKeys = keys.slice();				
			} 
			notes.push(note);

			// -------------------------------------------------
			// Внутрішня частина лігатури між шматками
			// -------------------------------------------------

			if (pieceIdx > 0) {

				const previousNote = notes[notes.length - 2];
				AddTie(previousNote, ties,note);
				logActiveNotes(`AN: internal tie added between pieceIdx ${pieceIdx - 1} and ${pieceIdx}`);
				logTies(`TIE: internal tie added between pieceIdx ${pieceIdx - 1} and ${pieceIdx}`);
			}


			// -------------------------------------------------
			// Фіксуємо інформацію для cross-bar tie
			//
			// ВАЖЛИВО:
			// тут НЕ зберігаємо Vex.Flow.StaveNote
			// з попереднього такту.
			//
			// Зберігаємо музичну інформацію:
			// MIDI pitch + індекс ноти в акорді.
			// -------------------------------------------------

			if (pieceIdx === durationsCode.length - 1) {

				if (pieceIdx === durationsCode.length - 1) {
					activeGroup.forEach((noteData, keyIndex) => {
						if (!crossBarTies || noteData.endTime <= measureEndTick) return;

						crossBarTies[noteData.pitch] = {
							pitch: noteData.pitch,
							keyIndex: keyIndex,
							key: noteData.key,
							endTime: noteData.endTime,
							note: note
						};

						logActiveNotes(`AN: crossBarTie registered: pitch=${noteData.pitch}, keyIndex=${keyIndex}, endTime=${noteData.endTime}`);
					});
				};
			}
		}
	);
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ПЕРЕВІРКИ ЗБІГУ ЧАСУ НОТИ З ПОПЕРЕДНЬОЮ НОТОЮ
// використовується для побудови акордів
// повертає void
// ----------------------
function checkConcide(event, lastNoteOnTime) {
	console.log("FOO: midiRenderer.js - checkConcide");
	if (event.absTime == lastNoteOnTime) {
		console.log(`NoteOn coincides with previous NoteOn: ${event.absTime}`);
	}
}


// Оновлена функція drawMeasure з покращеною обробкою помилок та перевірками
// ----------------------
// Параметри:
// - notes: Масив нот для рендерингу.
// - BARWIDTH: Ширина такту в пікселях.
// - context: Контекст рендерингу з VexFlow.
// - stave: Об'єкт stave з VexFlow для поточного такту.
// - ties: Масив ліг для нот.
// - index: Індекс поточного такту.
// - commentsDiv: HTML елемент для виведення коментарів.
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// Повертає: void
// ----------------------
// Використовує Vex.Flow.Voice, Vex.Flow.Formatter та makeBeams для форматування та рендерингу нот.

function drawMeasure(notes, BARWIDTH, context, stave, ties, index, commentsDiv, currentNumerator, currentDenominator, ticksPerBeat) {

	try {
		if (notes.length > 0) {


			// Фільтруємо некоректні ноти
			const validNotes = notes.filter(note => note !== null && note !== undefined);


			if (validNotes.length === 0) {
				console.warn(`Measure ${index + 1} has no valid notes to render.`);
				return;
			}

			// Обчислення ребер нот (beams) з обробкою помилок
			let beams = calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator);

			// Створюємо voice з урахуванням поточного розміру такту
			const voice = new Vex.Flow.Voice({
				num_beats: currentNumerator || 4,
				beat_value: currentDenominator || 4
			});

			// Встановлюємо строгий режим у false, щоб уникнути помилок через невідповідність тривалостей
			voice.setStrict(false);

			// Додаємо нотні елементи до голосу
			voice.addTickables(validNotes);

			// Форматуємо голос
			const formatter = new Vex.Flow.Formatter();
			formatter.joinVoices([voice]).format([voice], BARWIDTH - 50);

			// логування деталей нот для налагодження
			notes.forEach((note, index) => {
				logvexflowNoteDetails(note, index);
			});


			// Малюємо голос
			voice.draw(context, stave);

			// Розпізнаємо й малюємо тріолі
			drawTuplets(currentNumerator, currentDenominator, validNotes, ticksPerBeat, index, context);

			// Домальовуємо ребра нот (beams) 
			drawBeams(beams, context, index);

			// Домальовуємо ліги (ties) 
			drawTies(ties, context, index);
			console.log(`Measure ${index + 1} rendered successfully with ${validNotes.length} notes, ${beams.length} beams, and ${ties.length} ties.`);

		} else {
			console.warn(`Measure ${index + 1} has no notes to render.`);
		}
	} catch (error) {
		console.error(`Error rendering measure ${index + 1}: ${error.message}`);
		commentsDiv.innerHTML += `<br>Error rendering measure ${index + 1}: ${error.message}<br>`;
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І ОБЧИСЛЕННЯ НОТНИХ РЕБЕР (BEAMS) З ОБРОБКОЮ ПОМИЛОК
// використовує makeBeams.js 
// ----------------------
function calculateBeams(validNotes, ticksPerBeat, index, currentNumerator, currentDenominator) {
	let beams = [];
	if (typeof makeBeams === 'function' && validNotes.length > 0 && ticksPerBeat) {
		try {
			// Створюємо правильну структуру для makeBeams
			const measureForBeams = {
				notes: validNotes.map(note => ({
					vexNote: note // Обгортаємо StaveNote в очікувану структуру
				}))
			};
			console.log(`Calling makeBeams for measure ${index + 1} with ${validNotes.length} notes and ticksPerBeat ${ticksPerBeat}`);

			const timeSignature = { num: currentNumerator, den: currentDenominator };
			const beamResult = makeBeams(measureForBeams, ticksPerBeat, timeSignature);

			beams = beamResult.beams || [];
			console.log(`makeBeams found ${beams.length} beam groups for measure ${index + 1}`);
		} catch (beamError) {
			console.warn(`Error in makeBeams for measure ${index + 1}:`, beamError);
			beams = []; // Fallback до порожнього масиву
		}
	} else {
		console.log(`makeBeams skipped for measure ${index + 1}: function=${typeof makeBeams}, notes=${validNotes.length}, ticksPerBeat=${ticksPerBeat}`);
	}
	return beams;
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ ЛІГ З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.StaveTie
// Параметри:
// - ties: Масив об'єктів StaveTie з VexFlow.
// - context: Контекст рендерингу з VexFlow.
// - index: Індекс поточного такту.
// Повертає: void
// ----------------------
function drawTies(ties, context, index) {
	ties.forEach((tie, tieIndex) => {
		try {
			tie.setContext(context).draw();
			logTies(`MR: Tie ${tieIndex + 1} drawn for measure ${index + 1}`);
		} catch (tieError) {
			console.warn(`Error drawing tie ${tieIndex + 1} in measure ${index + 1}:`, tieError);
		}
	});
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ ТРІОЛЕЙ З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.Tuplet
// Параметри:
// - currentNumerator: Поточний чисельник розміру такту.
// - currentDenominator: Поточний знаменник розміру такту.
// - validNotes: Масив об'єктів StaveNote з VexFlow.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - index: Індекс поточного такту.
// - context: Контекст рендерингу з VexFlow.
// Повертає: void
// ----------------------
function drawTuplets(currentNumerator, currentDenominator, validNotes, ticksPerBeat, index, context) {
	let tuplets = [];
	try {
		if (typeof detectTuplets === 'function') {
			const timeSignature = { num: currentNumerator || 4, den: currentDenominator || 4 };
			const measureForTuplets = { notes: validNotes.map(vn => ({ vexNote: vn })) };
			tuplets = detectTuplets(measureForTuplets, ticksPerBeat, timeSignature);
			console.log(`MR:detectTuplets found ${tuplets.length} tuplets for measure ${index + 1}`);
		}
	} catch (te) {
		console.warn(`MR:detectTuplets error in measure ${index + 1}:`, te);
	}
	if (tuplets && tuplets.length) {
		tuplets.forEach((t, tIdx) => {
			try { t.setContext(context).draw(); }
			catch (e) { console.warn(`MR:Error drawing tuplet ${tIdx + 1} in measure ${index + 1}:`, e); }
		});
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОБРОБКИ І МАЛЮВАННЯ BEAMS З ОБРОБКОЮ ПОМИЛОК
// Використовує Vex.Flow.Beam
// Параметри:
// - beams: Масив об'єктів Beam з VexFlow.
// - context: Контекст рендерингу з VexFlow.
// - index: Індекс поточного такту.
// Повертає: void
// ----------------------
function drawBeams(beams, context, index) {
	if (beams && beams.length > 0) {
		beams.forEach((beam, beamIndex) => {
			try {
				beam.setContext(context).draw();
				console.log(`MR:Beam ${beamIndex + 1} drawn for measure ${index + 1}`);
			} catch (beamError) {
				console.warn(`MR:Error drawing beam ${beamIndex + 1} in measure ${index + 1}:`, beamError);
			}
		});
	}
	else {
		console.log(`MR:No beams to draw for measure ${index + 1}`);
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ СТВОРЕННЯ ПАУЗИ (REST) З ВИКОРИСТАННЯМ VEXFLOW
// Параметри:
// - durationCode: Рядок тривалості паузи (наприклад, "q", "h", "8", "16.", тощо).
// Повертає: Об'єкт Vex.Flow.StaveNote, що являє собою паузу, або null у разі помилки.
// ----------------------

function addMissingRests(lastNoteOffTime, notes, ticksPerMeasure, thresholdGap, ticksPerBeat) {
	console.log("FOO: midiRenderer.js - addMissingRests");
	while (lastNoteOffTime < ticksPerMeasure - thresholdGap) {
		const remainingTicks = ticksPerMeasure - lastNoteOffTime;
		console.log(`MR:add Missing Rest is running: remaining: ${remainingTicks}`);
		const restDurations = getDurationFromTicks(remainingTicks, ticksPerBeat);
		let timeadded = 0;
		restDurations.forEach((restDuration) => {
			notes.push(createRest(restDuration));
			timeadded += calculateTicksFromDuration(restDuration, ticksPerBeat);
		});
		lastNoteOffTime += timeadded;
		console.log(`MR:adding a rest done, time added = ${timeadded}, update lastNoteOffTime: ${lastNoteOffTime}`);
	}
}


// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ НОТ ІЗ ПОПЕРЕДНЬОГО ТАКТУ
// якщо є активні ноти з попереднього такту
// Параметри:
// - activeNotes: Об'єкт з активними нотами {pitch: startTime}.
// - measure: Масив MIDI подій поточного такту.
// Повертає: void
// ----------------------
function AddNotesFromPreviousBar(activeNotes, measure) {
	console.log("FOO: midiRenderer.js - AddNotesFromPreviousBar");
	Object.keys(activeNotes).forEach(pitch => {
		measure.unshift({
			type: 0x9,
			channel: 0,
			deltaTime: 0,
			absTime: 0,
			data: [Number(pitch), 100],
		});
		console.log(`MR:add note ${pitch} from previous bar`);
	});
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ОТРИМАННЯ ПОТОЧНОГО РОЗМІРУ ТАКТУ З ПОДІЙ TIME SIGNATURE
// шукає подію TimeSignature тільки в першому такті
// повертає {numerator, denominator}
// якщо немає події, повертає 4/4 за замовчуванням
// Параметри:
// - measures: Масив тактів, кожен такт — масив MIDI подій.
// Повертає: Об'єкт {numerator, denominator}.
// ----------------------
function getTimeSignature(measures) {
	console.log("FOO: midiRenderer.js - getTimeSignature");
	let numerator = 4, denominator = 4;
	if (measures.length > 0 && measures[0].length > 0) {
		for (const event of measures[0]) {
			if (event.type === 0xFF && event.metaType === 0x58 && Array.isArray(event.data)) {
				numerator = event.data[0];
				denominator = Math.pow(2, event.data[1]);
				break;
			}
		}
	}
	return { numerator, denominator };
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ЛІГИ (TIE) МІЖ НОТАМИ
// якщо попередня нота не null
// Параметри:
// - previousNote: Попередня нота (Vex.Flow.StaveNote) або null.
// - ties: Масив ліг (Vex.Flow.StaveTie) для рендерингу.
// - note: Поточна нота (Vex.Flow.StaveNote).
// Повертає: void
// ----------------------
function AddTie(previousNote, ties, note, firstIndex = 0, lastIndex = 0) {	
	if (previousNote && note) {
		ties.push(new Vex.Flow.StaveTie({
			first_note: previousNote,
			last_note: note,
			first_indices: [firstIndex],
			last_indices: [lastIndex]
		}));	
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПАУЗ МІЖ НОТАМИ
// якщо є розрив між останнім закінченням ноти і наступним початком ноти
// Параметри:
// - lastNoteOffTime: Абсолютний час закінчення останньої ноти.
// - event: Поточна подія ноти.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - thresholdGap: Мінімальна кількість тіків для додавання паузи.
// - notes: Масив нот для рендерингу.
// Повертає: void
// ----------------------

function addRestsBetween(lastNoteOffTime, event, ticksPerBeat, thresholdGap, notes, barStartAbsTime) {

	if (lastNoteOffTime > 0 && event.absTime > lastNoteOffTime) {
		const gapTicks = event.absTime - lastNoteOffTime;
		const restDurations = getDurationFromTicks(gapTicks, ticksPerBeat);
		if (gapTicks > thresholdGap) {
			restDurations.forEach((restDuration) => {
				console.log(`MR:addRestsBetween: current event abs time ${event.absTime} vs lastNoteOffTime: ${lastNoteOffTime}: rest is needed: ${restDuration}`);
				const rest = createRest(restDuration);
				if (rest) {
					rest.__startTime = lastNoteOffTime - barStartAbsTime;
					notes.push(rest);
				}
				console.log(`MR:addRestsBetween: added rest ${restDuration} at startTime ${rest.__startTime}`);
			});
		}
	}
}

// ----------------------
// ФУНКЦІЯ ДЛЯ ДОДАВАННЯ ПАУЗИ НА ПОЧАТКУ ТАКТУ
// якщо перша подія в такті починається не з 0
// Параметри:
// - event: Перша подія в такті.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// - thresholdGap: Мінімальна кількість тіків для додавання паузи.
// - notes: Масив нот для рендерингу.
// - barStartAbsTime: Абсолютний час початку такту.
// Повертає: void
// ----------------------
function AddStartRest(event, ticksPerBeat, thresholdGap, notes, barStartAbsTime, hasActiveNotesAtBarStart) {
	console.log("FOO: midiRenderer.js - AddStartRest");
	if (hasActiveNotesAtBarStart) {
		return;
	}
	const relTime = event.absTime - barStartAbsTime;
	if (relTime > 0) {
		const restDurations = getDurationFromTicks(relTime, ticksPerBeat);
		if (relTime > thresholdGap) {
			console.log(`adding a starting rest ${relTime}`);
			restDurations.forEach((restDuration) => {
				notes.push(createRest(restDuration));
			});
		}
	} else {
		console.log("no rest needed");
	}
}


const logEvent = (msg) => {

};

const logFOO = (msg) => {

};


// ----------------------
// ФУНКЦІЯ ДЛЯ КОРЕКТУВАННЯ КОДУ ТРИВАЛОСТІ НОТИ ЩО ВИНИКАЮТЬ НА СИНКОПУ і ЙДУТЬ ЧЕРЕЗ ДОЛЮ
// Параметри:
// - durationsCode: Масив кодів тривалості нот (наприклад, ['q', '8', '16']).
// - startTime: Абсолютний час початку ноти в тіках.
// - durationTicks: Тривалість ноти в тіках.
// - ticksPerBeat: Кількість тіків на чвертну ноту.
// Повертає: Масив кодів тривалості нот, скоригованих відповідно до долей.
// ----------------------
function adjustDurationCode(durationsCode, startTime, durationTicks, ticksPerBeat) {
	console.debug(
		`Adjusting duration code: ${durationsCode}, ` +
		`startTime: ${startTime}, ` +
		`durationTicks: ${durationTicks}, ` +
		`ticksPerBeat: ${ticksPerBeat}`
	);

	// Нота почалася точно на долю —
	// додаткового розбиття не потрібно.
	if (startTime % ticksPerBeat === 0) {
		return durationsCode;
	}

	const endTime = startTime + durationTicks;
	const nextBeatBoundary = Math.ceil(startTime / ticksPerBeat) * ticksPerBeat;

	// Якщо наступна доля знаходиться вже за кінцем ноти,
	// нота не перетинає долю.
	if (nextBeatBoundary >= endTime) {
		return durationsCode;
	}

	const firstPartTicks = nextBeatBoundary - startTime;
	const secondPartTicks = endTime - nextBeatBoundary;

	const firstPart = getDurationFromTicks(firstPartTicks, ticksPerBeat);
	const secondPart = getDurationFromTicks(secondPartTicks, ticksPerBeat);

	return [...firstPart, ...secondPart];
}


window.drawScore = drawScore;
window.renderMidiFromUrl = renderMidiFromUrl;
