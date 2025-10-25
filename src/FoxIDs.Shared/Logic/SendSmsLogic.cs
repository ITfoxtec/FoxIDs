using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.External.Sms;
using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SendSmsLogic : LogicBase
    {
        private readonly Settings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IHttpClientFactory httpClientFactory;

        public SendSmsLogic(Settings settings, TelemetryScopedLogger logger, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task SendSmsAsync(string phone, SmsContent smsContent)
        {
            if (phone == null) throw new ArgumentNullException(nameof(phone));
            if (smsContent == null) throw new ArgumentNullException(nameof(smsContent));

            try
            {
                var smsSettings = GetSettings();

                logger.ScopeTrace(() => $"Send SMS with '{smsSettings.Type}' using {(RouteBinding.SendSms == null ? "default" : "environment")} settings.");
                switch (smsSettings.Type)
                {
                    case SendSmsTypes.GatewayApi:
                        await SendSmsWithGatewayApiAsync(smsSettings, phone, smsContent);
                        break;

                    case SendSmsTypes.Smstools:
                        await SendSmsWithSmstoolsApiAsync(smsSettings, phone, smsContent);
                        break;

                    case SendSmsTypes.TeliaSmsGateway:
                        await SendSmsWithTeliaSmsGatewayAsync(smsSettings, phone, smsContent);
                        break;

                    default:
                        //TODO add support for other SMS providers
                        throw new NotSupportedException("SMS provider not supported.");
                }
            }
            catch (EmailConfigurationException cex)
            {
                logger.Warning(cex);
                return;
            }
        }

        private async Task SendSmsWithGatewayApiAsync(SendSms smsSettings, string phone, SmsContent smsContent)
        {
            try
            {
                var smsApiRequest = new GatewayApiRequest
                {
                    Recipients = [new GatewayApiRecipient { Msisdn = phone }],
                    Message = smsContent.Sms,
                    Sender = smsSettings.FromName,
                    Label = smsSettings.Label
                };
                logger.ScopeTrace(() => $"SMS to '{smsSettings.Type}', SMS Gateway API request '{smsApiRequest.ToJson()}'.", traceType: TraceTypes.Message);


                var httpClient = httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", smsSettings.ClientSecret);

                try
                {
                    using var response = await httpClient.PostAsPlainJsonAsync(smsSettings.ApiUrl, smsApiRequest);
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            var result = await response.Content.ReadAsStringAsync();
                            var smsApiResponse = result.ToObject<GatewayApiResponse>();
                            logger.Event($"SMS send to '{phone}'.");
                            logger.ScopeTrace(() => $"SMS send to '{phone}', API response '{smsApiResponse.ToJson()}'.", traceType: TraceTypes.Message);
                            return;

                        default:
                            var resultError = await response.Content.ReadAsStringAsync();
                            logger.ScopeTrace(() => $"Send SMS to '{phone}', API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);
                            throw new Exception($"SMS gateway API error '{resultError}'. Status code={response.StatusCode}.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to call SMS gateway API URL '{smsSettings.ApiUrl}'.", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Sending SMS to '{phone}' failed.", ex);
            }
        }

        private async Task SendSmsWithSmstoolsApiAsync(SendSms smsSettings, string phone, SmsContent smsContent)
        {
            try
            {
                var smsApiRequest = new SmstoolsApiRequest
                {
                    To = phone,
                    Message = smsContent.Sms,
                    Sender = smsSettings.FromName
                };
                logger.ScopeTrace(() => $"SMS to '{smsSettings.Type}', SMS Tools API request '{smsApiRequest.ToJson()}'.", traceType: TraceTypes.Message);


                var httpClient = httpClientFactory.CreateClient();
                try
                {
                    using var response = await httpClient.PostAsPlainJsonAsync(smsSettings.ApiUrl, smsApiRequest);
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            var result = await response.Content.ReadAsStringAsync();
                            var smsApiResponse = result.ToObject<SmstoolsApiResponse>();
                            logger.Event($"SMS send to '{phone}'.");
                            logger.ScopeTrace(() => $"SMS send to '{phone}', API response '{smsApiResponse.ToJson()}'.", traceType: TraceTypes.Message);
                            return;

                        default:
                            var resultError = await response.Content.ReadAsStringAsync();
                            logger.ScopeTrace(() => $"Send SMS to '{phone}', API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);
                            throw new Exception($"SMS gateway API error '{resultError}'. Status code={response.StatusCode}.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to call SMS gateway API URL '{smsSettings.ApiUrl}'.", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Sending SMS to '{phone}' failed.", ex);
            }
        }

        private async Task SendSmsWithTeliaSmsGatewayAsync(SendSms smsSettings, string phone, SmsContent smsContent)
        {
            try
            {
                var smsApiRequest = new TeliaSmsGatewayRequest
                {
                    OutboundMessageRequest = new TeliaSmsGatewayMessageRequest
                    {
                        Address = ["tel:" + phone],
                        SenderAddress = smsSettings.ClientId,
                        SenderName = smsSettings.FromName,
                        OutboundSmsTextMessage = new TeliaSmsGatewayTextMessage
                        {
                            Message = smsContent.Sms
                        }
                    }
                };
                logger.ScopeTrace(() => $"SMS to '{smsSettings.Type}', Telia SMS API request '{smsApiRequest.ToJson()}'.", traceType: TraceTypes.Message);

                var httpClientHandler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    SslProtocols = SslProtocols.Tls12,
                    ClientCertificates = { smsSettings.Key.ToX509Certificate(includePrivateKey: true) }
                };

                using var httpClient = new HttpClient(httpClientHandler, disposeHandler: true);
                httpClient.SetAuthorizationHeaderBearer(smsSettings.ClientSecret);
                try
                {
                    using var response = await httpClient.PostAsPlainJsonAsync(smsSettings.ApiUrl, smsApiRequest);
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                        case HttpStatusCode.Accepted:
                            var result = await response.Content.ReadAsStringAsync();
                            logger.Event($"SMS send to '{phone}'.");
                            logger.ScopeTrace(() => $"SMS send to '{phone}', API response '{result}'.", traceType: TraceTypes.Message);
                            return;

                        default:
                            var resultError = await response.Content.ReadAsStringAsync();
                            logger.ScopeTrace(() => $"Send SMS to '{phone}', Telia API error '{resultError}'. Status code={response.StatusCode}.", traceType: TraceTypes.Message);
                            throw new Exception($"Telia SMS gateway API error '{resultError}'. Status code={response.StatusCode}.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to call Telia SMS gateway API URL '{smsSettings.ApiUrl}'.", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Sending SMS to '{phone}' failed.", ex);
            }
        }

        private SendSms GetSettings()
        {
            if (RouteBinding.SendSms != null)
            {
                return RouteBinding.SendSms;
            }

            if (settings.Sms != null)
            {
                switch (settings.Sms.Type)
                {
                    case SendSmsTypes.GatewayApi:
                        return new SendSms
                        {
                            Type = settings.Sms.Type,
                            FromName = settings.Sms.FromName,
                            ApiUrl = settings.Sms.ApiUrl,
                            ClientId = settings.Sms.ClientId,
                            ClientSecret = settings.Sms.ClientSecret,
                            Label = settings.Sms.Label,
                        };

                    case SendSmsTypes.Smstools:
                        return new SendSms
                        {
                            Type = settings.Sms.Type,
                            FromName = settings.Sms.FromName,
                            ApiUrl = settings.Sms.ApiUrl,
                            ClientId = settings.Sms.ClientId,
                            ClientSecret = settings.Sms.ClientSecret,
                        };

                    case SendSmsTypes.TeliaSmsGateway:
                        var clientCertificate = X509Certificate2.CreateFromPem(settings.Sms.CertificatePemCrt, settings.Sms.CertificatePemKey);
                        return new SendSms
                        {
                            Type = settings.Sms.Type,
                            FromName = settings.Sms.FromName,
                            ApiUrl = settings.Sms.ApiUrl,
                            ClientId = settings.Sms.ClientId,
                            ClientSecret = settings.Sms.ClientSecret,
                            Key = clientCertificate.ToFTJsonWebKey(includePrivateKey: true)
                        };

                    default:
                        throw new NotSupportedException($"SMS type '{settings.Sms.Type}' is not supported in settings.");
                }
            }

            throw new EmailConfigurationException("SMS settings is not configured.");
        }
    }
}
