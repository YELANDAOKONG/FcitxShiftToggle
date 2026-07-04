namespace Fcitx5RShiftToggle;

public sealed class Options
{
    public bool ShowHelp { get; private init; }
    public bool ListDevices { get; private init; }
    public bool ToggleOnce { get; private init; }
    public string? DevicePath { get; private init; }
    public string? TargetUser { get; private init; }

    public static Options Parse(string[] args)
    {
        var showHelp = false;
        var listDevices = false;
        var toggleOnce = false;
        string? devicePath = null;
        string? targetUser = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    break;

                case "--list":
                    listDevices = true;
                    break;

                case "--toggle-once":
                    toggleOnce = true;
                    break;

                case "--device":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--device requires a value.");
                    devicePath = args[++i];
                    break;

                case "--user":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--user requires a value.");
                    targetUser = args[++i];
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        return new Options
        {
            ShowHelp = showHelp,
            ListDevices = listDevices,
            ToggleOnce = toggleOnce,
            DevicePath = devicePath,
            TargetUser = targetUser
        };
    }
}