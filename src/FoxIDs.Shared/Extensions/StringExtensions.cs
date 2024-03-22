using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string text)
        {
            if(text.IsNullOrEmpty())
            {
                return text;
            }

            var resultText = new List<string>();

            var textSplit = text.Split('.');
            foreach (var item in textSplit)
            {
                if(item.Length > 0)
                {
                    resultText.Add($"{Char.ToLowerInvariant(item[0])}{(item.Length > 1 ? item.Substring(1) : string.Empty)}");
                }
                else
                {
                    resultText.Add(item);
                }
            }

            return string.Join('.', resultText);
        }

        public static string PartyIdToName(this string upPartyId)
        {
            if (upPartyId.IsNullOrEmpty())
            {
                return upPartyId;
            }

            var split = upPartyId.Split(':');
            return split[split.Length - 1];
        }

        public static Task<string> HashIdStringAsync(this string linkClaim) => linkClaim?.ToLower()?.Sha256HashBase64urlEncodedAsync();

        public static string Compress(this string text)
        {
            byte[] compressedBytes;

            using (var uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                using (var compressedStream = new MemoryStream())
                {
                    // setting the leaveOpen parameter to true to ensure that compressedStream will not be closed when compressorStream is disposed
                    // this allows compressorStream to close and flush its buffers to compressedStream and guarantees that compressedStream.ToArray() can be called afterward
                    // although MSDN documentation states that ToArray() can be called on a closed MemoryStream, I don't want to rely on that very odd behavior should it ever change
                    using (var compressorStream = new DeflateStream(compressedStream, CompressionLevel.Fastest, true))
                    {
                        uncompressedStream.CopyTo(compressorStream);
                    }

                    // call compressedStream.ToArray() after the enclosing DeflateStream has closed and flushed its buffer to compressedStream
                    compressedBytes = compressedStream.ToArray();
                }
            }

            return Convert.ToBase64String(compressedBytes);
        }

        public static string DecompressResponse(this string value)
        {
            using (var originalStream = new MemoryStream(Convert.FromBase64String(value)))
            using (var decompressedStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(originalStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(decompressedStream);
                }
                return Encoding.UTF8.GetString(decompressedStream.ToArray());
            }
        }
    }
}
