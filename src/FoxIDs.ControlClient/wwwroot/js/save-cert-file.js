function saveCertFile(fileName, content) {
    var link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/pkix-cert;charset=utf-8," + encodeURIComponent("-----BEGIN CERTIFICATE-----\n" + content + "\n-----END CERTIFICATE-----")
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}