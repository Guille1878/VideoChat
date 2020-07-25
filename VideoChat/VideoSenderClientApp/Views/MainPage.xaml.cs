using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VideoSenderClientApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        { 
            this.InitializeComponent();
            TextBoxUserName.Text = UserService.User;
        }

        private void TextBoxUserName_TextChanged(object sender, TextChangedEventArgs e) => UserService.User = TextBoxUserName.Text;
    }
}
