namespace Fcitx5RShiftToggle;

public class Program
{
    public const ushort EV_KEY = 0x01;
    public const ushort KEY_RIGHTSHIFT = 54;

    public static int Main(string[] args)
    {
        
        if (!Environment.Is64BitProcess)
        {
            Console.Error.WriteLine("This program currently assumes 64-bit Linux input_event layout.");
            return 1;
        }

        var options = Options.Parse(args);

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (options.ListDevices)
        {
            ListInputDevices();
            return 0;
        }

        var controller = new Fcitx5Controller(options.TargetUser);

        if (options.ToggleOnce)
        {
            controller.Toggle();
            return 0;
        }

        if (string.IsNullOrWhiteSpace(options.DevicePath))
        {
            Console.Error.WriteLine("Missing --device.");
            Console.Error.WriteLine();
            PrintHelp();
            return 1;
        }

        Console.WriteLine("fcitx5 Right Shift toggle daemon");
        Console.WriteLine($"Device: {options.DevicePath}");
        Console.WriteLine($"Target user: {controller.TargetUser}");
        Console.WriteLine("Hotkey: tap Right Shift alone");
        Console.WriteLine();

        var listener = new RightShiftListener(options.DevicePath, controller);
        listener.Run();

        return 0;
    }
    
    public static void PrintHelp()
    {
        Console.WriteLine("""
                          Usage:
                            Fcitx5RShiftToggle --list
                            Fcitx5RShiftToggle --device /dev/input/by-id/xxx-event-kbd --user YOUR_USER
                            Fcitx5RShiftToggle --toggle-once --user YOUR_USER

                          Options:
                            --list              List input event devices.
                            --device PATH       Keyboard event device path.
                            --user USER         User whose fcitx5 should be controlled.
                            --toggle-once       Toggle fcitx5 once and exit.
                            -h, --help          Show help.

                          Examples:
                            sudo ./Fcitx5RShiftToggle --list
                            sudo ./Fcitx5RShiftToggle --device /dev/input/by-id/usb-xxx-event-kbd --user yourname
                            ./Fcitx5RShiftToggle --toggle-once
                          """);
    }

    public static void ListInputDevices()
    {
        var sysClassInput = "/sys/class/input";

        if (!Directory.Exists(sysClassInput))
        {
            Console.Error.WriteLine("/sys/class/input does not exist.");
            return;
        }

        foreach (var eventDir in Directory.EnumerateDirectories(sysClassInput, "event*").OrderBy(x => x))
        {
            var eventName = Path.GetFileName(eventDir);
            var devPath = "/dev/input/" + eventName;
            var namePath = Path.Combine(eventDir, "device/name");

            var name = File.Exists(namePath)
                ? File.ReadAllText(namePath).Trim()
                : "(unknown)";

            Console.WriteLine($"{devPath}  {name}");
        }

        Console.WriteLine();
        Console.WriteLine("More stable paths:");
        Console.WriteLine("  ls -l /dev/input/by-id/*event-kbd");
    }

}