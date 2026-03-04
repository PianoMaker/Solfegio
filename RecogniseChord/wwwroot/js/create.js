// Script for Create.cshtml play button and form handling
// submit count form on radio change
document.querySelectorAll('#countForm input[type=radio]').forEach(r => {
	r.addEventListener('change', () => {
		document.getElementById('countForm').submit();
	});
});

document.addEventListener('DOMContentLoaded', function () {

	console.log('create.js loaded');
	const playBtn = document.getElementById('playBtn');
	const audio = document.getElementById('chordAudio');
	const genInput = document.getElementById('generatedRel');

	if (!playBtn || !audio || !genInput) {
		console.warn('create.js: missing expected DOM elements');
		return;
	}


	// initial setup
	try {
		setupFromGenerated();
	} catch (err) {
		console.error('create.js setup error:', err);
	}

	// play/pause behaviour
	if (playBtn) {
		console.debug('playbtn eventlistener is loading')
		playBtn.disabled = false;
		playBtn.addEventListener('click', function () {
			console.debug('playbtn clicked')
			if (!audio.src) {
				console.warn('create.js: no audio src set');
				return;
			}
			if (audio.paused) {
				audio.play().catch(e => console.error('Audio play failed:', e));
				playBtn.innerText = 'Зупинити';
			} else {
				audio.pause();
				playBtn.innerText = 'Відтворити';
			}
		});
	}
	else {
		console.warn('no playBtn')
	}

	audio.addEventListener('ended', function () {
		playBtn.innerText = 'Відтворити';
	});


	function normalizeUrl(rel) {
		if (!rel) return '';
		// remove leading ~/ or extra slashes, ensure one leading slash
		rel = rel.replace(/^~\//, '').replace(/^\/+/, '');
		return '/' + rel;
	}

	function setupFromGenerated() {
		const rel = (genInput.value || '').trim();
		if (!rel) {
			playBtn.disabled = true;
			audio.removeAttribute('src');
			return;
		}

		const url = normalizeUrl(rel);
		audio.src = url;
		audio.load();
		playBtn.disabled = false;
	}

});
