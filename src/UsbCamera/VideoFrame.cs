/*
 * VideoFrame.cs
 * Copyright (c) 2019 secile
 * This software is released under the MIT license.
 * see https://opensource.org/licenses/MIT
 */

using System;

namespace UsbCamera
{
    /// <summary>
    /// Represents a captured video frame with raw pixel data.
    /// This is a framework-agnostic representation that can be converted to platform-specific image types.
    /// </summary>
    public class VideoFrame
    {
        /// <summary>Frame width in pixels.</summary>
        public int Width { get; }

        /// <summary>Frame height in pixels.</summary>
        public int Height { get; }

        /// <summary>Stride (bytes per row) of the frame buffer.</summary>
        public int Stride { get; }

        /// <summary>Pixel format of the frame data.</summary>
        public PixelFormat Format { get; }

        /// <summary>Raw pixel data buffer.</summary>
        public byte[] Data { get; }

        /// <summary>Timestamp when the frame was captured.</summary>
        public double Timestamp { get; }

        public VideoFrame(int width, int height, int stride, PixelFormat format, byte[] data, double timestamp = 0)
        {
            Width = width;
            Height = height;
            Stride = stride;
            Format = format;
            Data = data;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Creates a copy of the frame data to prevent modification of the original buffer.
        /// </summary>
        public VideoFrame Clone()
        {
            var dataCopy = new byte[Data.Length];
            Buffer.BlockCopy(Data, 0, dataCopy, 0, Data.Length);
            return new VideoFrame(Width, Height, Stride, Format, dataCopy, Timestamp);
        }
    }

    /// <summary>
    /// Pixel format of video frame data.
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>24-bit RGB, bottom-up (standard Windows bitmap format).</summary>
        Bgr24,
        
        /// <summary>8-bit grayscale (Y800/Y8).</summary>
        Gray8,
        
        /// <summary>16-bit grayscale (Y16).</summary>
        Gray16
    }
}
