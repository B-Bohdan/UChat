using System.Diagnostics;
using System.Windows.Controls;
using uchat.ViewModels;

namespace uchat.Views.Pages
{
    public partial class MainPage : Page
    {
        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        // Не казино
        private async void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://vegas.ua",
                    UseShellExecute = true
                });
            });
        }
    }
}
