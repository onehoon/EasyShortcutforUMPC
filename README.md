# Easy Shortcut for UMPC

<img width="1449" height="650" alt="widget" src="https://github.com/user-attachments/assets/d4d165c9-912d-48d4-9080-c378ac266095" />

## Designed for UMPCs and handheld Windows PCs

Easy Shortcut for UMPC is an Xbox Game Bar widget designed for UMPCs and handheld Windows gaming PCs.

The widget provides large touch-friendly shortcut buttons in a compact fixed-width layout, making common keyboard shortcuts easier to use on small Windows handheld screens.

## Features

- Xbox Game Bar widget for quick shortcut access
- Large touch-friendly buttons
- Configurable Lossless Scaling shortcut
- Configurable OptiScaler Overlay shortcut
- Customizable OptiScaler Overlay button name
- Three user-configurable Custom shortcut buttons
- Custom buttons open Settings automatically when they are not set
- Display Resolution buttons for supported internal displays
- Current display resolution and refresh rate shown next to the Display Resolution title
- Configurable top shortcut order
- Configurable vertical layout order
- Mouse and gamepad-friendly Settings access through the Xbox Game Bar widget options menu

## Installation

Easy Shortcut for UMPC is currently distributed through GitHub releases.

1. Go to the latest GitHub release.
2. Download the attached ZIP file.
3. Extract the ZIP file.
4. Run `install-Beta.cmd` by double-clicking it.
5. Open Xbox Game Bar with `Win + G`.
6. Open **Easy Shortcut for UMPC** from the widget list.

## How to use

1. Open Xbox Game Bar with `Win + G`.
2. Open **Easy Shortcut for UMPC** from the widget list.
3. Pin the widget if you want it to stay visible while gaming.
4. Tap the shortcut buttons as needed.
5. Open Settings from the Xbox Game Bar widget options menu to configure shortcuts, button names, and layout.

## Opening Settings

Settings are opened from the Xbox Game Bar widget options menu.

### Mouse

Right-click the **Easy Shortcut for UMPC** widget icon at the top of Xbox Game Bar, then select **Options**.

### Gamepad

Move focus to the widget, press the **Menu button (≡)** on the gamepad, then select **Options**.

### Custom shortcut buttons

If a Custom shortcut button is shown as **Not Set**, pressing that button also opens Settings.

This makes it easy to assign a shortcut even when there is no Settings button inside the main widget.

## Shortcut list

| Button | Default shortcut | Configurable |
|---|---|---|
| Lossless Scaling | `Ctrl + Alt + S` | Yes |
| OptiScaler Overlay | `Insert` | Yes |
| Custom 1 | `Not Set` | Yes |
| Custom 2 | `Not Set` | Yes |
| Custom 3 | `Not Set` | Yes |

> [!NOTE]
> The previous default `OptiScaler Overlay (Alt + Insert)` button has been removed from the main layout.
>
> `Alt + Insert` can still be assigned to the OptiScaler Overlay button or to any Custom shortcut in Settings.

## Main widget layout

The main widget uses a compact layout optimized for UMPC screens.

Default top shortcut order:

```text
[ Lossless Scaling ] [ OptiScaler Overlay ]
```

The top shortcut order can be changed in Settings:

```text
[ OptiScaler Overlay ] [ Lossless Scaling ]
```

Only these two top shortcut buttons support left/right order swapping.

Other rows do not support left/right swapping.

## Settings

The Settings page lets you customize the widget behavior.

You can:

- Change the Lossless Scaling shortcut
- Reset Lossless Scaling back to `Ctrl + Alt + S`
- Change the OptiScaler Overlay shortcut
- Change the OptiScaler Overlay button name
- Reset OptiScaler Overlay back to its default name and shortcut
- Configure Custom 1, Custom 2, and Custom 3
- Reset Custom buttons back to `Not Set`
- Swap the top shortcut order
- Change the vertical layout order of widget sections

## Built-in shortcut settings

### Lossless Scaling

The Lossless Scaling button sends the following shortcut by default:

```text
Ctrl + Alt + S
```

You can change this shortcut in Settings.

### OptiScaler Overlay

The OptiScaler Overlay button sends the following shortcut by default:

```text
Insert
```

You can change this shortcut in Settings.

You can also change the button name. For example:

```text
OptiScaler Overlay
Overlay
Frame Gen Overlay
```

Changing the OptiScaler Overlay button name only changes the button label inside the widget.

The app name remains **Easy Shortcut for UMPC**.

## Top Shortcut Order

The top shortcut row contains:

```text
Lossless Scaling
OptiScaler Overlay
```

Settings provides a separate **Top Shortcut Order** option.

Available orders:

```text
Lossless Scaling / OptiScaler Overlay
OptiScaler Overlay / Lossless Scaling
```

This setting only changes the left/right order of the top shortcut row.

It does not affect:

- Display Resolution buttons
- Custom buttons
- Vertical Layout Order
- App name
- Widget name

## Custom shortcuts

Easy Shortcut for UMPC includes three customizable shortcut buttons.

Each Custom button can be assigned a modifier and a key, such as:

```text
Ctrl + Alt + X
Shift + F1
Alt + Enter
Alt + Insert
Insert
Home
End
```

If a Custom button is not configured, it is shown as:

```text
Not Set
```

When a **Not Set** Custom button is pressed, the Settings page opens so you can assign a shortcut.

### How to configure Custom shortcuts

1. Open Xbox Game Bar with `Win + G`.
2. Open **Easy Shortcut for UMPC**.
3. Open Settings from the Xbox Game Bar widget options menu.
4. Choose a modifier and key for Custom 1, Custom 2, or Custom 3.
5. Press **Save**.

### Reset behavior

| Item | Reset behavior |
|---|---|
| Lossless Scaling | Restores `Ctrl + Alt + S` |
| OptiScaler Overlay shortcut | Restores `Insert` |
| OptiScaler Overlay name | Restores `OptiScaler Overlay` |
| Custom 1 | Resets to `Not Set` |
| Custom 2 | Resets to `Not Set` |
| Custom 3 | Resets to `Not Set` |
| Top Shortcut Order | Restores `Lossless Scaling / OptiScaler Overlay` |

### Supported modifiers

- None
- Ctrl
- Alt
- Shift
- Ctrl + Alt
- Ctrl + Shift
- Alt + Shift
- Ctrl + Alt + Shift

### Supported keys

Custom shortcuts support common keyboard keys, including:

- A-Z
- 0-9
- F1-F12
- Insert
- Delete
- Home
- End
- Page Up
- Page Down
- Space
- Tab
- Escape
- Arrow keys

## Layout Order

The widget sections can be reordered vertically in Settings.

Available sections:

- Top Shortcuts
- Display Resolution
- Custom

This changes the vertical order of the widget sections.

It does not change the left/right order inside each section.

The left/right order of the top row is controlled separately by **Top Shortcut Order**.

## Display Resolution

The Display Resolution feature is intended for UMPCs and handheld Windows PCs using the internal display.

The widget can show the current display resolution and refresh rate next to the Display Resolution title.

Example:

```text
Display Resolution        1920x1200p @120Hz
```

Resolution buttons are shown only when the device is using its internal display.

For compatibility and stability reasons, the resolution buttons are hidden when an external monitor is connected.

Available resolution options depend on the detected internal display resolution and the modes supported by the device.

| Internal display group | Available options |
|---|---|
| 1200p | 1200p, 1080p, 1050p, 900p |
| 1080p | 1080p, 900p, 720p |

> [!NOTE]
> The resolution-switching feature is intended for UMPC standalone mode.
>
> On desktop PCs or devices using an external display, the resolution buttons will not be shown.
>
> The widget only shows resolution options detected as supported on the current device.

## Notes

> [!NOTE]
> Some games or applications may block simulated keyboard input.
>
> Some system-level apps or elevated/admin apps may not respond to shortcuts from the widget.
>
> Display resolution controls are available only on supported internal displays.
>
> Resolution controls are hidden when an external display is detected.
>
> Xbox Game Bar must be enabled.

## Requirements

- Windows 10/11 with Xbox Game Bar support
- Xbox Game Bar enabled
- Sideloading enabled if required by your Windows settings

## License

Easy Shortcut for UMPC is released under the GNU General Public License v3.0.

You may use, modify, and redistribute this project under the terms of the GPL-3.0 license.

If you distribute modified versions or binaries, you must also provide the corresponding source code under the same license.

[![Latest release downloads](https://img.shields.io/github/downloads/onehoon/EasyShortcutforUMPC/latest/total?label=latest%20downloads)](https://github.com/onehoon/EasyShortcutforUMPC/releases/latest)
[![Total downloads](https://img.shields.io/github/downloads/onehoon/EasyShortcutforUMPC/total?label=total%20downloads)](https://github.com/onehoon/EasyShortcutforUMPC/releases)
[![GitHub release](https://img.shields.io/github/v/release/onehoon/EasyShortcutforUMPC?label=latest%20release)](https://github.com/onehoon/EasyShortcutforUMPC/releases/latest)
