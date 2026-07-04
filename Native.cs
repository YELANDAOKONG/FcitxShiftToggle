using System.Runtime.InteropServices;

namespace Fcitx5RShiftToggle;

public static partial class Native
{
    [LibraryImport("libc")]
    public static partial uint geteuid();
}