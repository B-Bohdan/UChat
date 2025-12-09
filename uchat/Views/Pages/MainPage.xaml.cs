using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class MainPage : Page
    {
        private IDbContextFactory<ApplicationContext> _databaseModelFactory;
        private INavigationService _navigationService;
        private MainPageViewModel _mainPageViewModel;

        public MainPage(MainPageViewModel viewModel, IDbContextFactory<ApplicationContext> databaseModelFactory, 
            MenuPage menuPage, FoundedUserPage foundedUserPage, ChatInfoPage chatInfoPage, SearchInviteChatPage searchInviteChatPage, INavigationService navigationService)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            _mainPageViewModel = viewModel;
            _databaseModelFactory = databaseModelFactory;
            _navigationService = navigationService;

            Application.Current.Dispatcher.Invoke(() =>
            {
                profileFrame.Navigate(menuPage);
                findUserFrame.Navigate(foundedUserPage);
                chatMenuPanelFrame.Navigate(chatInfoPage);
                addUserToChatMenu.Navigate(searchInviteChatPage);
            });
        }

        private void chatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedChat = _mainPageViewModel.ApplicationState.SelectedChat;

            if(selectedChat == null)
            {
                chatAreaFrame.Navigate(null);
                noSelectedChatHint.Visibility = Visibility.Visible;
            }
            else
            {
                noSelectedChatHint.Visibility = Visibility.Collapsed;
                _navigationService.ChangePage<ChatPage>(chatAreaFrame);
            } 
        }
    }
}
