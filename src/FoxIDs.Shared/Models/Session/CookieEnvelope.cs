using System.Linq;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace FoxIDs.Models.Session
{
    public class CookieEnvelope
    {
        [JsonProperty(PropertyName = "c")]
        public long? CreatedForCleanup { get; set; }

        [JsonProperty(PropertyName = "r")]
        public string RouteForCleanup { get; set; }

        [JsonIgnore]
        public string Content { get; set; }
    }

    public class CookieEnvelope<TMessage> : CookieEnvelope where TMessage : CookieMessage
    {
        [JsonIgnore]
        public TMessage Message { get; set; }

        public string ToCookieString(IDataProtector protector)
        {
            Protect(protector);
            if (CreatedForCleanup == null && RouteForCleanup == null)
            {
                return Content;
            }
            else
            {
                return $"{this.ToJson().Base64UrlEncode()}.{Content}";
            }
        }

        public static CookieEnvelope<TMessage> FromCookieString(IDataProtector protector, string cookie)
        {
            var cookieSplit = cookie.Split('.');
            if(cookieSplit.Count() == 2)
            {
                var envelope = cookieSplit[0].Base64UrlDecode().ToObject<CookieEnvelope<TMessage>>();
                envelope.Content = cookieSplit[1];
                envelope.Unprotect(protector);
                return envelope;
            }
            else
            {
                var envelope = new CookieEnvelope<TMessage>();
                envelope.Content = cookieSplit[0];
                envelope.Unprotect(protector);
                return envelope;
            }            
        }

        private void Protect(IDataProtector protector)
        {
            Content = protector.Protect(Message.ToJson());
        }

        private void Unprotect(IDataProtector protector)
        {
            Message = protector.Unprotect(Content).ToObject<TMessage>();
        }
    }
}
