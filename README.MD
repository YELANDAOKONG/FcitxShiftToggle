# Fcitx5RShiftToggle

一个用于临时缓解 `fcitx5` 异常问题的小工具：
通过监听 **右 Shift 单独按下并松开**，快速切换 `fcitx5` 的运行状态。

适合在等待上游修复期间作为临时方案使用。

## 功能

* 监听键盘输入设备
* 单击右 Shift 切换 `fcitx5`
* `fcitx5` 正在运行时执行：

```bash
pkill -x fcitx5
```

* `fcitx5` 未运行时执行：

```bash
fcitx5 -d
```

* 支持作为 systemd 服务常驻运行
* 不依赖 KDE / GNOME / Wayland 全局快捷键
* 不需要修改虚拟键盘设置，KDE 输入法虚拟键盘保持 `None` 即可

## 使用场景

某些环境下，`fcitx5` 可能会导致键盘修饰键状态异常，例如：

* 某些程序中 Ctrl / Shift 状态异常
* 滚轮被识别成缩放
* Backspace / 字符输入异常
* 临时重启 `fcitx5` 后恢复正常

这个工具提供一个快速开关，让用户可以不打开终端、不手动输入命令，直接用右 Shift 单击重启或关闭 `fcitx5`。

## 注意事项

本程序需要读取 `/dev/input/event*` 键盘设备，因此通常需要 root 权限运行。

但是，程序即使以 root 运行，也不会以 root 身份启动 `fcitx5`。
启动 `fcitx5` 时会通过 `--user` 指定普通用户身份。

请不要让 `fcitx5` 以 root 用户运行。

## 构建

需要安装 .NET SDK。

```bash
dotnet build -c Release
```

发布单文件：

```bash
dotnet publish -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true
```

生成的可执行文件通常位于：

```bash
bin/Release/net*/linux-x64/publish/Fcitx5RShiftToggle
```

## 查找键盘设备

推荐使用 `/dev/input/by-id/` 下的稳定路径，而不是直接使用 `/dev/input/eventX`。

```bash
ls -l /dev/input/by-id/*event-kbd
```

也可以使用程序自带的设备列表功能：

```bash
sudo ./Fcitx5RShiftToggle --list
```

示例输出：

```text
/dev/input/event4  AT Translated Set 2 keyboard
/dev/input/event7  USB Gaming Keyboard
```

更推荐使用类似这样的路径：

```text
/dev/input/by-id/usb-Your_Keyboard_Name-event-kbd
```

因为 `/dev/input/eventX` 的编号在重启或插拔设备后可能变化。

## 运行

```bash
sudo ./Fcitx5RShiftToggle \
  --device /dev/input/by-id/your-keyboard-event-kbd \
  --user your-user-name
```

示例：

```bash
sudo ./Fcitx5RShiftToggle \
  --device /dev/input/by-id/usb-Example_Keyboard-event-kbd \
  --user yourname
```

运行后：

* 单独按下并松开右 Shift：切换 `fcitx5`
* 右 Shift + 其他按键：不会触发切换
* Ctrl+C：退出程序

## 单次切换

也可以不监听键盘，只执行一次切换：

```bash
./Fcitx5RShiftToggle --toggle-once
```

以 root 运行时建议指定用户：

```bash
sudo ./Fcitx5RShiftToggle --toggle-once --user your-user-name
```

## systemd 服务

将程序复制到 `/opt`：

```bash
sudo mkdir -p /opt/fcitx5-rshift-toggle
sudo cp ./Fcitx5RShiftToggle /opt/fcitx5-rshift-toggle/
sudo chmod +x /opt/fcitx5-rshift-toggle/Fcitx5RShiftToggle
```

创建服务文件：

```bash
sudo nano /etc/systemd/system/fcitx5-rshift-toggle.service
```

内容示例：

```ini
[Unit]
Description=Right Shift tap toggles fcitx5
After=graphical.target

[Service]
Type=simple
ExecStart=/opt/fcitx5-rshift-toggle/Fcitx5RShiftToggle --device /dev/input/by-id/your-keyboard-event-kbd --user your-user-name
Restart=on-failure
RestartSec=1

[Install]
WantedBy=multi-user.target
```

启用并启动：

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now fcitx5-rshift-toggle.service
```

查看日志：

```bash
journalctl -u fcitx5-rshift-toggle.service -f
```

停止服务：

```bash
sudo systemctl stop fcitx5-rshift-toggle.service
```

禁用服务：

```bash
sudo systemctl disable fcitx5-rshift-toggle.service
```

## 为什么不是 KDE 全局快捷键？

KDE Wayland 下，普通程序不一定能可靠注册或拦截全局快捷键。
而本工具直接读取 Linux input event 设备，因此不依赖桌面环境的全局快捷键系统。

这对于临时修复输入法导致的键盘状态异常更直接。

## 为什么不真正拦截右 Shift？

本工具默认只是监听右 Shift，而不是阻止它传递给系统。

真正拦截单个按键通常需要：

1. grab 整个键盘设备；
2. 自己过滤按键；
3. 通过 uinput 重新注入其他按键。

这种方式风险更高。程序崩溃或逻辑错误时，可能导致键盘暂时不可用。

因此当前设计更保守：

* 单击右 Shift：触发切换
* 右 Shift 仍然正常传给系统
* 右 Shift 作为组合键使用时不会触发切换

## 安全说明

本程序需要读取键盘输入设备。
这意味着它具备观察键盘事件的能力，因此请只运行自己信任的构建版本。

推荐做法：

* 从源码自行构建
* 只监听自己的键盘设备
* 不要随意使用来路不明的二进制文件
* 不要以 root 身份启动 `fcitx5`

## 常见问题

### Permission denied

说明当前用户没有权限读取 `/dev/input/event*`。

可以使用：

```bash
sudo ./Fcitx5RShiftToggle --device ... --user ...
```

或者自行配置 udev 规则 / input 组权限。

### 按右 Shift 没反应

检查是否选错键盘设备。

可以重新列出设备：

```bash
sudo ./Fcitx5RShiftToggle --list
```

也可以查看稳定设备路径：

```bash
ls -l /dev/input/by-id/*event-kbd
```

如果有多个键盘，需要选择实际正在使用的那个。

### 启动 fcitx5 失败

确认普通用户环境存在：

```bash
ls /run/user/$(id -u your-user-name)
```

确认 DBus session bus 存在：

```bash
ls /run/user/$(id -u your-user-name)/bus
```

确认 `fcitx5` 命令可用：

```bash
which fcitx5
```

### KDE 虚拟键盘需要改吗？

不需要。
KDE 输入法 / 虚拟键盘那里保持 `None` 即可。

## License

* MIT
