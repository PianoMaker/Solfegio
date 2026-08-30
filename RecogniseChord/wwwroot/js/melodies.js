document.addEventListener("DOMContentLoaded", function () {
    console.log("melodies.js starts.");

    const melodyNotation = document.querySelectorAll(".melody-notation");


    function applyMelodyLayout(card) {
        const padLeft = Math.min((window.innerWidth - 768), 100);
        console.log(
            "window.innerWidth = ",
            window.innerWidth,
            "padLeft =",
            padLeft
        );


        if (window.innerWidth <= 580) {

            // Вузький екран
            card.style.display = "grid";
            card.style.gridTemplateColumns = "1fr";
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
            card.style.gridTemplateColumns = "211px 600px";
            card.style.paddingLeft = `${padLeft}px`;
            card.style.alignItems = "center";
            card.style.overflowX = "hidden";

            const info = card.querySelector(".melody-info");
            if (info) {
                info.style.width = "211px";
            }

            const notation = card.querySelector(".melody-notation");
            if (notation) {
                notation.style.width = "800px";
            }
        }
    }


    // ============================================================
    // РЕНДЕРИНГ НОТ
    // ============================================================

    melodyNotation.forEach(function (element, index) {

        console.log(
            "time signature from dataset = ",
            element.dataset.numerator,
            "/",
            element.dataset.denominator
        );

        const card = element.parentElement;

        const pattern = element.dataset.pattern;

        const id = "melody-notation-" + index;

        const numerator =
            element.dataset.numerator || 4;

        const denominator =
            element.dataset.denominator || 4;

        const barwidth =
            element.dataset.barwidth || 160;

        const key =
            element.dataset.key || "C";


        element.id = id;

        applyMelodyLayout(card);


        // --------------------------------------------------------
        // Якщо немає data-pattern — нічого не рендеримо
        // --------------------------------------------------------

        if (!pattern) {
            console.log(
                "No data-pattern for:",
                card
            );
            return;
        }


        // --------------------------------------------------------
        // Рендер нотного запису
        // --------------------------------------------------------

        window.renderPatternString(
            pattern,
            id,
            null,
            numerator,       // numerator
            denominator,     // denominator
            1600,            // GENERALWIDTH
            90,              // HEIGHT
            0,               // TOPPADDING
            barwidth,        // BARWIDTH
            40,              // CLEFZONE
            10,              // Xmargin
            512,              // RESPONSIVE_THRESHOLD
            0.7,              // BASESCALING
            0.6,              // SCALINGFACTOR
            key               // KeySignature
        );
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

    window.addEventListener("resize", function () {

        melodyNotation.forEach(function (element) {

            applyMelodyLayout(
                element.parentElement
            );

        });

    });

});
