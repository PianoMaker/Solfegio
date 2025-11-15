// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
	const chordSelector = document.querySelectorAll('#soundcount input[type=radio]');	//кількість звуків
	const selectform = document.getElementById('selectform');							//форма select

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

	// Delegate change for chord/type selects and submit their containing form (recognise)
	document.addEventListener('change', (e) => {
		const t = e.target;
		if (!(t instanceof HTMLSelectElement)) return;
		if (t.id === 'SelectedChord') {
			localStorage.setItem('selectedChord', t.value);
			t.closest('form')?.submit();
			return;
		}
		if (t.id === 'SelectedType') {
			localStorage.setItem('selectedType', t.value);
			t.closest('form')?.submit();
		}
	});
});

