using NeoSmart.PrettySize;

namespace PixHide.WinUI3App.Helpers;

internal class FileSizeFormatter
{
    public static string FormatFileSize(ulong bytes)
    {
        return PrettySize.Format(bytes, UnitBase.Base2); // Yes, I'm that lazy
    }
}
