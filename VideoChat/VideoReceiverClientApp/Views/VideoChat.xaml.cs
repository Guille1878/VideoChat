using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.UI.Core;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VideoReceiverClientApp
{
    public sealed partial class VideoChat : UserControl
    {
        HubConnection hubConnection;

        public VideoChat()
        {
            this.InitializeComponent();
        }

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
        
        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
          
            try
            {
                hubConnection = new HubConnectionBuilder().WithUrl("https://localhost:44308/video", options =>
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

                hubConnection.Closed += async (error) =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await hubConnection.StartAsync();
                };

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        static bool isStreamingIn = false;
        static Queue<byte[]> StreamedArraysQueue = new Queue<byte[]>();
        private async void StreamVideo_Click(object sender, RoutedEventArgs e)
        {
            isStreamingIn = StreamVideo.IsChecked ?? false;

            if (isStreamingIn)
            {

                hubConnection.On<byte[]>("DownloadStream", (stream) =>
                {
                    _ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (isStreamingIn)
                            StreamedArraysQueue.Enqueue(stream);
                    });
                });

                if (hubConnection.State == HubConnectionState.Disconnected)
                    await hubConnection.StartAsync();

                _ = BuildImageFrames();
            }
            else
                await hubConnection.StopAsync();

        }

        private async Task BuildImageFrames()
        {

            while (isStreamingIn)
            {
                await Task.Delay(5);

                StreamedArraysQueue.TryDequeue(out byte[] buffer);

                if (!(buffer?.Any() ?? false))
                    continue;

                try
                {


                    var randomAccessStream = new InMemoryRandomAccessStream();
                    await randomAccessStream.WriteAsync(buffer.AsBuffer());
                    randomAccessStream.Seek(0); 
                    await randomAccessStream.FlushAsync();

                    var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    var imageSource = await ConvertToSoftwareBitmapSource(softwareBitmap);

                    ImageVideo.Source = imageSource;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        public async Task<SoftwareBitmapSource> ConvertToSoftwareBitmapSource(SoftwareBitmap softwareBitmap)
        {
            var displayableImage = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(displayableImage);

            return bitmapSource;
        }
    }
}
