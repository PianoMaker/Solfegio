document.addEventListener('DOMContentLoaded', function () {
	const chordSelector = document.querySelectorAll('#soundcount input[type=radio]');// радіокнопки
	const selectform = document.getElementById('selectform');						// form for sound count selection
	const recogniseform = document.getElementById('recogniseform');					// form for chord recognition
	const recognisebox = document.getElementById('recognisebox');					// box containing recogniseform
	const recognisebutton = document.getElementById('recogniseButton');				// button to submit recogniseform
	const SelectedQuality = document.getElementById('SelectedQuality');
	const selectedType = document.getElementById('SelectedType');
	const playBtn = document.getElementById('playBtn');								//play button
	const radiobuttons = document.querySelectorAll('input[name="SelectedCount"]');
	const resultBox = document.getElementById('resultBox');

	// Additional mappings (do not change existing triadTypes / ninthQualities)
	// These arrays are used only to recognize SelectedType values rendered in UI
	const intervalPerfectKeys = ['QUARTA', 'QUINTA', 'OCTAVA', 'PRIMA'];
	const intervalPerfectUkr = ['кварта', 'квінта', 'октава', 'прима'];
	const intervalImperfectKeys = ['SECUNDA', 'TERZIA', 'SEKSTA', 'SEPTYMA'];
	const intervalImperfectUkr = ['секунда', 'терція', 'секста', 'септима'];

	// restore saved radio
	const savedSoundCount = localStorage.getItem('selectedSoundCount');
	if (savedSoundCount) {
		chordSelector.forEach(radio => {
			if (radio.value === savedSoundCount) radio.checked = true;
		});
	}

	const hidebox = sessionStorage.getItem('hidebox');
	if (hidebox === 'true') {
		if (recognisebox) {
			recognisebox.style.display = 'none';
			radiobuttons.forEach(btn => {
				btn.addEventListener('change', function () {
					btn.checked = false; // prevent double submission
					sessionStorage.removeItem('hidebox'); // set flag
					console.log('Hiding recognise box based on session storage flag.');
				})
			})
		}
	}

	// restore saved selects
	const savedQuality = localStorage.getItem('SelectedQuality');
	if (savedQuality && SelectedQuality) SelectedQuality.value = savedQuality;
	const savedType = localStorage.getItem('selectedType');
	if (savedType && selectedType) selectedType.value = savedType;

	logChord();

	const hasCheckedRadio = Array.from(radiobuttons).some(btn => btn.checked);

	// КОРОБКА РОЗПІЗНАВАННЯ
	if (recognisebox) {
		if (hasCheckedRadio) {
			recognisebox.style.display = 'flex';
			console.log('Showing recognise box because a sound count is already selected.');
		} else {
			recognisebox.style.display = 'none';
			console.log('Hiding recognise box until user selects sound count.');
		}
	}
	else {
		console.warn('Recognise box element not found.');
	}

	// КНОПКА РОЗПІЗНАННЯ
	if (recognisebutton) {
		recognisebutton.addEventListener('click', () => {
				
			radiobuttons.forEach(btn => {
				btn.addEventListener('change', function () {
					btn.checked = false;
					recognisebox.style.display = 'none';
					console.log('Sound count radio changed, resetting session storage flags and re-enabling button.');
				});
			});

			if (resultBox) {
				resultBox.style.display = 'none';
				console.log('Result box hidden on recognise button click.');
			}

			sessionStorage.removeItem('selectedSoundCount');
			sessionStorage.removeItem('SelectedQuality');
			sessionStorage.removeItem('selectedType');
			sessionStorage.setItem('hidebox', 'true'); // preserve intent if needed	

			recogniseform.submit();

			// After submit, uncheck all radio buttons and hide recognisebox
			//setTimeout(() => {
			//	radiobuttons.forEach(btn => {
			//		btn.checked = false;
			//	});

			//	if (recognisebox) {
			//		recognisebox.style.display = 'none';
			//		console.log('Recognisebox hidden after recognise submit.');
			//	}

			//	localStorage.removeItem('selectedSoundCount');
			//}, 100);
		});
	}
	else {
		console.warn('Recognise button or form element not found.');
	}

	// Обробник вибір кількості звуків
	radiobuttons.forEach(btn => {
		btn.addEventListener('change', function () {
			console.log('Sound count radio changed, showing recognise box.');
			if (recognisebox) recognisebox.style.display = 'flex';
		});
	});

	function setQualityOptions(options, preserveSelection = false) {
		if (!SelectedQuality) return;
		const prev = SelectedQuality.value;
		SelectedQuality.innerHTML = '';
		options.forEach(o => {
			const opt = document.createElement('option');
			opt.value = o;
			opt.textContent = o;
			SelectedQuality.appendChild(opt);
		});
		if (preserveSelection && options.includes(prev)) SelectedQuality.value = prev;
	}




	// When user changes SelectedType, update qualities client-side ONLY for intervals (SelectedCount == 2).
	// Do NOT alter or handle chords (SelectedCount != 2) on the client side — server continues to manage chord lists.
	if (selectedType) {
		selectedType.addEventListener('change', (e) => {
			const val = (e.target.value || '').trim();
			localStorage.setItem('selectedType', val);

			const countRadio = document.querySelector('input[name="SelectedCount"]:checked');
			const currentCount = countRadio ? countRadio.value : null;
			console.log(`selected type change handler is working, ${currentCount} sounds`)

			if (currentCount === 2) {
				const valLower = val.toLowerCase();

				const isPerfect = intervalPerfectKeys.some(k => k.toLowerCase() === valLower) || intervalPerfectUkr.includes(valLower);
				const isImperfect = intervalImperfectKeys.some(k => k.toLowerCase() === valLower) || intervalImperfectUkr.includes(valLower);

				if (isPerfect) {
					setQualityOptions(['чиста'], true);					
				}
				else if (isImperfect) {
					setQualityOptions(['велика', 'мала'], true);					
				}
			}
		});
	}

	// ОБРОБНИК ВИБОРУ АКОРДІВ
	chordSelector.forEach(input => {
		input.addEventListener('change', () => {
			localStorage.setItem('selectedSoundCount', input.value);
			selectform?.submit();
			console.log('selected sounds value:', input.value);
		});
	});


	document.addEventListener('change', (e) => {
		const t = e.target;
		if (!(t instanceof HTMLSelectElement)) return;
		if (t.id === 'SelectedQuality') {
			localStorage.setItem('SelectedQuality', t.value);
			// do not submit the form here; user must click "Розпізнати"
			return;
		}
		if (t.id === 'SelectedType') {
			// already handled above; ensure stored
			localStorage.setItem('selectedType', t.value);
			// do not submit the form here; user must click "Розпізнати"
		}
	});

	// Play button handler

	if (playBtn) {
		playBtn.addEventListener('click', async () => {
			// Hide result box when user plays the chord again
			const resultBox = document.getElementById('resultBox');
			if (resultBox) {
				resultBox.style.display = 'none';
				console.log('Result box hidden on play button click.');
			}

			try {
				// Preferred: play saved WAV file if available
				if (window.generatedFile && typeof window.generatedFile === 'string' && window.generatedFile.length > 0) {
					const audio = new Audio(window.generatedFile);
					audio.volume = 0.6;
					audio.play().catch(err => console.warn('Audio play failed:', err));
					return;
				}
				
				// Fallback: play synthesized tones from notes payload
				const seq = window.generatedChord;
				if (!seq) {
					console.warn('No generated chord available to play.');
					return;
				}
				const notesArray = Array.isArray(seq) ? seq : (Array.isArray(seq.notes) ? seq.notes : []);
				if (notesArray.length === 0) {
					console.warn('No note data to synthesize.');
					return;
				}

				const AudioCtx = window.AudioContext || window.webkitAudioContext;
				const ctx = new AudioCtx();
				const master = ctx.createGain();
				master.gain.value = 0.25;
				master.connect(ctx.destination);

				let maxMs = 0;
				for (const s of notesArray) if (s && typeof s.duration === 'number') maxMs = Math.max(maxMs, s.duration);
				const now = ctx.currentTime;
				const attack = 0.01;
				const decay = 0.15;
				const sustain = 0.75;
				const release = 0.2;
				const tones = notesArray.filter(s => s && s.frequency && s.frequency > 0);
				const toneCount = Math.max(1, tones.length);
				for (const s of notesArray) {
					const freq = Number(s.frequency) || 0;
					const durSec = (Number(s.duration) || 0) / 1000;
					if (freq <= 0) continue;
					const osc = ctx.createOscillator();
					osc.type = 'sine';
					osc.frequency.value = freq;
					const g = ctx.createGain();
					g.gain.value = 0.0;
					osc.connect(g);
					g.connect(master);
					const start = now;
					const sustainStart = start + attack + decay;
					const stopTime = start + durSec + release;
					g.gain.setValueAtTime(0.0, start);
					g.gain.linearRampToValueAtTime(1.0 / toneCount, start + attack);
					g.gain.linearRampToValueAtTime((sustain * (1.0 / toneCount)), sustainStart);
					g.gain.setValueAtTime((sustain * (1.0 / toneCount)), start + durSec);
					g.gain.linearRampToValueAtTime(0.0, stopTime);
					osc.start(start);
					osc.stop(stopTime + 0.01);
				}

				setTimeout(() => { try { ctx.close(); } catch (e) { } }, maxMs + 500);
			} catch (err) {
				console.error('Error playing chord:', err);
			}
		});
	}
	else {
		console.warn('no play button found')
	}

});

function logChord() {
	try {
		if (window.generatedChord) {
			console.log('Generated chord from server:', window.generatedChord);
			// If payload has type/quality, log a concise message
			if (window.generatedChord.type || window.generatedChord.quality) {
				console.log(`Server chord: type=${window.generatedChord.type}, quality=${window.generatedChord.quality}, count=${window.generatedChord.count}, root=${window.generatedChord.root}, file=${window.generatedChord.file}`);
			}
		}
		if (window.generatedFile) {
			console.log('Generated WAV file:', window.generatedFile);
		}
	} catch (e) {
		// ignore logging errors
	}
}
