document.addEventListener("DOMContentLoaded", function () {
    console.log("melodies.js starts.");

    const melodyNotation = document.querySelectorAll(".melody-notation");

    function applyMelodyLayout(card) {
        const padLeft = Math.min((window.innerWidth - 768), 100);
        console.log("window.innerWidth = ", window.innerWidth, "padLeft =", padLeft);


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

    melodyNotation.forEach(function (element, index) {

        const card = element.parentElement;

        const pattern = element.dataset.pattern;
        const id = "melody-notation-" + index;
        const numerator = element.dataset.numerator || 4;
        const denominator = element.dataset.denominator || 4;

        element.id = id;

        applyMelodyLayout(card);

        window.renderPatternString(
            pattern,
            id,
            null,
            numerator,      // numerator
            denominator,      // denominator
            1600,   // GENERALWIDTH
            90,     // HEIGHT
            0,      // TOPPADDING
            200,    // BARWIDTH
            40,     // CLEFZONE
            10,     // Xmargin
            512,    // RESPONSIVE_THRESHOLD
            0.7,    // BASESCALING
            0.6     // SCALINGFACTOR
        );
    });

    // Якщо користувач змінює ширину вікна
    window.addEventListener("resize", function () {
        melodyNotation.forEach(function (element) {
            applyMelodyLayout(element.parentElement);
        });
    });
});
