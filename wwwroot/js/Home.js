// You can add any interactive functionality here if needed
document.addEventListener('DOMContentLoaded', function () {
    console.log('Home page loaded');
});


string GetStatusBadgeClass(string status)
{
    return status switch
            {
        "Paid" => "bg-success",
        "Pending" => "bg-warning",
        "Rejected" => "bg-danger",
        "UnderProcess" => "bg-info text-white",

        _ => "bg-secondary"
            };
    }