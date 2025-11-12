(function () {
    // ****
    // Compensating controls for CVE-2024-6531 
    $('[data-toggle="tooltip"]').tooltip({ html: false });
    $('[data-toggle="popover"]').popover({ html: false });
    // ****

    window.SetElementFocus = (element) => {
        element.focus();
    };

    $('.input-control').each(function () {
        var input = $(this);
        var parent = input.closest('.active-group');

        if (input.val()) {
            if (parent) {
                parent.addClass('active');
            }
        }

        input.blur(function () {
            if (input.val()) {
                if (parent) {
                    parent.addClass('active');
                }
            }
            else {
                if (parent) {
                    parent.removeClass('active');
                }
            }
        });
    });

    function browserValueCheck() {
        $('.input-control:not(.active)').each(function () {
            var input = $(this);
            if (input.val()) {
                var parent = input.closest('.active-group');
                if (parent) {
                    parent.addClass('active');
                }
            }
        });
    }
    setInterval(browserValueCheck, 100);

})();

function triggerClick(elt) {
    elt.click();
}