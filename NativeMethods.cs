using System.Runtime.InteropServices;

namespace StoreFiler;

/// <summary>Sletting til papirkurv via Windows Shell (SHFileOperation).</summary>
internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        public int fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;   // Til papirkurv
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOERRORUI = 0x0400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    /// <summary>Send filene til papirkurven. Windows viser sin egen bekreftelsesdialog.</summary>
    public static bool DeleteToRecycleBin(IEnumerable<string> paths)
    {
        // Dobbel null-terminering kreves av API-et.
        string from = string.Join('\0', paths) + "\0\0";
        var op = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = from,
            fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOERRORUI),
        };
        int result = SHFileOperation(ref op);
        return result == 0 && op.fAnyOperationsAborted == 0;
    }
}
