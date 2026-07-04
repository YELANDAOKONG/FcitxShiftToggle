namespace Fcitx5RShiftToggle;

public sealed class RightShiftListener
{
    private readonly string _devicePath;
    private readonly Fcitx5Controller _controller;

    private bool _rightShiftDown;
    private bool _otherKeyPressedWhileRightShiftDown;
    private DateTimeOffset _lastToggleTime = DateTimeOffset.MinValue;

    public RightShiftListener(string devicePath, Fcitx5Controller controller)
    {
        _devicePath = devicePath;
        _controller = controller;
    }

    public void Run()
    {
        using var stream = new FileStream(
            _devicePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 24,
            FileOptions.None);

        Console.WriteLine("Listening... Press Ctrl+C to exit.");
        Console.WriteLine();

        Span<byte> buffer = stackalloc byte[24];

        while (true)
        {
            if (!ReadExactly(stream, buffer))
                break;

            // Linux x86_64 struct input_event:
            // timeval: 16 bytes
            // type:    ushort at offset 16
            // code:    ushort at offset 18
            // value:   int    at offset 20
            var type = BitConverter.ToUInt16(buffer.Slice(16, 2));
            var code = BitConverter.ToUInt16(buffer.Slice(18, 2));
            var value = BitConverter.ToInt32(buffer.Slice(20, 4));

            if (type != Program.EV_KEY)
                continue;

            HandleKeyEvent(code, value);
        }
    }

    private void HandleKeyEvent(ushort code, int value)
    {
        // value:
        // 0 = key up
        // 1 = key down
        // 2 = repeat

        var isKeyDown = value == 1;
        var isKeyUp = value == 0;

        if (code == Program.KEY_RIGHTSHIFT)
        {
            if (isKeyDown)
            {
                _rightShiftDown = true;
                _otherKeyPressedWhileRightShiftDown = false;
            }
            else if (isKeyUp)
            {
                if (_rightShiftDown && !_otherKeyPressedWhileRightShiftDown)
                {
                    var now = DateTimeOffset.UtcNow;

                    // Debounce.
                    if (now - _lastToggleTime > TimeSpan.FromMilliseconds(350))
                    {
                        _lastToggleTime = now;
                        _controller.Toggle();
                    }
                }

                _rightShiftDown = false;
                _otherKeyPressedWhileRightShiftDown = false;
            }

            return;
        }

        if (_rightShiftDown && isKeyDown)
        {
            // Avoid toggling when using Right Shift as a normal modifier,
            // for example RightShift + A.
            _otherKeyPressedWhileRightShiftDown = true;
        }
    }

    private static bool ReadExactly(Stream stream, Span<byte> buffer)
    {
        var total = 0;

        while (total < buffer.Length)
        {
            var read = stream.Read(buffer.Slice(total));

            if (read == 0)
                return false;

            total += read;
        }

        return true;
    }
}