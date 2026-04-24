using OpenCvSharp;
using System.Diagnostics;

using System.Security.Cryptography;
using System.Text;

using PixHide.Application.Helpers;
using PixHide.Application.Models;
using PixHide.Application.Abstractions.Services;

namespace PixHide.Application.Services;

public class LSBDecoderService : ILSBDecoderService
{
    const byte FORMAT_VERSION = 1;

    public DecodeResult DecodeFromImage(Mat img)
    {
        ArgumentNullException.ThrowIfNull(img);
        if (img.Empty()) throw new ArgumentException("Image is empty", nameof(img));
        if (img.Depth() != MatType.CV_8U) throw new ArgumentException("Only 8-bit images are supported", nameof(img));

        var sw = Stopwatch.StartNew(); // For performance measurement, can be removed in production

        int width = img.Cols;
        int height = img.Rows;
        int pixelCount = width * height;
        int channels = img.Channels();

        long lenLong = (long)width * height * channels;

        if (lenLong >= int.MaxValue) throw new InvalidDataException("Image too large");
        int len = (int)lenLong;

        if (len < ContainerHeader.HeaderBytes)
            throw new InvalidDataException("Image too small to contain header.");

        ReadOnlySpan<byte> image = img.AsSpan<byte>();

        int[] imgIndexes = LSBHelper.GetImageIndexes(pixelCount, 0);
        if (imgIndexes.Length != width * height) throw new Exception("imgIndexes length must be equal to pixel count (width*height)");

        // --- Read header (1 bit per channel) ---
        var headerObj = ReadHeader(image, imgIndexes, channels);

        if (headerObj.FormatVersion > FORMAT_VERSION)
            throw new InvalidDataException($"Unsupported version {headerObj.FormatVersion}. Max supported is {FORMAT_VERSION}.");

        byte nBits = headerObj.BitsPerChannel;
        int payloadSize = headerObj.PayloadLength;
        var fileHash = headerObj.PayloadHash;

        if (payloadSize < 0) throw new InvalidDataException("Invalid payload size in header.");

        // --- Read payload (nBits per channel) ---
        int startIdx = ContainerHeader.HeaderBits; // index into flat where data LSB-groups begin
        int lsbCount = len - startIdx;
        if (lsbCount <= 0) throw new InvalidDataException("No payload present in image.");

        long totalDataBits = (long)lsbCount * nBits;
        long neededBits = (long)payloadSize * 8;
        if (totalDataBits < neededBits)
            throw new InvalidDataException("Image does not contain enough data for declared payload size.");

        // Build payload bytes directly
        var payload = new byte[payloadSize];

        for (int b = 0; b < payloadSize; b++)
        {
            byte val = 0;

            for (int k = 0; k < 8; k++)
            {
                long globalBit = (long)b * 8 + k; // 0-based bit index in the stream
                long groupIndex = globalBit / nBits; // which nBits-group from start of payload
                int bitInGroup = (int)(globalBit % nBits); // 0..nBits-1 (MSB-first within group)

                // overall group index within image (groups are per-channel bytes)
                long groupGlobal = startIdx + groupIndex;

                int pixelIndex = (int)(groupGlobal / channels);
                int channel = (int)(groupGlobal % channels);

                int pixelId = imgIndexes[pixelIndex];

                int imgPos = pixelId * channels + channel;

                int shift = (nBits - 1 - bitInGroup);
                int bit = (image[imgPos] >> shift) & 1;

                val = (byte)( (val << 1) | bit );
            }

            payload[b] = val;
        }

        // --- Check hash (over whole payload) ---
        var actualHash = SHA256.HashData(payload);
        if ( !actualHash.SequenceEqual(fileHash) )
            throw new InvalidDataException("SHA256 mismatch: payload corrupted or wrong image.");

        sw.Stop();

        // --- Parse payload: nameLength(1 byte) + name + data ---
        if (payloadSize < 1) throw new InvalidDataException("Payload too small to contain name length.");

        int nameLen = payload[0];
        if (nameLen < 0 || nameLen > payloadSize - 1) throw new InvalidDataException("Invalid name length in payload.");

        string? embeddedName = nameLen > 0 ? Encoding.UTF8.GetString(payload, 1, nameLen) : null;

        int dataStart = 1 + nameLen;
        int dataLen = payloadSize - dataStart;
        
        var data = new byte[dataLen];
        
        if (dataLen > 0) Array.Copy(payload, dataStart, data, 0, dataLen);

        // --- Debug / Stats ---
        double headerSizeKb = ContainerHeader.HeaderBytes / 1024.0;
        double payloadSizeKb = payloadSize / 1024.0;
        double dataSizeKb = dataLen / 1024.0;
        double capacityDataBits = (double)lsbCount * nBits;
        double maxDataKb = capacityDataBits / 8.0 / 1024.0;
        double usedPercent = maxDataKb > 0 ? payloadSizeKb / maxDataKb * 100.0 : 0.0;

        Debug.WriteLine("===== DECODING COMPLETE =====");
        Debug.WriteLine($"Payload size:         {payloadSizeKb:F2} KB");
        Debug.WriteLine($"File size:            {dataSizeKb:F2} KB");
        Debug.WriteLine($"Header size:          {headerSizeKb:F2} KB");
        Debug.WriteLine($"Max data capacity:    {maxDataKb:F2} KB");
        Debug.WriteLine($"Usage:                {usedPercent:F2} %");
        Debug.WriteLine($"Decoding time:        {sw.ElapsedMilliseconds} ms");

        return new DecodeResult(
            FileName: embeddedName,
            Data: data,
            FileSize: (ulong)dataLen,
            MaxCapacity: (ulong)(capacityDataBits / 8) - (ulong)nameLen - 1
        );
    }

    public static ContainerHeader ReadHeader(
        ReadOnlySpan<byte> image,
        int[] imgIndexes,
        int channels)
    {
        Span<byte> header = new byte[ContainerHeader.HeaderBytes];
        int bitIndex = 0;
        for (int b = 0; b < ContainerHeader.HeaderBytes; b++)
        {
            byte val = 0;
            for (int k = 0; k < 8; k++)
            {
                int groupIndex = bitIndex++;
                int pixelIndex = groupIndex / channels;
                int channel = groupIndex % channels;
                int pixelId = imgIndexes[pixelIndex];
                int imgPos = pixelId * channels + channel;
                val = (byte)((val << 1) | (image[imgPos] & 1));
            }
            header[b] = val;
        }

        return ContainerHeader.FromBytes(header);
    }
}