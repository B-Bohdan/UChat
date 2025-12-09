using System.Windows.Controls;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class ChatInfoPage : Page
    {
        public ChatInfoPage(ChatPageViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}
