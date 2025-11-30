using System.Windows.Controls;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class AuthorizationPage : Page
    {
        public AuthorizationPage(AuthorizationPageViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}
