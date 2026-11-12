using System.IO;
using System.Runtime.InteropServices;

namespace TarkovQuestTracker.Interop;

/// <summary>How sending a file to the Recycle Bin went.</summary>
public enum RecycleResult
{
    Done,
    /// <summary>Open somewhere (the game still writing it, a picture viewer): try again later.</summary>
    Busy,
    /// <summary>Windows or an antivirus said no (Controlled folder access, ransomware protection): stop trying.</summary>
    Blocked,
}

/// <summary>
/// Moves a file to the Recycle Bin the way Explorer does (the player can put it back), without any dialog.
/// </summary>
static class RecycleBin
{
    const uint FO_DELETE = 0x0003;
    const ushort FOF_SILENT = 0x0004, FOF_NOCONFIRMATION = 0x0010, FOF_ALLOWUNDO = 0x0040, FOF_NOERRORUI = 0x0400;
    /// <summary>SHFileOperation's own "access denied" codes, besides the Win32 ERROR_ACCESS_DENIED (5).</summary>
    const int DE_ACCESSDENIEDSRC = 0x78, ERROR_ACCESS_DENIED = 5;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

    public static RecycleResult Send(string path)
    {
        if (!File.Exists(path)) return RecycleResult.Done;  // gone already (the player deleted it)
        var op = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = path + "\0\0",  // a double-NUL-terminated list of one
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
        };
        var code = SHFileOperation(ref op);
        if (code == 0 && !op.fAnyOperationsAborted && !File.Exists(path)) return RecycleResult.Done;
        if (code is DE_ACCESSDENIEDSRC or ERROR_ACCESS_DENIED) return RecycleResult.Blocked;
        // a sharing violation or anything unclear: tried again next round (the caller gives up after a few)
        return RecycleResult.Busy;
    }
}
