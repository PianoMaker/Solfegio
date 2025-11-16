// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
	const chordSelector = document.querySelectorAll('#soundcount input[type=radio]');
	const selectform = document.getElementById('selectform');
	const recogniseform = document.getElementById('recogniseform');

	const selectedChord = document.getElementById('SelectedChord');
	const selectedType = document.getElementById('SelectedType');

	// restore saved radio
	const savedSoundCount = localStorage.getItem('selectedSoundCount');
	if (savedSoundCount) {
		chordSelector.forEach(radio => {
			if (radio.value === savedSoundCount) radio.checked = true;
		});
	}
	// restore saved selects
	const savedChord = localStorage.getItem('selectedChord');
	if (savedChord && selectedChord) selectedChord.value = savedChord;
	const savedType = localStorage.getItem('selectedType');
	if (savedType && selectedType) selectedType.value = savedType;

	// Log generated chord metadata from server (notes, type, quality, file...)
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

	function setQualityOptions(options, preserveSelection = false) {
		if (!selectedChord) return;
		const prev = selectedChord.value;
		selectedChord.innerHTML = '';
		options.forEach(o => {
			const opt = document.createElement('option');
			opt.value = o;
			opt.textContent = o;
			selectedChord.appendChild(opt);
		});
		if (preserveSelection && options.includes(prev)) selectedChord.value = prev;
	}

	// Interval and triad type sets
	const triadTypes = ['TRI', 'SEXT', 'QSEXT'];
	const intervalTypesPerfect = ['PRIMA', 'QUARTA', 'QUINTA', 'OCTAVA'];
	const intervalTypesImperfect = ['SECUNDA', 'TERZIA', 'SEKSTA', 'SEPTYMA'];

	// When user changes SelectedType, update qualities client-side for triad and interval types
	if (selectedType) {
		selectedType.addEventListener('change', (e) => {
			const val = e.target.value;
			localStorage.setItem('selectedType', val);

			// Triad types -> triad qualities
			if (triadTypes.includes(val)) {
				setQualityOptions(['MAJ', 'MIN', 'AUG', 'DIM'], true);
				return; // do not submit
			}

			// Perfect intervals -> PERFECT/AUG/DIM
			if (intervalTypesPerfect.includes(val)) {
				setQualityOptions(['PERFECT', 'AUG', 'DIM'], true);
				return; // do not submit
			}

			// Imperfect intervals -> MAJ/MIN/AUG/DIM (show major/minor first)
			if (intervalTypesImperfect.includes(val)) {
				setQualityOptions(['MAJ', 'MIN', 'AUG', 'DIM'], true);
				return; // do not submit
			}

			// For other types leave quality list as-is; server will supply correct list when needed
		});
	}

	// save and submit on radio change -> posts SelectedCount to OnPostSelect
	chordSelector.forEach(input => {
		input.addEventListener('change', () => {
			localStorage.setItem('selectedSoundCount', input.value);

			// Do not perform any playback here. Only submit the select form so server updates lists.
			selectform?.submit();
			console.log('selected sounds value:', input.value);
		});
	});

	// Do NOT auto-submit recognise form when user changes selects.
	// Only update local storage so the user's guess is preserved until they click "Розпізнати".
	document.addEventListener('change', (e) => {
		const t = e.target;
		if (!(t instanceof HTMLSelectElement)) return;
		if (t.id === 'SelectedChord') {
			localStorage.setItem('selectedChord', t.value);
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
	const playBtn = document.getElementById('playButton') || document.getElementById('playBtn');
	if (playBtn) {
		playBtn.addEventListener('click', async () => {
			try {
				// Preferred: play saved WAV file if available
				if (window.generatedFile && typeof window.generatedFile === 'string' && window.generatedFile.length >0) {
					const audio = new Audio(window.generatedFile);
					audio.volume =0.8;
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
				if (notesArray.length ===0) {
					console.warn('No note data to synthesize.');
					return;
				}

				const AudioCtx = window.AudioContext || window.webkitAudioContext;
				const ctx = new AudioCtx();
				const master = ctx.createGain();
				master.gain.value =0.25;
				master.connect(ctx.destination);

				let maxMs =0;
				for (const s of notesArray) if (s && typeof s.duration === 'number') maxMs = Math.max(maxMs, s.duration);
				const now = ctx.currentTime;
				const attack =0.01;
				const decay =0.15;
				const sustain =0.75;
				const release =0.2;
				const tones = notesArray.filter(s => s && s.frequency && s.frequency >0);
				const toneCount = Math.max(1, tones.length);
				for (const s of notesArray) {
					const freq = Number(s.frequency) ||0;
					const durSec = (Number(s.duration) ||0) /1000;
					if (freq <=0) continue;
					const osc = ctx.createOscillator();
					osc.type = 'sine';
					osc.frequency.value = freq;
					const g = ctx.createGain();
					g.gain.value =0.0;
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
					osc.stop(stopTime +0.01);
				}

				setTimeout(() => { try { ctx.close(); } catch (e) {} }, maxMs +500);
			} catch (err) {
				console.error('Error playing chord:', err);
			}
		});
	}

});

