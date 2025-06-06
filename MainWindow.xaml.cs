using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Windows;
using System.Windows.Threading;
using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Extensions;
using YoloDotNet.Models;
using System.ComponentModel;

namespace WebcamDemo
{
    public partial class MainWindow : Window
    {
        private readonly Yolo _yolo;
        private SKImage _currentFrame = default!;
        private Dispatcher _dispatcher = default!;
        private bool _runDetection = false;
        private SKRect _rect;

        private const int WEBCAM_WIDTH = 1080;
        private const int WEBCAM_HEIGHT = 608;
        private const int FPS = 30;
        private const string FRAME_FORMAT_EXTENSION = ".png";

        public bool IsMirrorEnabled;

        public MainWindow()
        {
            InitializeComponent();

            _yolo = new Yolo(new YoloOptions()
            {
                OnnxModel = @"E:\YoloDotNet\YoloDotNet\test\assets\Models\yolov8s.onnx",
                ModelType = ModelType.ObjectDetection,
                Cuda = false
            });

            _dispatcher = Dispatcher.CurrentDispatcher;
            _currentFrame = SKImage.FromBitmap(new SKBitmap(WEBCAM_WIDTH, WEBCAM_HEIGHT));
            _rect = new SKRect(0, 0, WEBCAM_WIDTH, WEBCAM_HEIGHT);

            Task.Run(WebcamAsync);
        }

        private async Task WebcamAsync()
        {
            using var capture = new VideoCapture(0, VideoCapture.API.DShow);
            capture.Set(CapProp.Fps, FPS);
            capture.Set(CapProp.FrameWidth, WEBCAM_WIDTH);
            capture.Set(CapProp.FrameHeight, WEBCAM_HEIGHT);

            using var mat = new Mat();
            using var buffer = new VectorOfByte();

            while (true)
            {
                capture.Read(mat);
                CvInvoke.Imencode(FRAME_FORMAT_EXTENSION, mat, buffer);
                buffer.Position = 0;

                _currentFrame?.Dispose();
                _currentFrame = SKImage.FromEncodedData(buffer);

                buffer.Clear();

                if (_runDetection)
                {
                    var results = _yolo.RunObjectDetection(_currentFrame);
                    _currentFrame = _currentFrame.Draw(results);
                }

                await _dispatcher.InvokeAsync(() => WebCamFrame.InvalidateVisual());
            }
        }

        private void UpdateWebcamFrame(object sender, SKPaintSurfaceEventArgs e)
        {
            //using var canvas = e.Surface.Canvas;
            //canvas.DrawImage(_currentFrame, _rect);
            //canvas.Flush();

            using var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            if (_currentFrame != null)
            {
                // Fixing the mirror effects
                var canvasWidth = e.Info.Width;
                var canvasHeight = e.Info.Height;

                //// Flipping the entire canvas: image + detections
                //canvas.Scale(-1, 1); // Flip X axis
                //canvas.Translate(-canvasWidth, 0); // Shift back to visible area

                var destRect = new SKRect(0, 0, canvasWidth, canvasHeight);
                canvas.DrawImage(_currentFrame, destRect);
            }

            canvas.Flush();
        }

        private void StartClick(object sender, RoutedEventArgs e)
            => _runDetection = true;

        private void StopClick(object sender, RoutedEventArgs e)
            => _runDetection = false;

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _yolo?.Dispose();
            _currentFrame?.Dispose();
        }
    }
}
