using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.Capture;
using System.Threading.Tasks;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.Media.MediaProperties;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage.Streams;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VideoSenderClientApp.Views
{
    public sealed partial class VideoChat : UserControl
    {
        MediaCapture mediaCapture;
        bool isPreviewing;
        DisplayRequest displayRequest = new DisplayRequest();
        HubConnection connection;
        public VideoChat()
        {
            this.InitializeComponent();

            connection = new HubConnectionBuilder().WithUrl("https://localhost:44308/video", options =>
            {
                options.HttpMessageHandlerFactory = (handler) =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = ValidateCertificate;
                    }
                    return handler;
                };
                options.CloseTimeout = TimeSpan.FromSeconds(20);
            }).Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };

        }

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
        
        private async void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
          
            try
            {
                await StartPreviewAsync();
                await connection.StartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
               
        private async void SwitchCamera_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchCamera.IsChecked ?? false)
            {
                await StartPreviewAsync();
            }
        }

        private async Task StartPreviewAsync()
        {
            try
            {
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });                

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                // This will be thrown if the user denied access to the camera in privacy settings
                // ShowMessageToUser("The app was denied access to the camera");
                return;
            }

            try
            {
                OwnCamera.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                // ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        private async void ScreenShot_Click(object sender, RoutedEventArgs e)
        {
            var lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));

            var capturedPhoto = await lowLagCapture.CaptureAsync();
            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;

            await lowLagCapture.FinishAsync();
        }



        static bool isStreamingOut = false;
        static Queue<VideoFrame> FramesQueue = new Queue<VideoFrame>();
        int delayMilliSeconds = 20;
        private void RecordVideo_Click(object sender, RoutedEventArgs e)
        {
            isStreamingOut = RecordVideo.IsChecked ?? false;

            if (isStreamingOut)
            {
                _ = SaveFrames();
                _ = StreamOutFrames();
            }
        }

        private async Task SaveFrames()
        {
            while (isStreamingOut)
            {
                // Get information about the preview
                var previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                // Create a video frame in the desired format for the preview frame
                VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

                FramesQueue.Enqueue(await mediaCapture.GetPreviewFrameAsync(videoFrame));
                await Task.Delay(delayMilliSeconds);
            }
        } 
        private async Task StreamOutFrames()
        {
            try
            {

                while (isStreamingOut)
                {
                    FramesQueue.TryDequeue(out VideoFrame frame);

                    if (frame == null)
                    {
                        await Task.Delay(delayMilliSeconds);
                        continue;
                    }

                    var memoryRandomAccessStream = new InMemoryRandomAccessStream();

                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, memoryRandomAccessStream);

                    // Set the software bitmap
                    encoder.SetSoftwareBitmap(frame.SoftwareBitmap);
                    //encoder.BitmapTransform.ScaledWidth = 320;
                    //encoder.BitmapTransform.ScaledHeight = 240;
                    //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                    //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    encoder.IsThumbnailGenerated = false;
                    await encoder.FlushAsync();


                    try
                    {
                        var array = new byte[memoryRandomAccessStream.Size];
                        await memoryRandomAccessStream.ReadAsync(array.AsBuffer(), (uint)memoryRandomAccessStream.Size, InputStreamOptions.None);

                        if (array.Any())
                            await connection.InvokeAsync("UploadStream", array);

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }

                    await Task.Delay(5);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            FramesQueue.Clear();
        }        
    }
}
