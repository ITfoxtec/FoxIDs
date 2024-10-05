function saveCertFile(fileName, content) {
    var link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/pkix-cert;charset=utf-8," + encodeURIComponent("-----BEGIN CERTIFICATE-----\n" + content + "\n-----END CERTIFICATE-----")
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function readError() {
    return document.getElementById('error').textContent;    
}

function loadMollie(profileId, testmode) {
    var mollie = Mollie(profileId, { locale: 'en_GB', testmode: testmode });

    var cardNumber = mollie.createComponent('cardNumber');
    cardNumber.mount('#card-number');

    var cardHolder = mollie.createComponent('cardHolder');
    cardHolder.mount('#card-holder');

    var expiryDate = mollie.createComponent('expiryDate');
    expiryDate.mount('#expiry-date');

    var verificationCode = mollie.createComponent('verificationCode');
    verificationCode.mount('#verification-code');

    var cardNumberError = document.querySelector('#card-number-error');

    cardNumber.addEventListener('change', event => {
        if (event.error && event.touched) {
            cardNumberError.textContent = event.error;
        } else {
            cardNumberError.textContent = '';
        }
    });

    document.getElementById('mollieform').addEventListener('submit', async e => {
        e.preventDefault();

        var { token, error } = await mollie.createToken();

        if (error) {
            // Something wrong happened while creating the token. Handle this situation gracefully.
            return;
        }

        // Add token to the form
        var tokenInput = document.createElement('input');
        tokenInput.setAttribute('type', 'hidden');
        tokenInput.setAttribute('name', 'cardToken');
        tokenInput.setAttribute('value', token);

        form.appendChild(tokenInput);

        // Submit form to the server
        form.submit();
    });
}