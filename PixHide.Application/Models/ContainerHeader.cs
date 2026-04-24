namespace PixHide.Application.Models;

public sealed class ContainerHeader
{
    public static readonly byte[] Magic = [(byte)'L', (byte)'S', (byte)'B', (byte)'D'];

    public const int HeaderBytes = 4 + 1 + 1 + 4 + 32 + 6; // MAGIC(4) + FORMAT_VERSION(1) + BITS_PER_CHANNEL(1) + Payload Length(4) + Payload Hash(32) + Empty Padding (6) = 48
    public const int HeaderBits = HeaderBytes * 8;

    public byte FormatVersion { get; }
    public byte BitsPerChannel { get; }
    public int PayloadLength { get; }
    public byte[] PayloadHash { get; }

    public ContainerHeader(
        byte formatVersion,
        byte bitsPerChannel,
        int payloadLength,
        byte[] payloadHash)
    {
        if (payloadHash.Length != 32)
            throw new ArgumentException("Hash must be 32 bytes (256 bits)", nameof(payloadHash));

        FormatVersion = formatVersion;
        BitsPerChannel = bitsPerChannel;
        PayloadLength = payloadLength;
        PayloadHash = payloadHash;
    }

    public static ContainerHeader FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderBytes)
            throw new ArgumentException($"Data must be at least {HeaderBytes} bytes");
        
        if ( !MagicEquals(data))
            throw new InvalidDataException("Invalid MAGIC in header");

        byte formatVersion = data[4];
        byte bitsPerChannel = data[5];

        if (bitsPerChannel < 1 || bitsPerChannel > 7) throw new InvalidDataException($"Invalid bits-per-channel in header: {bitsPerChannel}");

        int payloadLength = BitConverter.ToInt32( data.Slice(6, 4) );
        byte[] payloadHash = data.Slice(10, 32).ToArray();
        return new ContainerHeader(formatVersion, bitsPerChannel, payloadLength, payloadHash);
    }

    public byte[] ToBytes()
    {
        byte[] data = new byte[HeaderBytes];

        Magic.CopyTo(data, 0);

        data[4] = FormatVersion;
        data[5] = BitsPerChannel;

        BitConverter.GetBytes(PayloadLength).CopyTo(data, 6);

        PayloadHash.CopyTo(data, 10);

        return data;
    }

    public static bool MagicEquals( ReadOnlySpan<byte> data )
    {
        return data.Length >= Magic.Length && data[..Magic.Length].SequenceEqual(Magic);
    }
}