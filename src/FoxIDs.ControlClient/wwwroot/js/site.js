(function () {
    window.SetElementFocus = (element) => {
        element.focus();
    };

    var setFocus = true;

    $('.input-control').each(function () {
        var input = $(this);
        var parent = input.closest('.active-group'); // $(this.parentNode);

        if (setFocus) {
            input.focus();
            setFocus = false;
        }

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
