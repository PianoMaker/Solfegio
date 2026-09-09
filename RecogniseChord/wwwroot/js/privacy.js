document.addEventListener("DOMContentLoaded", function () {

    console.debug("privacy.js starts")
    const showDetails = document.getElementById("showdetails");
    const details = document.getElementById("details");

    showDetails.addEventListener("click", function () {

        if (getComputedStyle(details).display === "none") {
            details.style.display = "block";
            showDetails.textContent = "Сховати";
        } else {
            details.style.display = "none";
            showDetails.textContent = "Детальніше";
        }

    });
});
