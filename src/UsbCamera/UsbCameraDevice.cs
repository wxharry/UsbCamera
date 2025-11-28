/*
 * UsbCameraDevice.cs
 * Copyright (c) 2019 secile
 * This software is released under the MIT license.
 * see https://opensource.org/licenses/MIT
 */

using System;
using System.Linq;
using UsbCameraLegacy = UsbCamera.Net.UsbCamera;
using LegacyVideoFormat = UsbCamera.Net.UsbCamera.VideoFormat;

namespace UsbCamera
{
    /// <summary>
    /// Provides access to USB cameras via DirectShow with framework-agnostic VideoFrame output.
    /// This is a wrapper around the legacy UsbCamera.Net implementation.
    /// </summary>
    public class UsbCameraDevice : IDisposable
    {
        private readonly UsbCameraLegacy _camera;
        private readonly int _width;
        private readonly int _height;
        private readonly int _stride;
        private readonly PixelFormat _pixelFormat;

        /// <summary>USB camera image size.</summary>
        public System.Drawing.Size Size => _camera.Size;

        /// <summary>True if image buffer is ready and frames can be retrieved.</summary>
        public bool IsReady => _camera.IsReady;

        /// <summary>Camera supports still image capture.</summary>
        public bool StillImageAvailable => _camera.StillImageAvailable;

        /// <summary>Properties that can be adjusted (exposure, white balance, etc.).</summary>
        public UsbCameraLegacy.PropertyItems Properties => _camera.Properties;

        /// <summary>
        /// Called when preview frame captured.
        /// Note: Called by worker thread.
        /// </summary>
        public event Action<VideoFrame>? PreviewFrameCaptured;

        /// <summary>
        /// Called when still image captured by hardware button or software trigger.
        /// Note: Called by worker thread.
        /// </summary>
        public event Action<VideoFrame>? StillImageCaptured;

        /// <summary>
        /// Get available USB camera list.
        /// </summary>
        public static string[] FindDevices()
        {
            return UsbCameraLegacy.FindDevices();
        }

        /// <summary>
        /// Get video formats supported by the camera.
        /// </summary>
        public static VideoFormat[] GetVideoFormats(int cameraIndex)
        {
            var legacyFormats = UsbCameraLegacy.GetVideoFormat(cameraIndex);
            return legacyFormats.Select(f => new VideoFormat
            {
                Width = f.Size.Width,
                Height = f.Size.Height,
                TimePerFrame = f.TimePerFrame,
                SubType = f.SubType
            }).ToArray();
        }

        /// <summary>
        /// Create USB Camera with specified format.
        /// </summary>
        public UsbCameraDevice(int cameraIndex, VideoFormat format)
        {
            var legacyFormat = new LegacyVideoFormat
            {
                Size = new System.Drawing.Size(format.Width, format.Height),
                TimePerFrame = format.TimePerFrame,
                SubType = format.SubType
            };

            _camera = new UsbCameraLegacy(cameraIndex, legacyFormat);
            _width = _camera.Size.Width;
            _height = _camera.Size.Height;

            // Determine pixel format and stride
            if (format.SubType?.Contains("Y800") == true || format.SubType?.Contains("Y8") == true)
            {
                _pixelFormat = PixelFormat.Gray8;
                _stride = _width;
            }
            else if (format.SubType?.Contains("Y16") == true)
            {
                _pixelFormat = PixelFormat.Gray16;
                _stride = _width * 2;
            }
            else
            {
                _pixelFormat = PixelFormat.Bgr24;
                _stride = _width * 3;
            }

            // Wire up legacy bitmap events to VideoFrame events
            _camera.PreviewCaptured += OnLegacyPreviewCaptured;
            _camera.StillImageCaptured += OnLegacyStillImageCaptured;
        }

        /// <summary>
        /// Create USB Camera with specified size (uses default format).
        /// </summary>
        public UsbCameraDevice(int cameraIndex, int width, int height)
            : this(cameraIndex, new VideoFormat { Width = width, Height = height })
        {
        }

        private void OnLegacyPreviewCaptured(object bitmap)
        {
            if (PreviewFrameCaptured == null) return;

            var frame = ConvertToVideoFrame(bitmap);
            if (frame != null)
            {
                PreviewFrameCaptured(frame);
            }
        }

        private void OnLegacyStillImageCaptured(object bitmap)
        {
            if (StillImageCaptured == null) return;

            var frame = ConvertToVideoFrame(bitmap);
            if (frame != null)
            {
                StillImageCaptured(frame);
            }
        }

        private VideoFrame? ConvertToVideoFrame(object bitmap)
        {
            // The legacy library returns byte[] when built without symbols
            if (bitmap is System.Collections.Generic.IEnumerable<byte> bytes)
            {
                var data = (bytes as byte[]) ?? bytes.ToArray();
                return new VideoFrame(_width, _height, _stride, _pixelFormat, data);
            }

            // Handle System.Drawing.Bitmap
            if (bitmap is System.Drawing.Bitmap bmp)
            {
                return ConvertBitmapToFrame(bmp);
            }

            return null;
        }

        private VideoFrame ConvertBitmapToFrame(System.Drawing.Bitmap bmp)
        {
            var bmpData = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb
            );

            try
            {
                var data = new byte[Math.Abs(bmpData.Stride) * bmp.Height];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, data, 0, data.Length);
                
                return new VideoFrame(
                    bmp.Width,
                    bmp.Height,
                    Math.Abs(bmpData.Stride),
                    PixelFormat.Bgr24,
                    data
                );
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
        }

        /// <summary>
        /// Get current frame as VideoFrame.
        /// </summary>
        public VideoFrame? GetFrame()
        {
            if (!IsReady) return null;

            var bitmap = _camera.GetBitmap();
            return ConvertToVideoFrame(bitmap);
        }

        /// <summary>Start camera capture.</summary>
        public void Start() => _camera.Start();

        /// <summary>Stop camera capture.</summary>
        public void Stop() => _camera.Stop();

        /// <summary>Trigger still image capture (if supported).</summary>
        public void TriggerStillImage() => _camera.StillImageTrigger();

        /// <summary>
        /// Set preview control (for native DirectShow rendering).
        /// </summary>
        public void SetPreviewControl(IntPtr handle, System.Drawing.Size size)
        {
            _camera.SetPreviewControl(handle, size);
        }

        /// <summary>Set preview size.</summary>
        public void SetPreviewSize(System.Drawing.Size size)
        {
            _camera.SetPreviewSize(size);
        }

        public void Dispose()
        {
            _camera?.Release();
        }
    }

    /// <summary>
    /// Video format information.
    /// </summary>
    public class VideoFormat
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long TimePerFrame { get; set; }
        public int Fps => TimePerFrame == 0 ? 0 : (int)(10000000 / TimePerFrame);
        public string? SubType { get; set; }

        public override string ToString()
        {
            return $"{Width}x{Height} @ {Fps}fps ({SubType})";
        }
    }
}
