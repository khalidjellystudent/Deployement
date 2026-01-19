document.addEventListener('DOMContentLoaded', function () {
    // Add any interactive functionality here if needed
    console.log('Ticket results page loaded');

    // Example: Add click effect to rows
    const ticketRows = document.querySelectorAll('.hover-highlight');
    ticketRows.forEach(row => {
        row.addEventListener('click', function () {
            // The click is already handled by the anchor tag
            // Add any additional effects here
        });
    });
});