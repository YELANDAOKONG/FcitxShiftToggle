using System.Diagnostics;

namespace Fcitx5RShiftToggle;

public static class ProcessUtil
{
    public static int Run(string fileName, IEnumerable<string> args, bool quiet)
    {
        using var process = Start(fileName, args, quiet, capture: false);
        process.WaitForExit();
        return process.ExitCode;
    }

    public static string Capture(string fileName, IEnumerable<string> args)
    {
        using var process = Start(fileName, args, quiet: true, capture: true);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} exited with code {process.ExitCode}.");

        return output;
    }

    private static Process Start(
        string fileName,
        IEnumerable<string> args,
        bool quiet,
        bool capture)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = quiet || capture,
            RedirectStandardError = quiet || capture
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        return Process.Start(psi)
               ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
    }
}