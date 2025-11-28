# Migration Guide: UsbCamera.Net to New Architecture

## Overview

The UsbCamera library has been restructured to eliminate conditional compilation symbols (`USBCAMERA_WPF`, `USBCAMERA_BYTEARRAY`) and provide a cleaner, more maintainable API for NuGet distribution.

## New Package Structure

### Core Package: `UsbCamera`
- Framework-agnostic video capture library
- Returns `VideoFrame` objects containing raw pixel data
- Supports .NET Framework 4.7.2 and .NET 6.0+

### WPF Adapter: `UsbCamera.Wpf`
- Extension methods to convert `VideoFrame` to WPF image types
- `ToBitmapSource()` - Creates frozen BitmapSource (immutable)
- `ToWriteableBitmap()` - Reusable WriteableBitmap for high-performance updates
- Supports .NET Framework 4.7.2 and .NET 6.0+ with WPF

### WinForms Adapter: `UsbCamera.WinForms`
- Extension method to convert `VideoFrame` to System.Drawing.Bitmap
- `ToBitmap()` - Creates GDI+ Bitmap
- Supports .NET Framework 4.7.2 and .NET 6.0+

## Installation

### For WPF Applications
```powershell
Install-Package UsbCamera
Install-Package UsbCamera.Wpf
```

### For WinForms Applications
```powershell
Install-Package UsbCamera
Install-Package UsbCamera.WinForms
```

## Migration Examples

### Old API (Conditional Compilation - DEPRECATED)

```csharp
using UsbCamera.Net;

// Old way - returns System.Drawing.Bitmap or BitmapSource depending on build symbols
var camera = new UsbCamera();
camera.Start();
camera.GetBitmap += (sender, bitmap) => {
    // Type depends on USBCAMERA_WPF symbol
    PreviewImage = bitmap; 
};
```

### New API - WPF

```csharp
using UsbCamera;
using UsbCamera.Wpf;

// New way - explicit VideoFrame with WPF extensions
var camera = new UsbCameraDevice();
camera.FrameReceived += (sender, frame) => {
    // Convert VideoFrame to BitmapSource
    var bitmapSource = frame.ToBitmapSource();
    PreviewImage = bitmapSource;
};
camera.Start();
```

**High-Performance WPF (Reusable WriteableBitmap):**
```csharp
WriteableBitmap? writeableBitmap = null;

camera.FrameReceived += (sender, frame) => {
    if (writeableBitmap == null || 
        writeableBitmap.PixelWidth != frame.Width || 
        writeableBitmap.PixelHeight != frame.Height)
    {
        writeableBitmap = frame.ToWriteableBitmap();
        PreviewImage = writeableBitmap;
    }
    else
    {
        frame.ToWriteableBitmap(writeableBitmap);
    }
};
```

### New API - WinForms

```csharp
using UsbCamera;
using UsbCamera.WinForms;

var camera = new UsbCameraDevice();
camera.FrameReceived += (sender, frame) => {
    var bitmap = frame.ToBitmap();
    pictureBox.Image?.Dispose();
    pictureBox.Image = bitmap;
};
camera.Start();
```

## Key Differences

| Aspect | Old API | New API |
|--------|---------|---------|
| Base Type | `UsbCamera.Net.UsbCamera` | `UsbCamera.UsbCameraDevice` |
| Frame Event | `GetBitmap` (platform-specific) | `FrameReceived` (VideoFrame) |
| Frame Type | Bitmap/BitmapSource (build-dependent) | `VideoFrame` (always same) |
| Conversion | Implicit/conditional | Explicit extension methods |
| NuGet Support | Poor (requires consumer to define symbols) | Excellent (consumer chooses adapter) |

## VideoFrame Structure

```csharp
public class VideoFrame
{
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public PixelFormat Format { get; }
    public byte[] Data { get; }
    public DateTime Timestamp { get; }
}

public enum PixelFormat
{
    Bgr24,   // 24-bit BGR (standard RGB cameras)
    Gray8,   // 8-bit grayscale
    Gray16   // 16-bit grayscale
}
```

## Benefits of New Architecture

1. **NuGet-Friendly**: Consumers don't need to define compilation symbols
2. **Explicit Dependencies**: Clear separation between core and UI-specific code
3. **Framework Agnostic**: Core library has no UI dependencies
4. **Type Safety**: No conditional types that change based on build configuration
5. **Performance**: WriteableBitmap reuse option for WPF reduces allocations
6. **Maintainability**: Easier to test and extend
7. **Multi-Platform**: Can target both .NET Framework and modern .NET

## Backward Compatibility

The old `UsbCamera.Net.UsbCamera` class is still available but considered **legacy**. It's recommended to migrate to `UsbCameraDevice` for new projects.

## Build All Packages

```bash
dotnet build src/UsbCamera/UsbCamera.csproj -c Release
dotnet build src/UsbCamera.Wpf/UsbCamera.Wpf.csproj -c Release
dotnet build src/UsbCamera.WinForms/UsbCamera.WinForms.csproj -c Release

# Create NuGet packages
dotnet pack src/UsbCamera/UsbCamera.csproj -c Release
dotnet pack src/UsbCamera.Wpf/UsbCamera.Wpf.csproj -c Release
dotnet pack src/UsbCamera.WinForms/UsbCamera.WinForms.csproj -c Release
```

Packages will be output to `bin/Release/*.nupkg`.
