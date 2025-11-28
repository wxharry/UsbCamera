/*
 * VideoFrameExtensions.cs
 * Copyright (c) 2019 secile
 * This software is released under the MIT license.
 * see https://opensource.org/licenses/MIT
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace UsbCamera.WinForms
{
    /// <summary>
    /// Extension methods to convert VideoFrame to System.Drawing.Bitmap.
    /// </summary>
    public static class VideoFrameExtensions
    {
        /// <summary>
        /// Converts a VideoFrame to a System.Drawing.Bitmap.
        /// Creates a new Bitmap instance each time - caller is responsible for disposal.
        /// </summary>
        public static Bitmap ToBitmap(this VideoFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            var pixelFormat = frame.Format switch
            {
                PixelFormat.Bgr24 => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                PixelFormat.Gray8 => System.Drawing.Imaging.PixelFormat.Format24bppRgb, // Convert to RGB
                PixelFormat.Gray16 => System.Drawing.Imaging.PixelFormat.Format24bppRgb, // Convert to RGB
                _ => throw new NotSupportedException($"Pixel format {frame.Format} is not supported.")
            };

            var bitmap = new Bitmap(frame.Width, frame.Height, pixelFormat);

            try
            {
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, frame.Width, frame.Height),
                    ImageLockMode.WriteOnly,
                    pixelFormat
                );

                try
                {
                    if (frame.Format == PixelFormat.Bgr24)
                    {
                        CopyBgr24Data(frame, bmpData);
                    }
                    else if (frame.Format == PixelFormat.Gray8)
                    {
                        CopyGray8Data(frame, bmpData);
                    }
                    else if (frame.Format == PixelFormat.Gray16)
                    {
                        CopyGray16Data(frame, bmpData);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        private static void CopyBgr24Data(VideoFrame frame, BitmapData bmpData)
        {
            // Copy from bottom-up format (frame data is already bottom-up)
            for (int y = 0; y < frame.Height; y++)
            {
                var srcIdx = frame.Data.Length - (frame.Stride * (y + 1));
                var dstPtr = IntPtr.Add(bmpData.Scan0, bmpData.Stride * y);
                Marshal.Copy(frame.Data, srcIdx, dstPtr, frame.Stride);
            }
        }

        private static void CopyGray8Data(VideoFrame frame, BitmapData bmpData)
        {
            // Convert Gray8 to RGB24
            var buffer = new byte[bmpData.Stride * frame.Height];
            
            int srcIdx = 0;
            for (int y = 0; y < frame.Height; y++)
            {
                int dstIdx = y * bmpData.Stride;
                for (int x = 0; x < frame.Width; x++)
                {
                    byte gray = frame.Data[srcIdx++];
                    buffer[dstIdx++] = gray; // B
                    buffer[dstIdx++] = gray; // G
                    buffer[dstIdx++] = gray; // R
                }
            }

            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
        }

        private static void CopyGray16Data(VideoFrame frame, BitmapData bmpData)
        {
            // Convert Gray16 to RGB24 (assuming 12-bit depth for now)
            var buffer = new byte[bmpData.Stride * frame.Height];
            const int bits = 12;
            const int pad = 16 - bits;
            const int max = (1 << bits) - 1;

            int srcIdx = 0;
            for (int y = 0; y < frame.Height; y++)
            {
                int dstIdx = y * bmpData.Stride;
                for (int x = 0; x < frame.Width; x++)
                {
                    byte lo = frame.Data[srcIdx++];
                    byte hi = frame.Data[srcIdx++];
                    ushort value = (ushort)(((hi << 8) + lo) >> pad);
                    byte gray = (byte)(value * byte.MaxValue / max);
                    
                    buffer[dstIdx++] = gray; // B
                    buffer[dstIdx++] = gray; // G
                    buffer[dstIdx++] = gray; // R
                }
            }

            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
        }
    }
}
