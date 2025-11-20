using System.Windows.Controls;
using uchat.ViewModels;

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
