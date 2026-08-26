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
                notation.style.width = "600px";
            }
        }
    }

    melodyNotation.forEach(function (element, index) {

        const card = element.parentElement;

        const pattern = element.dataset.pattern;
        const id = "melody-notation-" + index;

        element.id = id;

        applyMelodyLayout(card);

        window.renderPatternString(pattern, id, null);
    });

    // Якщо користувач змінює ширину вікна
    window.addEventListener("resize", function () {
        melodyNotation.forEach(function (element) {
            applyMelodyLayout(element.parentElement);
        });
    });
});
