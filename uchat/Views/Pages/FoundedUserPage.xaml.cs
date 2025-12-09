using System.Windows.Controls;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class FoundedUserPage : Page
    {
        public FoundedUserPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}
