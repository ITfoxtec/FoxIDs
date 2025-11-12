(function () {
    // ****
    // Compensating controls for CVE-2024-6531 
    $('[data-toggle="tooltip"]').tooltip({ html: false });
    $('[data-toggle="popover"]').popover({ html: false });
    // ****

    var setFocus = true;

    $('.input-control').each(function () {
        var input = $(this);
        var parent = input.closest('.active-group');

        if (setFocus) {
            input.focus();
            setFocus = false;
        }

        if (input.val()) {
            parent.addClass('active');
        }

        input.blur(function () {
            if (input.val()) {
                parent.addClass('active');
            }
            else {
                parent.removeClass('active');
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

    function setDisableOnSubmit() {
        $("form").submit(function (event) {
            if (!this.submitDisable) {
                if ($(this).valid()) {
                    $("input[type=submit]", this).attr("disabled", true);
                    this.submitDisable = true;
                }
            }
            else {
                event.preventDefault();
            }
        });
    }
    setDisableOnSubmit();

    $('#form-filter').submit(function (event) {
        var filterValue = $('.input-control', this).val();
        $('.list-item-filter', this).each(function () {
            var item = $(this);
            if (!filterValue) {
                item.removeClass('d-none');
            }
            else { 
                var upParty = item.attr('up-party-name');
                if (upParty.indexOf(filterValue) >= 0) {
                    item.removeClass('d-none');
                }
                else {
                    item.addClass('d-none');
                }
            }
        });
        event.preventDefault();
    });

    $('.footer-content').click(function () {
        $('.footer-version').toggle('slow');
    });
})();


