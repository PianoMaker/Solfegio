// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
	const chordSelector = document.querySelectorAll('#soundcount input[type=radio]');	//кількість звуків
	const selectform = document.getElementById('selectform');							//форма select
	const recogniseform = document.getElementById('recogniseform');						//форма recognise

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

	// save and submit on radio change -> posts SelectedCount to OnPostSelect
	chordSelector.forEach(input => {
		input.addEventListener('change', () => {
			localStorage.setItem('selectedSoundCount', input.value);
			selectform?.submit();
			console.log('selected sounds value:', input.value);
		});
	});

	// Submit explicit forms by id for chord/type changes
	document.addEventListener('change', (e) => {
		const t = e.target;
		if (!(t instanceof HTMLSelectElement)) return;
		if (t.id === 'SelectedChord') {
			localStorage.setItem('selectedChord', t.value);
			recogniseform?.submit();
			return;
		}
		if (t.id === 'SelectedType') {
			localStorage.setItem('selectedType', t.value);			
			recogniseform?.submit();
		}
	});

	// Play button handler (plays chord generated on server and embedded as window.generatedChord)
	const playBtn = document.getElementById('playButton');
	if (playBtn) {
		playBtn.addEventListener('click', async () => {
			try {
				const seq = window.generatedChord;
				console.log('generatedChord:', seq);
				if (!seq || !Array.isArray(seq) || seq.length === 0) {
					console.warn('No generated chord available to play.');
					return;
				}

				// Create		ontext
				const AudioCtx = window.AudioContext || window.webkitAudioContext;
				const ctx = new AudioCtx();

				// master gain
				const master = ctx.createGain();
				master.gain.value =0.25; // overall volume
				master.connect(ctx.destination);

				// compute max duration in seconds
				let maxMs =0;
				for (const s of seq) if (s && typeof s.duration === 'number') maxMs = Math.max(maxMs, s.duration);
				const now = ctx.currentTime;

				// ADSR settings (seconds)
				const attack =0.01;
				const decay =0.15;
				const sustain =0.75;
				const release =0.2;

				// Count tones (exclude rests)
				const tones = seq.filter(s => s.frequency && s.frequency >0);
				const toneCount = Math.max(1, tones.length);

				// create oscillators for each tone
				const oscillators = [];
				for (const s of seq) {
					const freq = Number(s.frequency) ||0;
					const durSec = (Number(s.duration) ||0) /1000;
					if (freq <=0) continue; // rest

					const osc = ctx.createOscillator();
					osc.type = 'sine';
					osc.frequency.value = freq;

					const g = ctx.createGain();
					// scale per tone to avoid clipping
					g.gain.value =0.0;
					osc.connect(g);
					g.connect(master);

					const start = now;
					const sustainStart = start + attack + decay;
					const stopTime = start + durSec + release;

					// ADSR envelope
					g.gain.setValueAtTime(0.0, start);
					g.gain.linearRampToValueAtTime(1.0 / toneCount, start + attack);
					g.gain.linearRampToValueAtTime((sustain * (1.0 / toneCount)), sustainStart);
					// release schedule
					g.gain.setValueAtTime((sustain * (1.0 / toneCount)), start + durSec);
					g.gain.linearRampToValueAtTime(0.0, stopTime);

					osc.start(start);
					osc.stop(stopTime +0.01);

					oscillators.push(osc);
				}

				// auto-close AudioContext after playback
				setTimeout(() => {
					try { ctx.close(); } catch (e) { /* ignore */ }
				}, maxMs +500);

			} catch (err) {
				console.error('Error playing chord:', err);
			}
		});
	}

});

// Run in browser console on the page
const a = new (window.AudioContext || window.webkitAudioContext)();
const o = a.createOscillator();
const g = a.createGain();
o.type = 'sine';
o.frequency.value = 440;
g.gain.value = 0.1;
o.connect(g);
g.connect(a.destination);
o.start();
setTimeout(()=>{ o.stop(); a.close(); }, 600);

