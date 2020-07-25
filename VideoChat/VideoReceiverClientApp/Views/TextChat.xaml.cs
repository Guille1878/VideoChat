using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VideoReceiverClientApp
{
    public sealed partial class TextChat : UserControl
    {
        HubConnection connection;
        public TextChat()
        {
            this.InitializeComponent();

            connection = new HubConnectionBuilder().WithAutomaticReconnect().WithUrl("https://localhost:44308/text", options =>
            {
                options.HttpMessageHandlerFactory = (handler) =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = ValidateCertificate;
                    }
                    return handler;
                };
            }).Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
        }

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

        private async void ButtonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await connection.InvokeAsync("SendMessage", UserService.User, TextBoxMessage.Text);
            }
            catch (Exception ex)
            {
                ListBoxMessages.Items.Add(ex.Message);
            }
        }

        private async void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            connection.On<string, string>("GetMessage", (name, message) =>
                       {
                           _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                             {
                                 var newMessage = $"{name}: {message}";
                                 ListBoxMessages.Items.Add(newMessage);
                             });
                       });


            try
            {
                await connection.StartAsync();
                ListBoxMessages.Items.Add("Connection started");
                ButtonSendMessage.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ListBoxMessages.Items.Add(ex.Message);
            }
        }
    }
}
