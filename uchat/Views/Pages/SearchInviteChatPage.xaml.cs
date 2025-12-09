using System.Windows.Controls;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class SearchInviteChatPage : Page
    {
        public SearchInviteChatPage(ChatPageViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}
