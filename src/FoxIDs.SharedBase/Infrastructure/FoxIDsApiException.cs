﻿using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Net;

namespace FoxIDs.Infrastructure
{
    [Serializable]
    public class FoxIDsApiException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public string Response { get; private set; }

        public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

        public FoxIDsApiException() { }
        public FoxIDsApiException(string message, HttpStatusCode statusCode, string response, IReadOnlyDictionary<string, IEnumerable<string>> headers) : base(message) 
        {
            StatusCode = statusCode;
            Response = response;
            Headers = headers;
        }

        public FoxIDsApiException(string message, HttpStatusCode statusCode, string response, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
            Response = response;
        }

        public override string Message => Response.IsNullOrWhiteSpace() ? base.Message : Response.Substring(0, Response.Length >= 512 ? 512 : Response.Length);

        public override string ToString()
        {
            return $"{base.ToString()}{Environment.NewLine}Response: {Response}";
        }
    }
}
