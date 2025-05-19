using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.MasterSeedTool
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task ValidateResponseAsync(this HttpResponseMessage responseMessage)
        {
            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.OK:
                    return;
                case HttpStatusCode.NoContent:
                    return;
                //case HttpStatusCode.Found:
                //    return;

                default:
                    var result = await responseMessage.Content.ReadAsStringAsync();
                    throw new Exception($"Http status code '{responseMessage.StatusCode}', response '{result}'.");
            }
        }
    }
}
