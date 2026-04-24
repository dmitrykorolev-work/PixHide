using OpenCvSharp;

using PixHide.Application.Models;

namespace PixHide.Application.Abstractions.Services;

public interface ILSBDecoderService
{
    DecodeResult DecodeFromImage(Mat img);
}
