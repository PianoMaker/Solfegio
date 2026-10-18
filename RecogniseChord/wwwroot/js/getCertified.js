document.addEventListener('DOMContentLoaded', () => {
    const root = document.getElementById('certifiedQuiz');
    if (!root) return;

    const levelPicker = document.getElementById('quizLevelPicker');
    const panel = document.getElementById('quizPanel');
    const audio = document.getElementById('quizAudio');
    const progress = document.getElementById('quizProgress');
    const timer = document.getElementById('quizTimer');
    const correctCount = document.getElementById('quizCorrect');
    const incorrectCount = document.getElementById('quizIncorrect');
    const countSelect = document.getElementById('quizCount');
    const typeSelect = document.getElementById('quizType');
    const qualitySelect = document.getElementById('quizQuality');
    const submitButton = document.getElementById('quizSubmit');
    const nextButton = document.getElementById('quizNext');
    const feedback = document.getElementById('quizFeedback');
    const outcome = document.getElementById('quizOutcome');
    const certificateForm = document.getElementById('certificateForm');
    const googleLoginPrompt = document.getElementById('googleLoginPrompt');
    const certificateName = document.getElementById('certificateName');
    const certificatePreview = document.getElementById('certificatePreview');
    const certificateRecipient = document.getElementById('certificateRecipient');
    const certificateLevel = document.getElementById('certificateLevel');
    const certificateScore = document.getElementById('certificateScore');
    const certificateDate = document.getElementById('certificateDate');
    const certificateNumber = document.getElementById('certificateNumber');
    const certificateQr = document.getElementById('certificateQr');
    const certificateVerificationLink = document.getElementById('certificateVerificationLink');
    const verifyForm = document.getElementById('verifyCertificateForm');
    const lookupNumber = document.getElementById('certificateLookupNumber');
    const lookupResult = document.getElementById('certificateLookupResult');
    let finalResult = null;
    const isAuthenticated = root.dataset.authenticated === 'true';
    const token = document.querySelector('#quizTokenForm input[name="__RequestVerificationToken"]')?.value;
    const text = root.dataset;

    function format(template, ...values) {
        return values.reduce((result, value, index) => result.replace(`{${index}}`, value), template);
    }

    let level = '';
    let secondsLeft = 0;
    let timerId = null;
    let answered = false;

    function endpoint(base, params = {}) {
        const url = new URL(base, window.location.href);
        for (const [key, value] of Object.entries(params)) url.searchParams.set(key, value);
        return url;
    }

    function fillSelect(select, items, placeholder) {
        select.replaceChildren(new Option(placeholder, ''));
        for (const item of items) select.add(new Option(item.label, item.value));
    }

    countSelect.addEventListener('keydown', event => {
        if (!/^[2-5]$/.test(event.key)) return;
        const option = Array.from(countSelect.options).find(item => item.value === event.key);
        if (!option) return;
        event.preventDefault();
        countSelect.value = option.value;
        countSelect.dispatchEvent(new Event('change', { bubbles: true }));
    });

    typeSelect.addEventListener('keydown', event => {
        if (countSelect.value !== '2' || !/^[1-8]$/.test(event.key)) return;
        const intervalOptions = Array.from(typeSelect.options).filter(option => option.value);
        const option = intervalOptions[Number(event.key) - 1];
        if (!option) return;
        event.preventDefault();
        typeSelect.value = option.value;
        typeSelect.dispatchEvent(new Event('change', { bubbles: true }));
    });

    async function updateAnswerOptions() {
        const response = await fetch(endpoint(root.dataset.optionsUrl, {
            count: countSelect.value || (level === 'advanced' ? 3 : 2),
            type: typeSelect.value
        }));
        if (!response.ok) throw new Error(text.errorOptions);
        const options = await response.json();
        const oldType = typeSelect.value;
        fillSelect(typeSelect, options.types, text.placeholderType);
        if (options.types.some(option => option.value === oldType)) typeSelect.value = oldType;
        fillSelect(qualitySelect, options.qualities, text.placeholderQuality);
    }

    async function loadChord(reset = false) {
        answered = false;
        feedback.textContent = '';
        outcome.style.display = 'none';
        certificateForm.style.display = 'none';
        certificatePreview.style.display = 'none';
        submitButton.disabled = false;
        submitButton.style.display = '';
        nextButton.style.display = 'none';
        countSelect.disabled = false;
        typeSelect.disabled = false;
        qualitySelect.disabled = false;

        const result = await fetch(endpoint(root.dataset.nextUrl, { level, reset }));
        if (!result.ok) throw new Error(text.errorNext);
        const chord = await result.json();

        progress.textContent = format(text.labelChord, chord.question, chord.total);
        audio.src = new URL(chord.audioUrl, window.location.href).href;
        audio.load();

        const minCount = level === 'advanced' ? 3 : 2;
        const maxCount = level === 'easy' ? 3 : (level === 'base' ? 4 : 5);
        fillSelect(countSelect, Array.from({ length: maxCount - minCount + 1 }, (_, i) => {
            const value = String(minCount + i);
            return { value, label: value };
        }), text.placeholderCount);
        countSelect.value = String(minCount);
        typeSelect.value = '';
        await updateAnswerOptions();

        secondsLeft = chord.timeLimitSeconds;
        timer.textContent = `${secondsLeft} ${text.seconds}`;
        clearInterval(timerId);
        timerId = setInterval(() => {
            secondsLeft--;
            timer.textContent = `${Math.max(0, secondsLeft)} ${text.seconds}`;
            if (secondsLeft <= 0) submitAnswer(true).catch(showError);
        }, 1000);
    }

    async function submitAnswer(timedOut = false) {
        if (answered) return;
        answered = true;
        clearInterval(timerId);
        submitButton.disabled = true;

        const body = new URLSearchParams({
            count: countSelect.value || '0',
            type: timedOut ? '' : typeSelect.value,
            quality: timedOut ? '' : qualitySelect.value,
            timedOut: String(timedOut)
        });
        const response = await fetch(root.dataset.answerUrl, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8',
                'RequestVerificationToken': token
            },
            body
        });
        if (!response.ok) throw new Error(text.errorAnswer);
        const result = await response.json();

        correctCount.textContent = result.correctCount;
        incorrectCount.textContent = result.incorrectCount;
        feedback.textContent = `${result.correct ? text.feedbackCorrect : (result.timedOut ? text.feedbackTimeout : text.feedbackIncorrect)} ${text.feedbackAnswer} ${result.answer}.`;
        feedback.className = result.correct ? 'mt-3 text-success' : 'mt-3 text-danger';
        countSelect.disabled = true;
        typeSelect.disabled = true;
        qualitySelect.disabled = true;

        if (result.complete) {
            submitButton.style.display = 'none';
            nextButton.style.display = 'none';
            progress.textContent = format(text.finished, result.total);
            finalResult = result;
            outcome.textContent = result.passed ? text.outcomePassed : text.outcomeFailed;
            outcome.className = result.passed ? 'mt-4 text-success' : 'mt-4 text-danger';
            outcome.style.display = '';
            if (result.passed) {
                googleLoginPrompt.style.display = isAuthenticated ? 'none' : '';
                certificateForm.style.display = isAuthenticated ? '' : 'none';
                if (isAuthenticated && root.dataset.displayName) certificateName.value = root.dataset.displayName;
            }
            levelPicker.style.display = '';
        } else {
            nextButton.style.display = '';
        }
    }

    function showError(error) {
        clearInterval(timerId);
        feedback.textContent = error.message || String(error);
        feedback.className = 'mt-3 text-danger';
    }

    countSelect.addEventListener('change', () => updateAnswerOptions().catch(showError));
    typeSelect.addEventListener('change', () => updateAnswerOptions().catch(showError));
    submitButton.addEventListener('click', () => submitAnswer(false).catch(showError));
    nextButton.addEventListener('click', () => loadChord().catch(showError));

    document.getElementById('generateCertificate').addEventListener('click', async () => {
        const name = certificateName.value.trim();
        if (!name) {
            certificateName.reportValidity();
            return;
        }
        if (!finalResult?.passed) return;

        const button = document.getElementById('generateCertificate');
        button.disabled = true;
        try {
            const response = await fetch(root.dataset.issueUrl, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8',
                    'RequestVerificationToken': token
                },
                body: new URLSearchParams()
            });
            if (!response.ok) throw new Error(text.errorIssue);
            const issued = await response.json();
            finalResult = { ...finalResult, passed: true, correctCount: issued.correctCount, total: issued.total };
            certificateRecipient.textContent = issued.recipientName;
            certificateLevel.textContent = issued.level === 'easy' ? text.levelEasy : (issued.level === 'base' ? text.levelBase : text.levelAdvanced);
            certificateScore.textContent = `${issued.correctCount} / ${issued.total}`;
            certificateDate.textContent = new Intl.DateTimeFormat(text.culture.startsWith('en') ? 'en-US' : 'uk-UA', { dateStyle: 'long' }).format(new Date(issued.issuedAtUtc));
            certificateNumber.textContent = issued.number;
            certificateQr.src = issued.qrUrl;
            certificateVerificationLink.href = issued.verificationUrl;
            certificateVerificationLink.textContent = text.verifyTitle;
            certificatePreview.style.display = '';
        } catch (error) {
            showError(error);
        } finally {
            button.disabled = false;
        }
    });

    verifyForm.addEventListener('submit', async event => {
        event.preventDefault();
        if (!verifyForm.reportValidity()) return;
        lookupResult.replaceChildren();
        try {
            const url = new URL(root.dataset.lookupUrl, window.location.href);
            url.searchParams.set('number', lookupNumber.value.trim());
            const response = await fetch(url, { credentials: 'same-origin' });
            if (!response.ok) throw new Error(text.verifyError);
            const result = await response.json();
            const message = document.createElement('p');
            if (!result.valid) {
                message.className = 'alert alert-danger';
                message.textContent = text.verifyInvalid;
            } else {
                message.className = 'alert alert-success';
                const date = new Intl.DateTimeFormat(text.culture.startsWith('en') ? 'en-US' : 'uk-UA', { dateStyle: 'long' }).format(new Date(result.issuedAtUtc));
                message.textContent = `${text.verifyName}: ${result.recipientName} · ${text.verifyLevel}: ${result.level === 'easy' ? text.levelEasy : (result.level === 'base' ? text.levelBase : text.levelAdvanced)} · ${text.verifyScore}: ${result.correctCount} / ${result.total} · ${text.verifyDate}: ${date}`;
            }
            lookupResult.append(message);
        } catch {
            const message = document.createElement('p');
            message.className = 'alert alert-danger';
            message.textContent = text.verifyError;
            lookupResult.append(message);
        }
    });

    document.getElementById('googleLoginButton').addEventListener('click', () => {
        if (finalResult?.passed) sessionStorage.setItem('pendingCertificateResult', JSON.stringify({ level, result: finalResult }));
    });

    if (isAuthenticated) {
        certificateName.value = root.dataset.displayName || '';
        const saved = sessionStorage.getItem('pendingCertificateResult');
        if (saved) {
            sessionStorage.removeItem('pendingCertificateResult');
            try {
                const pending = JSON.parse(saved);
                if (pending.result?.passed) {
                    level = pending.level;
                    finalResult = pending.result;
                    panel.style.display = '';
                    levelPicker.style.display = 'none';
                    progress.textContent = format(text.finished, finalResult.total);
                    outcome.textContent = text.outcomePassed;
                    outcome.className = 'mt-4 text-success';
                    outcome.style.display = '';
                    correctCount.textContent = finalResult.correctCount;
                    incorrectCount.textContent = finalResult.incorrectCount;
                    certificateForm.style.display = '';
                }
            } catch { sessionStorage.removeItem('pendingCertificateResult'); }
        }
    }

    document.getElementById('printCertificate').addEventListener('click', () => {
        document.body.classList.add('certified-print-mode');
        window.print();
    });
    window.addEventListener('afterprint', () => document.body.classList.remove('certified-print-mode'));

    levelPicker.querySelectorAll('[data-level]').forEach(button => {
        button.addEventListener('click', async () => {
            level = button.dataset.level;
            correctCount.textContent = '0';
            incorrectCount.textContent = '0';
            levelPicker.style.display = 'none';
            panel.style.display = '';
            try { await loadChord(true); }
            catch (error) { showError(error); levelPicker.style.display = ''; }
        });
    });
});
