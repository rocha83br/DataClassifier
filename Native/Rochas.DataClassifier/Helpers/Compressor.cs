using System;
using System.IO;
using System.Text;
using System.IO.Compression;

namespace Rochas.DataClassifier.Helpers
{
    public static class Compressor
    {
        #region Public Methods

        public static string CompressText(string rawText)
        {
            byte[] rawBinary = null;
            byte[] compressedBinary = null;

            rawBinary = ASCIIEncoding.ASCII.GetBytes(rawText);

            compressedBinary = compressBinary(rawBinary);

            return Convert.ToBase64String(compressedBinary);
        }

        public static string UncompressText(string compressedText)
        {
            string result = string.Empty;
            byte[] compressedBinary = Convert.FromBase64String(compressedText);
            byte[] destinBinary = uncompressBinary(compressedBinary);

            result = new string(ASCIIEncoding.ASCII.GetChars(destinBinary));

            return result.ToString();
        }

        #endregion

        #region Helper Methods

        private static byte[] compressBinary(byte[] rawSource)
        {
            var memDestination = new MemoryStream();
            var memSource = new MemoryStream(rawSource);
            var gzipStream = new GZipStream(memDestination, CompressionMode.Compress);

            memSource.CopyTo(gzipStream);

            gzipStream.Close();

            return memDestination.ToArray();
        }

        private static byte[] uncompressBinary(byte[] compressedSource)
        {
            byte[] unpackedContent = new byte[compressedSource.Length * 20];
            var memSource = new MemoryStream(compressedSource);

            var gzipStream = new GZipStream(memSource, CompressionMode.Decompress);

            var readedBytes = gzipStream.Read(unpackedContent, 0, unpackedContent.Length);

            var memDestination = new MemoryStream(unpackedContent, 0, readedBytes);

            return memDestination.ToArray();
        }

        #endregion
    }
}
