using OpenCvSharp;
using PixHide.Application.Abstractions.Services;
using PixHide.Application.Helpers;
using PixHide.Application.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PixHide.Application.Services;

public class LSBEncoderService : ILSBEncoderService
{
    const byte FORMAT_VERSION = 1;

    public void EncodeToImage(Mat img, byte[] inputFileData, string inputFileName, int bitsPerChannel, bool dither, bool contrastCorrection, bool clearAll = false, bool fillRandom = false)
    {

        ArgumentNullException.ThrowIfNull(img);
        if (img.Empty()) throw new ArgumentException("Image is empty", nameof(img));
        if (img.Depth() != MatType.CV_8U) throw new ArgumentException("Only 8-bit images are supported", nameof(img));

        if (bitsPerChannel <= 0 || bitsPerChannel >= 8)
            throw new ArgumentOutOfRangeException(nameof(bitsPerChannel), "bitsPerChannel must be 1-7");

        var sw = Stopwatch.StartNew(); // For performance measurement, can be removed in production

        int width = img.Cols;
        int height = img.Rows;
        int channels = img.Channels();
        int pixelCount = width * height;

        long imageBytesLong = (long)pixelCount * channels;

        if (imageBytesLong >= int.MaxValue)
            throw new NotSupportedException("Image too large for managed buffer (>2GB)");

        int imageBytes = (int)imageBytesLong;

        // ===== READ DATA =====
        byte[] data = inputFileData ?? [];

        // prepare payload = nameLength(1 byte) + name(bytes UTF8) + data
        string fileName = inputFileName ?? string.Empty;

        // Manual checks: limit to 255 characters and ensure UTF8 byte length fits in one byte
        if (fileName.Length > 255)
            throw new ArgumentException("File name too long (must be <= 255 characters)", nameof(inputFileName));

        byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
        int nameLen = nameBytes.Length;
        if (nameLen > 255)
            throw new ArgumentException("File name UTF8 encoding too long (must be <= 255 bytes)", nameof(inputFileName));

        // ===== PAYLOAD =====
        byte[] payload = new byte[1 + nameLen + data.Length];

        payload[0] = (byte)nameLen;
        if (nameLen > 0) nameBytes.CopyTo(payload, 1);
        if (data.Length > 0) data.CopyTo(payload, 1 + nameLen);

        // ===== HEADER =====
        var headerObj = new ContainerHeader(
            FORMAT_VERSION,
            (byte)bitsPerChannel,
            payload.Length,
            SHA256.HashData(payload)
        );

        Span<byte> headerSpan = headerObj.ToBytes();

        int startImgPos = ContainerHeader.HeaderBytes * 8;
        long capacityDataBits = ( (long)imageBytes - startImgPos ) * bitsPerChannel;

        int maxDataBytes = GetMaxDataBytes(img, bitsPerChannel, nameLen);

        if ( data.Length > maxDataBytes || maxDataBytes == 0 )
            throw new Exception("File too large for this image");


        int[] imgIndexes = LSBHelper.GetImageIndexes(pixelCount, 0);


        Mat? imageDitherMat = null;

        // ===== CONTRAST CORRECTION =====
        if (contrastCorrection && bitsPerChannel > 1)
        {
            int startPixel = startImgPos / channels;
            int pixelCountCorr = (int)(capacityDataBits / channels / bitsPerChannel);

            Debug.WriteLine($"Contrast correction: pixels {startPixel} .. {startPixel + pixelCountCorr}");

            imageDitherMat ??= img.Clone();

            ImageHelper.CorrectContrast(imageDitherMat, bitsPerChannel, startPixel, startPixel + pixelCountCorr, imgIndexes);
        }

        // ===== DITHER =====
        if (dither)
        {
            imageDitherMat ??= img.Clone();

            ImageHelper.FloydSteinbergDither(imageDitherMat, 8 - bitsPerChannel);
        }

        // ===== ENCODE HEADER (1 bit per channel) =====
        encodeBitsToImage(img, headerSpan, 1, 0, bitsPerChannel == 1 ? imageDitherMat : null, imgIndexes, false, false);

        // ===== ENCODE DATA (payload = nameLength + name + data) =====
        encodeBitsToImage(img, (ReadOnlySpan<byte>)payload, bitsPerChannel, startImgPos, imageDitherMat, imgIndexes, clearAll, fillRandom);

        sw.Stop();

        // ===== DEBUG / STATS =====
        double headerSizeKb = ContainerHeader.HeaderBytes / 1024.0;
        double payloadSizeKb = payload.Length / 1024.0;
        double dataSizeKb = data.Length / 1024.0;
        double maxDataKb = capacityDataBits / 8.0 / 1024.0;
        double usedPercent = payloadSizeKb / maxDataKb * 100.0;

        Debug.WriteLine("===== ENCODING COMPLETE =====");
        Debug.WriteLine($"Payload size:         {payloadSizeKb:F2} KB");
        Debug.WriteLine($"File size:            {dataSizeKb:F2} KB");
        Debug.WriteLine($"Header size:          {headerSizeKb:F2} KB");
        Debug.WriteLine($"Max data capacity:    {maxDataKb:F2} KB");
        Debug.WriteLine($"Usage:                {usedPercent:F2} %");
        Debug.WriteLine($"Encoding time:        {sw.ElapsedMilliseconds} ms");

        if (imageDitherMat is not null && !imageDitherMat.Empty()) imageDitherMat.Dispose();
        imgIndexes = null;
        imageDitherMat = null;
    }

    public int GetMaxDataBytes(Mat img, int bitsPerChannel, int fileNameLength)
    {
        ArgumentNullException.ThrowIfNull(img);
        if (img.Empty()) throw new ArgumentException("Image is empty", nameof(img));
        if (img.Depth() != MatType.CV_8U) throw new ArgumentException("Only 8-bit images are supported", nameof(img));

        if (bitsPerChannel <= 0 || bitsPerChannel >= 8)
            throw new ArgumentOutOfRangeException(nameof(bitsPerChannel), "bitsPerChannel must be 1..7");

        if (fileNameLength < 0 || fileNameLength > 255)
            throw new ArgumentOutOfRangeException(nameof(fileNameLength));

        long totalBits = getAvailableDataBits(img, bitsPerChannel);
        long nameBits = (1L + fileNameLength) * 8;

        long dataBits = totalBits - nameBits;
        if (dataBits <= 0) return 0;

        return (int)(dataBits / 8);
    }

    // UTILS

    private static void encodeBitsToImage(
        Mat img,
        ReadOnlySpan<byte> source,
        int bitsPerChannel,
        int startImgPos,
        Mat? imgDither,
        int[]? imgIndexes = null,
        bool clearAll = false,
        bool fillRandom = false
    ) {
        if (img.Empty()) throw new ArgumentException("Image is empty", nameof(img));
        if (imgDither is not null && imgDither.Empty()) throw new ArgumentException("Dither image is empty", nameof(img));

        if (img.Depth() != MatType.CV_8U) throw new ArgumentException("Only 8-bit images are supported", nameof(img));

        int width = img.Cols;
        int height = img.Rows;
        int pixelCount = width * height;
        int channels = img.Channels();

        long imageBytesLong = (long)width * height * channels;
        if (imageBytesLong >= int.MaxValue) throw new ArgumentException("Image is too large");

        if ( imgIndexes is not null && imgIndexes.Length != width * height) throw new ArgumentException("imgIndexes length must be equal to pixel count (width*height)", nameof(imgIndexes));

        if (bitsPerChannel <= 0 || bitsPerChannel >= 8)
            throw new ArgumentOutOfRangeException(nameof(bitsPerChannel));

        Span<byte> image = img.AsSpan<byte>();

        int imgLen = image.Length;
        var rnd = fillRandom ? Random.Shared : null;

        ReadOnlySpan<byte> baseSpan = (imgDither ?? img).AsSpan<byte>();

        int valueMask = (1 << bitsPerChannel) - 1;
        int lsbClearMask = (~valueMask) & 0xFF;

        long totalSourceBits = (long)source.Length * 8;
        long srcBitPos = 0;

        // startImgPos is a group index (one group == one channel byte)
        long groupIndex = startImgPos;

        while (groupIndex < imgLen)
        {
            int value = 0;

            if (srcBitPos < totalSourceBits)
            {
                for (int k = 0; k < bitsPerChannel; k++)
                {
                    int bit = 0;
                    if (srcBitPos < totalSourceBits)
                    {
                        int srcByteIndex = (int)(srcBitPos / 8);
                        int bitInByte = (int)(srcBitPos % 8); // 0..7, 0 — MSB
                        byte srcByte = source[srcByteIndex];
                        bit = (srcByte >> (7 - bitInByte)) & 1;
                    }
                    value = (value << 1) | bit;
                    srcBitPos++;
                }
            }
            else if (!clearAll)
            {
                // Nothing more to write and not clearing; exit
                break;
            }
            else if (fillRandom && rnd != null)
            {
                value = rnd.Next(1 << bitsPerChannel);
            }
            else
            {
                value = 0;
            }

            int pixelIndex = (int)(groupIndex / channels);
            int channel = (int)(groupIndex % channels);

            if (pixelIndex >= pixelCount) break; // Out of pixel range - stop writing

            int pixelId = imgIndexes != null ? imgIndexes[pixelIndex] : pixelIndex;
            int imgByteIndex = pixelId * channels + channel;

            byte baseByte = baseSpan[imgByteIndex];
            image[imgByteIndex] = (byte)( (baseByte & lsbClearMask ) | value );

            groupIndex++;
        }
    }

    private static long getAvailableDataBits(Mat img, int bitsPerChannel)
    {
        ArgumentNullException.ThrowIfNull(img);
        if (img.Empty()) throw new ArgumentException("Image is empty", nameof(img));
        if (img.Depth() != MatType.CV_8U) throw new ArgumentException("Only 8-bit images are supported", nameof(img));

        int width = img.Cols;
        int height = img.Rows;
        int channels = img.Channels();

        long imageBytesLong = (long)width * height * channels;
        if (imageBytesLong >= int.MaxValue)
            throw new NotSupportedException("Image too large for managed buffer (>2GB)");

        return (imageBytesLong - ContainerHeader.HeaderBytes * 8) * bitsPerChannel;
    }
}