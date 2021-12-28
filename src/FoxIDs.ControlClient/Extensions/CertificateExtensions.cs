using FoxIDs.Models.Api;
using System;

namespace FoxIDs.Client
{
    public static class CertificateExtensions
    {
        public static bool IsValid(this CertificateInfo certificateInfo)
        {
            var localTime = DateTime.Now;
            if (certificateInfo.ValidFrom > localTime) return false;
            if (certificateInfo.ValidTo < localTime) return false;
            return true;
        }
    }
}
