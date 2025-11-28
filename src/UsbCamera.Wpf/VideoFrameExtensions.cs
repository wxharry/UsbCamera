/*
 * VideoFrameExtensions.cs
 * Copyright (c) 2019 secile
 * This software is released under the MIT license.
 * see https://opensource.org/licenses/MIT
 */

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UsbCamera.Wpf
{
    /// <summary>
    /// Extension methods to convert VideoFrame to WPF BitmapSource.
    /// </summary>
    public static class VideoFrameExtensions
    {
        private const double Dpi = 96.0;

        /// <summary>
        /// Converts a VideoFrame to a frozen BitmapSource.
        /// Creates a new BitmapSource instance each time.
        /// </summary>
        public static BitmapSource ToBitmapSource(this VideoFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            var pixelFormat = frame.Format switch
            {
                PixelFormat.Bgr24 => PixelFormats.Bgr24,
                PixelFormat.Gray8 => PixelFormats.Gray8,
                PixelFormat.Gray16 => PixelFormats.Gray16,
                _ => throw new NotSupportedException($"Pixel format {frame.Format} is not supported.")
            };

            var bitmap = new WriteableBitmap(frame.Width, frame.Height, Dpi, Dpi, pixelFormat, null);
            
            // Copy data accounting for bottom-up format
            if (frame.Format == PixelFormat.Bgr24)
            {
                CopyBgr24Data(frame, bitmap);
            }
            else
            {
                // Gray8 and Gray16 are top-down
                bitmap.WritePixels(
                    new Int32Rect(0, 0, frame.Width, frame.Height),
                    frame.Data,
                    frame.Stride,
                    0
                );
            }

            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// Converts a VideoFrame to a WriteableBitmap, reusing an existing bitmap if provided.
        /// This minimizes allocations for high-frequency frame updates.
        /// </summary>
        /// <param name="frame">Source video frame.</param>
        /// <param name="target">Optional existing WriteableBitmap to reuse. Must match frame dimensions.</param>
        /// <returns>Updated WriteableBitmap (either the reused target or a new instance).</returns>
        public static WriteableBitmap ToWriteableBitmap(this VideoFrame frame, WriteableBitmap? target = null)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            var pixelFormat = frame.Format switch
            {
                PixelFormat.Bgr24 => PixelFormats.Bgr24,
                PixelFormat.Gray8 => PixelFormats.Gray8,
                PixelFormat.Gray16 => PixelFormats.Gray16,
                _ => throw new NotSupportedException($"Pixel format {frame.Format} is not supported.")
            };

            // Create new bitmap if target is null or doesn't match dimensions
            if (target == null || 
                target.PixelWidth != frame.Width || 
                target.PixelHeight != frame.Height ||
                target.Format != pixelFormat)
            {
                target = new WriteableBitmap(frame.Width, frame.Height, Dpi, Dpi, pixelFormat, null);
            }

            // Update the bitmap on the UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                target.Lock();
                try
                {
                    if (frame.Format == PixelFormat.Bgr24)
                    {
                        CopyBgr24DataToLocked(frame, target);
                    }
                    else
                    {
                        // Direct copy for Gray8/Gray16
                        unsafe
                        {
                            var src = frame.Data;
                            var dst = (byte*)target.BackBuffer;
                            for (int y = 0; y < frame.Height; y++)
                            {
                                System.Runtime.InteropServices.Marshal.Copy(
                                    src, 
                                    y * frame.Stride,
                                    (IntPtr)(dst + y * target.BackBufferStride),
                                    frame.Stride
                                );
                            }
                        }
                    }
                    target.AddDirtyRect(new Int32Rect(0, 0, frame.Width, frame.Height));
                }
                finally
                {
                    target.Unlock();
                }
            });

            return target;
        }

        private static void CopyBgr24Data(VideoFrame frame, WriteableBitmap bitmap)
        {
            var buffer = new byte[frame.Height * bitmap.BackBufferStride];
            
            // Convert from bottom-up to top-down
            for (int y = 0; y < frame.Height; y++)
            {
                var srcIdx = frame.Data.Length - (frame.Stride * (y + 1));
                var dstIdx = y * bitmap.BackBufferStride;
                Buffer.BlockCopy(frame.Data, srcIdx, buffer, dstIdx, frame.Stride);
            }

            bitmap.WritePixels(
                new Int32Rect(0, 0, frame.Width, frame.Height),
                buffer,
                bitmap.BackBufferStride,
                0
            );
        }

        private static unsafe void CopyBgr24DataToLocked(VideoFrame frame, WriteableBitmap bitmap)
        {
            var dst = (byte*)bitmap.BackBuffer;
            
            // Convert from bottom-up to top-down
            for (int y = 0; y < frame.Height; y++)
            {
                var srcIdx = frame.Data.Length - (frame.Stride * (y + 1));
                var dstPtr = dst + (y * bitmap.BackBufferStride);
                System.Runtime.InteropServices.Marshal.Copy(
                    frame.Data,
                    srcIdx,
                    (IntPtr)dstPtr,
                    frame.Stride
                );
            }
        }
    }
}
