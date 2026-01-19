// Toggle FAQ items
function toggleFAQ(id) {
    const content = document.getElementById(`content-${id}`);
    const icon = document.getElementById(`icon-${id}`);

    // Toggle content visibility
    content.style.display = content.style.display === 'block' ? 'none' : 'block';

    // Toggle icon rotation
    icon.style.transform = icon.style.transform === 'rotate(180deg)' ? 'rotate(0deg)' : 'rotate(180deg)';
}

// Initialize all FAQ answers as hidden
document.addEventListener('DOMContentLoaded', function () {
    const faqAnswers = document.querySelectorAll('.faq-answer');
    faqAnswers.forEach(answer => {
        answer.style.display = 'none';
    });
});