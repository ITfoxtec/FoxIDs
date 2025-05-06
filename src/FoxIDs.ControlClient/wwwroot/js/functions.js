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


async function clipboardWriteText(text) {
    if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text);
    }
    else {
        const textArea = document.createElement("textarea");
        textArea.value = text;

        textArea.style.position = "absolute";
        textArea.style.left = "0px";
        textArea.style.right = "0px";
        textArea.style.height = "0px";
        textArea.style.width = "0px";

        document.body.prepend(textArea);
        textArea.select();

        try {
            document.execCommand('copy');
        }
        catch (error) {
            console.error(error);
        }
        finally {
            textArea.remove();
        }
    }
}

var mollie = null;
var cardNumber = null;
var cardHolder = null;
var expiryDate = null;
var verificationCode = null;
function loadMollie(profileId, testmode) {
    mollie = Mollie(profileId, { locale: 'en_GB', testmode: testmode });

    cardNumber = mollie.createComponent('cardNumber');
    cardNumber.mount('#card-number');

    cardHolder = mollie.createComponent('cardHolder');
    cardHolder.mount('#card-holder');

    expiryDate = mollie.createComponent('expiryDate');
    expiryDate.mount('#expiry-date');

    verificationCode = mollie.createComponent('verificationCode');
    verificationCode.mount('#verification-code');

    var cardNumberError = document.querySelector('#card-number-error');
    cardNumber.addEventListener('change', event => {
        if (event.error && event.touched) {
            cardNumberError.textContent = event.error;
        } else {
            cardNumberError.textContent = '';
        }
    });

    var cardHolderError = document.querySelector('#card-holder-error');
    cardHolder.addEventListener('change', event => {
        if (event.error && event.touched) {
            cardHolderError.textContent = event.error;
        } else {
            cardHolderError.textContent = '';
        }
    });

    var expiryDateError = document.querySelector('#expiry-date-error');
    expiryDate.addEventListener('change', event => {
        if (event.error && event.touched) {
            expiryDateError.textContent = event.error;
        } else {
            expiryDateError.textContent = '';
        }
    });

    var verificationCodeError = document.querySelector('#verification-code-error');
    verificationCode.addEventListener('change', event => {
        if (event.error && event.touched) {
            verificationCodeError.textContent = event.error;
        } else {
            verificationCodeError.textContent = '';
        }
    });
}

function unloadMollie() {
    cardNumber.unmount();
    cardHolder.unmount();
    expiryDate.unmount();
    verificationCode.unmount();
}

async function submitMollie() {
    return await mollie.createToken();
}