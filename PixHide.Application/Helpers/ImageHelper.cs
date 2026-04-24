using OpenCvSharp;
using System.Diagnostics;

namespace PixHide.Application.Helpers;

public class ImageHelper
{
    public static void CorrectContrast(Mat img, int bitsPerChannel, int startPixel, int endPixel, int[]? imgIndexes = null)
    {
        ArgumentNullException.ThrowIfNull(img);
        if (img.Empty()) throw new ArgumentException("Image is empty", nameof(img));
        if (img.Depth() != MatType.CV_8U) throw new ArgumentException("Only 8-bit images are supported", nameof(img));
        if (bitsPerChannel < 1 || bitsPerChannel >= 8) throw new ArgumentOutOfRangeException(nameof(bitsPerChannel));
        if (bitsPerChannel == 1) return; // We can't correct contrast for 1 bit per channel, so just skip it

        int width = img.Cols;
        int height = img.Rows;
        int channels = img.Channels();

        if (imgIndexes is not null && imgIndexes.Length != width * height) throw new ArgumentException("imgIndexes length must be equal to pixel count (width*height)", nameof(imgIndexes));

        long imageBytesLong = (long)width * height * channels;
        if (imageBytesLong >= int.MaxValue) throw new ArgumentException("Image is too large");

        int end = Math.Min(endPixel, width * height);
        int avg = 1 << (bitsPerChannel - 1);

        Debug.WriteLine($"Contrast correction: avg={avg}, {bitsPerChannel}");

        Span<byte> image = img.AsSpan<byte>();

        for (int i = startPixel; i < end; i++)
        {
            int pixelId = (imgIndexes is not null) ? imgIndexes[i] : i;

            for (int c = 0; c < channels; c++)
            {
                int imageByteId = pixelId * channels + c;
                image[imageByteId] = ClampByte(image[imageByteId] - avg);
            }
        }
    }

    public static void FloydSteinbergDither(Mat src, int bitsPerChannel)
    {
        ArgumentNullException.ThrowIfNull(src);

        if (src.Empty()) throw new ArgumentException("Image is empty", nameof(src));
        if (src.Depth() != MatType.CV_8U) throw new ArgumentException("Only 8-bit images are supported", nameof(src));
        if (bitsPerChannel < 1 || bitsPerChannel >= 8) throw new ArgumentOutOfRangeException(nameof(bitsPerChannel));

        int w = src.Cols, h = src.Rows;
        int channels = src.Channels();

        Span<byte> image = src.AsSpan<byte>();

        int rowSize = w * channels;

        // Insead of holding the whole image in float (which can be very large), we will hold only 2 rows - current and next.
        float[] curr = new float[rowSize];
        float[] next = new float[rowSize];

        int levels = 1 << bitsPerChannel;
        float scale = 256f / levels;
        float f7_16 = 7f / 16f, f3_16 = 3f / 16f, f5_16 = 5f / 16f, f1_16 = 1f / 16f;

        // Initialize curr with the first row of the image
        for (int i = 0; i < rowSize; i++)
            curr[i] = image[i];

        for (int y = 0; y < h; y++)
        {
            int rowStart = y * rowSize;

            // Clear next row buffer
            Array.Clear(next, 0, rowSize);

            // Load next row into next buffer (if exists)
            if (y + 1 < h)
            {
                int nextRowStart = (y + 1) * rowSize;
                for (int i = 0; i < rowSize; i++)
                    next[i] = image[nextRowStart + i];
            }

            for (int x = 0; x < w; x++)
            {
                int idx = x * channels;

                for (int c = 0; c < channels; c++)
                {
                    int i = idx + c;

                    float oldVal = curr[i];
                    float newVal = (float)Math.Round(oldVal / scale) * scale;
                    float err = oldVal - newVal;

                    curr[i] = newVal;

                    // Right
                    if (x + 1 < w)
                        curr[i + channels] += err * f7_16;

                    // Bottom-left
                    if (y + 1 < h && x - 1 >= 0)
                        next[(x - 1) * channels + c] += err * f3_16;

                    // Bottom
                    if (y + 1 < h)
                        next[x * channels + c] += err * f5_16;

                    // Bottom-right
                    if (y + 1 < h && x + 1 < w)
                        next[(x + 1) * channels + c] += err * f1_16;
                }
            }

            // Write dithered row back to image
            for (int i = 0; i < rowSize; i++)
            {
                int v = (int)Math.Round(curr[i]);
                if (v < 0) v = 0;
                else if (v > 255) v = 255;
                image[rowStart + i] = (byte)v;
            }

            // Swap buffers
            (next, curr) = (curr, next);
        }
    }

    // ================= UTIL =================

    static private byte ClampByte(int v)
        => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
}
