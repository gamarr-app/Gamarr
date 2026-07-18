using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Checksum;

namespace NzbDrone.Core.RomCatalog
{
    public static class NoIntroRomHasher
    {
        public static NoIntroHashTriplet Compute(Stream stream)
        {
            using var md5 = MD5.Create();
            using var sha1 = SHA1.Create();
            var crc = new Crc32();
            var buffer = new byte[8192];

            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                crc.Update(buffer.Take(bytesRead).ToArray());
            }

            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            return new NoIntroHashTriplet
            {
                Md5 = ToHex(md5.Hash),
                Sha1 = ToHex(sha1.Hash),
                Crc32 = crc.Value.ToString("X8"),
                PreferredHashType = "sha1",
                PreferredHashValue = ToHex(sha1.Hash)
            };
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }
    }

    public class NoIntroHashTriplet
    {
        public string Md5 { get; set; }
        public string Sha1 { get; set; }
        public string Crc32 { get; set; }
        public string PreferredHashType { get; set; }
        public string PreferredHashValue { get; set; }
    }
}
