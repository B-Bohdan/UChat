using System.Windows;
using System.Windows.Controls;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class MenuPage : Page
    {
        public MenuPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        private void CopyEmailBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(EmailField.Text);
        }
    }
}
