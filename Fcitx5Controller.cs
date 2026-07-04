namespace Fcitx5RShiftToggle;

public sealed class Fcitx5Controller
{
    public string TargetUser { get; }
    private readonly bool _isRoot;
    private readonly int? _targetUid;

    public Fcitx5Controller(string? targetUser)
    {
        _isRoot = Fcitx5RShiftToggle.Native.geteuid() == 0;

        TargetUser =
            !string.IsNullOrWhiteSpace(targetUser)
                ? targetUser
                : GuessTargetUser();

        if (_isRoot && TargetUser == "root")
        {
            throw new InvalidOperationException(
                "Refusing to manage fcitx5 as root. Please pass --user YOUR_USER.");
        }

        if (_isRoot)
        {
            _targetUid = GetUid(TargetUser);
        }
    }

    public void Toggle()
    {
        if (IsRunning())
        {
            Stop();
        }
        else
        {
            Start();
        }
    }

    private bool IsRunning()
    {
        var args = new List<string>
        {
            "-x",
            "fcitx5"
        };

        if (_isRoot)
        {
            args.Add("-u");
            args.Add(TargetUser);
        }

        var exitCode = ProcessUtil.Run("pgrep", args, quiet: true);
        return exitCode == 0;
    }

    private void Stop()
    {
        Console.WriteLine("Stopping fcitx5...");

        var args = new List<string>
        {
            "-x",
            "fcitx5"
        };

        if (_isRoot)
        {
            args.Add("-u");
            args.Add(TargetUser);
        }

        ProcessUtil.Run("pkill", args, quiet: true);
    }

    private void Start()
    {
        Console.WriteLine("Starting fcitx5...");

        if (_isRoot)
        {
            StartAsTargetUser();
        }
        else
        {
            ProcessUtil.Run("fcitx5", ["-d"], quiet: false);
        }
    }

    private void StartAsTargetUser()
    {
        if (_targetUid is null)
            throw new InvalidOperationException("Target uid is unknown.");

        var runtimeDir = $"/run/user/{_targetUid}";
        var dbusBus = $"unix:path={runtimeDir}/bus";
        var waylandDisplay = GuessWaylandDisplay(runtimeDir) ?? "wayland-0";

        var args = new List<string>
        {
            "-u",
            TargetUser,
            "--",
            "env",
            $"XDG_RUNTIME_DIR={runtimeDir}",
            $"DBUS_SESSION_BUS_ADDRESS={dbusBus}",
            $"WAYLAND_DISPLAY={waylandDisplay}",
            "fcitx5",
            "-d"
        };

        ProcessUtil.Run("runuser", args, quiet: false);
    }

    private static string GuessTargetUser()
    {
        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");

        if (!string.IsNullOrWhiteSpace(sudoUser) && sudoUser != "root")
            return sudoUser;

        return Environment.UserName;
    }

    private static int GetUid(string user)
    {
        var output = ProcessUtil.Capture("id", ["-u", user]).Trim();

        if (!int.TryParse(output, out var uid))
            throw new InvalidOperationException($"Cannot resolve uid for user: {user}");

        return uid;
    }

    private static string? GuessWaylandDisplay(string runtimeDir)
    {
        if (!Directory.Exists(runtimeDir))
            return null;

        return Directory
            .EnumerateFiles(runtimeDir, "wayland-*")
            .Select(Path.GetFileName)
            .Where(x => x is not null && !x.EndsWith(".lock"))
            .OrderBy(x => x)
            .FirstOrDefault();
    }
}