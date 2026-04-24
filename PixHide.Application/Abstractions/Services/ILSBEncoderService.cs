using OpenCvSharp;

namespace PixHide.Application.Abstractions.Services;

public interface ILSBEncoderService
{
    void EncodeToImage(
        Mat img,
        byte[] inputFileData,
        string inputFileName,
        int bitsPerChannel,
        bool dither,
        bool contrastCorrection,
        bool clearAll = false,
        bool fillRandom = false
    );

    int GetMaxDataBytes(Mat img, int bitsPerChannel, int fileNameLength);
}
