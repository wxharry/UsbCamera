# UsbCamera
![NuGet Version](https://img.shields.io/nuget/v/usbcamera.core?color=darkviolet)

Windows USB camera capture library using DirectShow, with a framework-agnostic core and adapters for WPF and WinForms.

What’s included
- Core library: `UsbCamera` (multi-targets `net472` and `net6.0-windows7.0`)
- WPF adapter: `UsbCamera.Wpf` (helper extensions for `BitmapSource`)
- WinForms adapter: `UsbCamera.WinForms` (helper extensions for `System.Drawing.Bitmap`)
- Samples: WinForms and WPF demo apps
- CI/CD: Windows builds via MSBuild, tag-triggered GitHub Releases, optional NuGet publish

## Install
NuGet packages (published on tag releases):
- `UsbCamera` — core
- `UsbCamera.Wpf` — WPF helpers
- `UsbCamera.WinForms` — WinForms helpers

## Quick start (WinForms)
```csharp
using UsbCamera;

// enumerate devices
var devices = UsbCamera.FindDevices();
if (devices.Length == 0) return;

var index = 0;
var formats = UsbCamera.GetVideoFormat(index);
var format = formats[0];

var cam = new UsbCamera(index, format);
this.FormClosing += (s, e) => cam.Release();

// preview into a WinForms control
cam.SetPreviewControl(pictureBox1.Handle, pictureBox1.ClientSize);
pictureBox1.Resize += (s, e) => cam.SetPreviewSize(pictureBox1.ClientSize);

cam.Start();
var bmp = cam.GetBitmap(); // System.Drawing.Bitmap
```

## Quick start (WPF)
Use the WPF sample, or host a `PictureBox` via `WindowsFormsHost` for light preview, or convert frames with the `UsbCamera.Wpf` adapter to `BitmapSource`.

## Packages and Releases
- CI builds Debug/Release with MSBuild on Windows.
- Tag `vX.Y.Z` to trigger Release workflow:
  - Builds the solution
  - Packs NuGet for `UsbCamera`, `UsbCamera.Wpf`, `UsbCamera.WinForms`
  - Attaches `.nupkg`/`.snupkg` files and a zipped core `bin\Release` to the GitHub Release
  - Optional: pushes to NuGet when `NUGET_API_KEY` is configured

## Notes
- The legacy single-file “drop-in” guidance was replaced by SDK-style projects and NuGet packages.
- Some advanced features (still image capture, grayscale Y8/Y16 pipelines) are supported in the core but may require adapter-specific handling.

## Samples
See `samples/UsbCameraForms` and `samples/UsbCameraWpf` for end-to-end usage.
