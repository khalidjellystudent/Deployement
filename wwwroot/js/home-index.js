document.addEventListener('DOMContentLoaded', function() {
    const images = window.manualImages || [];
    const stepTexts = window.manualStepTexts || [];

    let currentIndex = 0;
    const imgEl = document.getElementById("tutorialImage");
    const captionEl = document.getElementById("caption");
    const descEl = document.getElementById("stepDescription");
    const prevBtn = document.getElementById("prevBtn");
    const nextBtn = document.getElementById("nextBtn");

    function updateStep() {
        if (!images.length) return;
        imgEl.src = images[currentIndex];
        captionEl.textContent = `الخطوة ${currentIndex + 1} من ${images.length}`;
        descEl.textContent = stepTexts[currentIndex] || '';
        prevBtn.disabled = currentIndex === 0;
        nextBtn.disabled = currentIndex === images.length - 1;
    }

    if (prevBtn && nextBtn) {
        prevBtn.addEventListener("click", () => {
            if (currentIndex > 0) {
                currentIndex--;
                updateStep();
            }
        });

        nextBtn.addEventListener("click", () => {
            if (currentIndex < images.length - 1) {
                currentIndex++;
                updateStep();
            }
        });
    }

    // Initialize
    updateStep();
});
