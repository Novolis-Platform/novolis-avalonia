using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Threading;

namespace Novolis.Avalonia.Agent;

internal static class MiniDump
{
    const int MiniDumpWithFullMemoryInfo = 0x00000800;
    const int MiniDumpWithThreadInfo = 0x00001000;
    const int MiniDumpNormal = 0x00000000;

    public static bool TryWrite(string path)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var proc = Process.GetCurrentProcess();
        return MiniDumpWriteDump(
            proc.Handle,
            (uint)proc.Id,
            fs.SafeFileHandle,
            MiniDumpNormal | MiniDumpWithThreadInfo | MiniDumpWithFullMemoryInfo,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
    }

    [DllImport("dbghelp.dll", SetLastError = true)]
    static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        uint processId,
        SafeHandle hFile,
        int dumpType,
        IntPtr exceptionParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);
}
