document.addEventListener("DOMContentLoaded", function () {
    console.log("melodies.js starts.");

    document.querySelectorAll(".interval-toggle").forEach(function (button) {
        button.addEventListener("click", function () {
            const section = button.closest(".interval-section");
            if (!section) return;

            const isOpen = section.classList.toggle("is-open");
            button.setAttribute("aria-expanded", String(isOpen));

            if (isOpen) {
                window.requestAnimationFrame(function () {
                    section.querySelectorAll(".melody-notation[data-pattern]").forEach(function (notation) {
                        applyMelodyLayout(notation.parentElement);
                        renderMelodyNotation(notation);
                    });
                });
            }
        });
    });

    const melodyNotation = document.querySelectorAll(".melody-notation");


    function applyMelodyLayout(card) {
        const padLeft = 12;
        console.log(
            "window.innerWidth = ",
            window.innerWidth,
            "padLeft =",
            padLeft
        );


        if (window.innerWidth <= 580) {

            // Вузький екран
            card.style.display = "grid";
            card.style.gridTemplateColumns = "minmax(0, 1fr)";
            card.style.paddingLeft = "0";
            card.style.overflowX = "hidden";

            const info = card.querySelector(".melody-info");
            if (info) {
                info.style.width = "100%";
            }

            const notation = card.querySelector(".melody-notation");
            if (notation) {
                notation.style.width = "100%";
            }

        } else {

            // Широкий екран
            card.style.display = "grid";
            card.style.gridTemplateColumns = "minmax(0, 211px) minmax(0, 1fr)";
            card.style.paddingLeft = `${padLeft}px`;
            card.style.alignItems = "center";
            card.style.overflowX = "hidden";

            const info = card.querySelector(".melody-info");
            if (info) {
                info.style.width = "211px";
            }

            const notation = card.querySelector(".melody-notation");
            if (notation) {
                notation.style.width = "100%";
            }
        }
    }


    // ============================================================
    // РЕНДЕРИНГ НОТ
    // ============================================================

    function renderMelodyNotation(element) {
        const pattern = element.dataset.pattern;
        if (!pattern) return;
        if (typeof window.renderPatternString !== "function") {
            console.error("patternRenderer.js did not load; cannot render notation.");
            return;
        }

        const notationWidth = element.clientWidth || element.parentElement?.clientWidth || 320;
        if (!notationWidth) return;

        window.renderPatternString(
            pattern,
            element.id,
            null,
            element.dataset.numerator || 4,
            element.dataset.denominator || 4,
            notationWidth,
            90,
            0,
            element.dataset.barwidth || 160,
            40,
            10,
            512,
            0.85,
            0.75,
            element.dataset.key || "C",
            true
        );
    }

    melodyNotation.forEach(function (element, index) {
        element.id = "melody-notation-" + index;
        applyMelodyLayout(element.parentElement);

        if (element.closest(".interval-section")?.classList.contains("is-open")) {
            renderMelodyNotation(element);
        }
    });


    // ============================================================
    // ГОТОВІ АУДІОФАЙЛИ
    //
    // Наприклад:
    //
    // <div class="melody-card"
    //      data-audio="/sound/s8crryce.mp3">
    //
    // Ці картки не мають data-pattern,
    // тому обробляються окремо.
    // ============================================================

    let activeAudio = null;

    window.stopMelodiesAudioFile = function () {
        if (activeAudio) {
            activeAudio.pause();
            activeAudio.currentTime = 0;
            activeAudio = null;
        }
    };


    const audioCards =
        document.querySelectorAll(".melody-card[data-audio]");


    audioCards.forEach(function (card) {

        console.log(
            "Audio card found:",
            card.dataset.audio
        );


        card.addEventListener("click", function () {

            const audioFile =
                card.dataset.audio;


            console.log(
                "AUDIO CARD CLICK:",
                audioFile
            );


            if (!audioFile) {
                console.warn(
                    "No data-audio on card"
                );
                return;
            }


            // ----------------------------------------------------
            // Якщо щось уже грає — зупиняємо
            // ----------------------------------------------------

            if (activeAudio) {

                activeAudio.pause();

                activeAudio.currentTime = 0;

                activeAudio = null;
            }


            // ----------------------------------------------------
            // Створюємо Audio
            // ----------------------------------------------------

            if (typeof window.stopMelodyPattern === "function") window.stopMelodyPattern();

            const audio =
                new Audio(audioFile);


            activeAudio = audio;


            // ----------------------------------------------------
            // Діагностика
            // ----------------------------------------------------

            audio.addEventListener(
                "canplay",
                function () {

                    console.log(
                        "AUDIO CAN PLAY:",
                        audioFile
                    );
                }
            );


            audio.addEventListener(
                "ended",
                function () {

                    console.log(
                        "AUDIO ENDED:",
                        audioFile
                    );

                    if (activeAudio === audio) {
                        activeAudio = null;
                    }
                }
            );


            audio.addEventListener(
                "error",
                function (event) {

                    console.error(
                        "AUDIO ERROR:",
                        event
                    );

                    console.error(
                        "Audio URL:",
                        audioFile
                    );

                    if (activeAudio === audio) {
                        activeAudio = null;
                    }
                }
            );


            // ----------------------------------------------------
            // Запуск
            // ----------------------------------------------------

            audio.play()
                .then(function () {

                    console.log(
                        "AUDIO PLAYING:",
                        audioFile
                    );

                })
                .catch(function (error) {

                    console.error(
                        "AUDIO PLAY FAILED:",
                        error
                    );
                });

        });

    });


    // ============================================================
    // RESIZE
    // ============================================================

    // Add transport controls while keeping the existing audio implementations.
    document.querySelectorAll(".melody-card").forEach(function (card) {
        const notation = card.querySelector(".melody-notation[data-pattern]");
        if (!notation && !card.dataset.audio) return;

        const info = card.querySelector(".melody-info");
        if (!info) return;

        const controls = document.createElement("div");
        controls.className = "melody-playback-controls";

        const playButton = document.createElement("button");
        playButton.type = "button";
        playButton.className = "melody-play-button";
        playButton.textContent = "▶";
        playButton.setAttribute("aria-label", "Відтворити мелодію");
        playButton.title = "Відтворити";

        const stopButton = document.createElement("button");
        stopButton.type = "button";
        stopButton.className = "melody-stop-button";
        stopButton.textContent = "■";
        stopButton.setAttribute("aria-label", "Зупинити відтворення");
        stopButton.title = "Зупинити";

        playButton.addEventListener("click", function (event) {
            event.stopPropagation();

            if (notation) {
                if (activeAudio) {
                    activeAudio.pause();
                    activeAudio.currentTime = 0;
                    activeAudio = null;
                }
                if (typeof window.playMelodyPattern === "function") {
                    window.playMelodyPattern(notation.dataset.pattern, 100);
                }
            } else {
                card.click();
            }
        });

        stopButton.addEventListener("click", function (event) {
            event.stopPropagation();

            if (activeAudio) {
                activeAudio.pause();
                activeAudio.currentTime = 0;
                activeAudio = null;
            }
            if (typeof window.stopMelodyPattern === "function") {
                window.stopMelodyPattern();
            }
        });

        controls.append(playButton, stopButton);
        info.appendChild(controls);
    });


    window.addEventListener("resize", function () {

        melodyNotation.forEach(function (element) {

            applyMelodyLayout(element.parentElement);
            if (element.closest(".interval-section")?.classList.contains("is-open")) {
                renderMelodyNotation(element);
            }
        });

    });

});
