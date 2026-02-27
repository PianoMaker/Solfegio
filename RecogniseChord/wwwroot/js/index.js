document.addEventListener('DOMContentLoaded', function () { 

	console.log('Document loaded, initializing chord recognizer UI.');

	//ФОРМИ ТА ЕЛЕМЕНТИ

	const selectform = document.getElementById('selectform'); 		// форма для вибору кількості звуків під час відповіді користувача
	const maxsoundsform = document.getElementById('maxsoundsform');		// форма для вибору максимальної кількості звуків 
	const recogniseform = document.getElementById('recogniseform');		// форма для кнопки "Перевірити"

	const timbreform = document.getElementById('timbreform')			// форма встановлення тембру

	let radiobuttons = document.querySelectorAll('input[name="SelectedCount"]');

	if (!radiobuttons || radiobuttons.length === 0) {
		const fallbackIds = ['sound2', 'sound3', 'sound4', 'sound5'];
		const list = [];
		for (const id of fallbackIds) {
			const el = document.getElementById(id);
			if (el) list.push(el);
		}
		radiobuttons = list.length ? list : radiobuttons;
	}
	if (!radiobuttons || radiobuttons.length === 0) {
		console.warn('No sound count radio buttons found with name "SelectedCount" or fallback IDs.');
	} else {
		console.log(`Found ${radiobuttons.length} sound count radio buttons.`);
	}

	
	const recognisebox = document.getElementById('recognisebox');						// box containing recogniseform
	const recognisebutton = document.getElementById('recogniseButton');					// button to submit recogniseform
	const SelectedQuality = document.getElementById('SelectedQuality');
	const SelectedTimbre = document.getElementById('SelectedTimbre');
	const selectedType = document.getElementById('SelectedType');
	const playBtn = document.getElementById('playBtn');						//play button
	
	const resultBox = document.getElementById('resultBox');
	const maxsoundsinput = document.getElementById('maxsoundsinput');
	

	// Additional mappings (do not change existing triadTypes / ninthQualities)
	// These arrays are used only to recognize SelectedType values rendered in UI
	const intervalPerfectKeys = ['QUARTA', 'QUINTA', 'OCTAVA', 'PRIMA'];
	const intervalPerfectUkr = ['кварта', 'квінта', 'октава', 'прима'];
	const intervalImperfectKeys = ['SECUNDA', 'TERZIA', 'SEKSTA', 'SEPTYMA'];
	const intervalImperfectUkr = ['секунда', 'терція', 'секста', 'септима'];

	//=================================
	// Відновлюємо збережені значення з sessionStorage
	//=================================

	const savedSoundCount = sessionStorage.getItem('selectedSoundCount');  // кількість звуків
	console.log('Restoring session current sound count:', savedSoundCount);
	const savedTimbre = sessionStorage.getItem('savedTimbre')				//тембр
	console.log('restoring session timbre:', savedTimbre);
	const savedQuality = sessionStorage.getItem('SelectedQuality');		// якість інтервалу
	console.log('restoring session quality:', savedQuality);
	const savedType = sessionStorage.getItem('selectedType');			// тип акорду
	console.log('restoring session max sound count:', savedType);
	const maxsounds = sessionStorage.getItem('maxSounds');				// макс. кількість звуків
	console.log('restoring session max sound count:', maxsounds);


	//====================================
	// Застосовуємо відновлені значення
	// ===================================
	// поточна кількість звуків
	
	if (savedSoundCount) {
		radiobuttons.forEach(radio => {
			if (radio.value === savedSoundCount) {
				radio.checked = true;
				console.log('Restored sound count radio button:', radio.value);
			}
		});
	}

	//=========================
	// значення обраного тембру
	//=========================

	if (SelectedTimbre) {
		;
		if (savedTimbre)
			SelectedTimbre.value = savedTimbre;
		else SelectedTimbre.value = 'sin';
	}


	// =====================================================
	// значення прапорця приховування коробки розпізнавання
	// ====================================================
	const hidebox = sessionStorage.getItem('hidebox');
	if (hidebox === 'true') {
		if (recognisebox) {
			recognisebox.style.display = 'none';
			sessionStorage.removeItem('hidebox'); 		
		}
	}

	// ============================
	// значення якості і типу інтервалів
	// ============================
	if (savedQuality && SelectedQuality) SelectedQuality.value = savedQuality;


	if (savedType && selectedType) selectedType.value = savedType;

	//=================================
	//значення макс кількості звуків
	//=================================
	
	if (maxsounds && maxsoundsform) {
		maxsoundsform.value = maxsounds;
		maxsoundsinput.value = maxsounds;
	}
	const hasCheckedRadio = Array.from(radiobuttons).some(btn => btn.checked);


	//======================================
	//Обробник переммикання тембрів
	//=====================================
	SelectedTimbre.addEventListener('change', (e) => {
		if (!e.isTrusted) return;
		if (!timbreform) {
			console.warn('timbreform not found, cannot submit.');
			return;
		}
		const newVal = (e.target && e.target.value) ? e.target.value : (SelectedTimbre.value || '');
		console.debug(`timbre changed to ${newVal}`)
		sessionStorage.setItem('savedTimbre', newVal);
		// submit the timbre form so server can generate WAV with new timbre
		timbreform.submit();
	});



	//==============================================
	// КОРОБКА РОЗПІЗНАВАННЯ АКОРДІВ КОРИСТУВАЧЕМ
	// за замовчуванням прихована, показується після вибору кількості звуків
	//==============================================
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
	//=================================
	// КНОПКА "ПЕРЕВІРИТИ"
	// натискання скидає всі прапорці в сесії та надсилає форму розпізнавання
	//=================================
	if (recognisebutton) {
		recognisebutton.addEventListener('click', (e) => {
			if (!e.isTrusted) return;
			console.log('Recognise button clicked.');
			
			radiobuttons.forEach(btn => { 		
				btn.checked = false; 			
				console.log('reset radiobuttons'); 		
			});

			if (resultBox) {
				resultBox.style.display = 'none';
				console.log('hide result boxs');
			}

			if (recognisebox) {
				recognisebox.style.display = 'none';
				console.log('hide recognise box');
			}

			sessionStorage.removeItem('selectedSoundCount'); // 
			sessionStorage.removeItem('SelectedQuality');
			sessionStorage.removeItem('selectedType');
			sessionStorage.setItem('hidebox', 'true'); 
			console.warn('selectedSoundCount erased');

			recogniseform.submit();

		});
	}
	else {
		console.warn('Recognise button or form element not found.');
	}

	// =================================
	// Обробник вибору кількості звуків
	// натискання показує коробку розпізнавання та надсилає форму вибору
	// =================================
	if (radiobuttons) {
		radiobuttons.forEach(btn => {
			btn.addEventListener('change', function (e) {
				if (!e.isTrusted) return;
				console.log('Sound count radio changed to value: ' + btn.value); // updated logging to include value
				if (recognisebox) recognisebox.style.display = 'flex';
				sessionStorage.setItem('selectedSoundCount', btn.value);
				console.log('selected sounds value:', btn.value);
				selectform?.submit();
			});
		});
	}
	else {
		console.warn('No sound count radio buttons found.');
	}

	// Helper to set quality options
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

	// =================================
	// Обробник зміни типу інтервалу
	// автоматично налаштовує якість для інтервалів
	// =================================

	if (selectedType) {
		selectedType.addEventListener('change', (e) => {
			if (!e.isTrusted) return;
			const val = (e.target.value || '').trim();
			sessionStorage.setItem('selectedType', val);

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
	// =================================
	// Обробник зміни тембру
	// =================================

	if (SelectedTimbre) {
		SelectedTimbre.addEventListener('change', (e) => {
			if (!e.isTrusted) return;
			if (!timbreform) {
				console.warn('timbreform not found, cannot submit.');
				return;
			}
			const newVal = (e.target && e.target.value) ? e.target.value : (SelectedTimbre.value || '');
			console.debug(`timbre changed to ${newVal}`)
			sessionStorage.setItem('savedTimbre', newVal);
			// submit the timbre form so server can generate WAV with new timbre
			timbreform.submit();
		});
	}
	

	// =================================
	// Обробник зміни select-елементів
	// зберігає вибрані значення в sessionStorage
	// =================================
	document.addEventListener('change', (e) => {
		if (!e.isTrusted) return;
		const t = e.target;
		if (!(t instanceof HTMLSelectElement)) return;
		if (t.id === 'SelectedQuality') {
			sessionStorage.setItem('SelectedQuality', t.value);
			// do not submit the form here; user must click "Розпізнати"
			return;
		}
		if (t.id === 'SelectedType') {
			// already handled above; ensure stored
			sessionStorage.setItem('selectedType', t.value);
			// do not submit the form here; user must click "Розпізнати"
		}
	});


	


	// =================================
	// ОБРОБНИК КНОПКИ ВІДТВОРЕННЯ
	// натискання відтворює збережений WAV-файл або синтезує тони з payload
	// =================================

	if (playBtn) {
		playBtn.addEventListener('click', async () => {
			const resultBox = document.getElementById('resultBox');
			console.log('Play button clicked.');
			logChord();
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
					g.gain.linearRampToValueAtTime((sustain * 1.0 / toneCount), sustainStart);
					g.gain.setValueAtTime((sustain * 1.0 / toneCount), start + durSec);
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

	// =================================
	// ОБРОБНИК ВИБОРУ МАКСИМАЛЬНОЇ КІЛЬКОСТІ ЗВУКІВ
	// змінює значення в sessionStorage та надсилає форму
	// =================================

	if (maxsoundsinput) {
		maxsoundsinput.addEventListener('change', (e) => {
			if (!e.isTrusted) return;
			const val = e.target?.value;
			console.log('Max sounds input changed to:', val);
			sessionStorage.setItem('maxSounds', val);
			maxsoundsform?.submit();
		});
	}

	// expose helper to set timbre select from other scripts
	window.setTimbreSelect = function(val) {
		if (!val) return;
		if (!SelectedTimbre) return;
		let opt = Array.from(SelectedTimbre.options).find(o => o.value === val);
		if (!opt) {
			opt = new Option(val, val);
			SelectedTimbre.add(opt);
		}
		SelectedTimbre.value = val;
		sessionStorage.setItem('savedTimbre', val);
	};

});

// Helper to log generated chord and file info
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

