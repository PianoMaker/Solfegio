// Script for Create.cshtml play button and form handling
// submit count form on radio change
document.querySelectorAll('#countForm input[type=radio]').forEach(r => {
 r.addEventListener('change', () => {
 document.getElementById('countForm').submit();
 });
});

document.addEventListener('DOMContentLoaded', () => {
 const btn = document.getElementById('playBtn');
 const audio = document.getElementById('chordAudio');
 const hidden = document.getElementById('generatedRel');

 function refreshAudio() {
 const rel = hidden ? hidden.value : '';
 console.log('refreshAudio generatedRel=', rel);
 if (rel && rel.trim() !== '') {
 audio.src = rel;
 audio.removeAttribute('aria-hidden');
 // enable only when audio can play
 btn.disabled = true;
 const onCan = function () {
 btn.disabled = false;
 audio.removeEventListener('canplay', onCan);
 };
 audio.addEventListener('canplay', onCan);
 audio.addEventListener('error', function onErr(e) {
 console.error('Audio load error', e);
 btn.disabled = true;
 });
 try { audio.load(); } catch(e) { console.error(e); }
 } else {
 audio.removeAttribute('src');
 btn.disabled = true;
 }
 }

 refreshAudio();

 if (btn) {
 btn.addEventListener('click', () => {
 try {
 audio.pause();
 audio.currentTime =0;
 audio.play().catch(e => console.error('audio play rejected', e));
 } catch(e) { console.error('play error', e); }
 });
 }

});
