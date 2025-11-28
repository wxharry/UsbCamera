using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;         // PixelFormats
using System.Windows.Media.Imaging; // BitmapSource
using UsbCamera;                    // UsbCameraDevice, VideoFormat
using UsbCamera.Wpf;                // VideoFrame WPF extensions

namespace UsbCameraWpf
{
    class MainWindowViewModel : ViewModel
    {
        private BitmapSource _Preview;
        public BitmapSource Preview
        {
            get { return _Preview; }
            set
            {
                if (_Preview != value)
                    _Preview = value;
                OnPropertyChanged();
            }
        }

        private BitmapSource _Capture;
        public BitmapSource Capture
        {
            get { return _Capture; }
            set
            {
                _Capture = value;
                OnPropertyChanged();
            }
        }

        public ICommand GetBitmap { get; private set; }

        public ICommand GetStillImage { get; private set; }

        public MainWindowViewModel()
        {
            // Find devices via new core wrapper
            var devices = UsbCameraDevice.FindDevices();
            if (devices.Length == 0) return; // no device

            var cameraIndex = 0;
            var formats = UsbCameraDevice.GetVideoFormats(cameraIndex);
            for (int i = 0; i < formats.Length; i++) Console.WriteLine("{0}:{1}", i, formats[i]);
            var format = formats.First();

            // Create device using new VideoFrame-based API
            var camera = new UsbCameraDevice(cameraIndex, format);

            // High-performance preview: reuse WriteableBitmap when size matches
            WriteableBitmap reusable = null;
            camera.PreviewFrameCaptured += frame =>
            {
                // Marshal to UI thread for bitmap updates
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Create once or update in-place
                    if (reusable == null || reusable.PixelWidth != frame.Width || reusable.PixelHeight != frame.Height)
                    {
                        reusable = frame.ToWriteableBitmap();
                        Preview = reusable;
                    }
                    else
                    {
                        frame.ToWriteableBitmap(reusable);
                    }
                }));
            };

            // Start streaming
            camera.Start();

            // Capture current frame on demand
            GetBitmap = new RelayCommand(() =>
            {
                var frame = camera.GetFrame();
                if (frame != null)
                {
                    Capture = frame.ToBitmapSource();
                }
            });

            // Still image support if available
            if (camera.StillImageAvailable)
            {
                GetStillImage = new RelayCommand(() => camera.TriggerStillImage());
                camera.StillImageCaptured += frame =>
                {
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Capture = frame.ToBitmapSource();
                    }));
                };
            }
        }
    }
}
