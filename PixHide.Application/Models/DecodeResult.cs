namespace PixHide.Application.Models;

public sealed record DecodeResult(
    string? FileName,
    byte[] Data,
    ulong FileSize,
    ulong MaxCapacity
);