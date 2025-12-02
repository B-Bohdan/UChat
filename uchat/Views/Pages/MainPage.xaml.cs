using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using uchat.Models;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class MainPage : Page
    {
        private IDbContextFactory<ApplicationContext> _databaseModelFactory;

        public MainPage(MainPageViewModel viewModel, IDbContextFactory<ApplicationContext> databaseModelFactory, MenuPage menuPage)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            _databaseModelFactory = databaseModelFactory;

            Application.Current.Dispatcher.Invoke(() =>
            {
                profileFrame.Navigate(menuPage);
            });
        }
    }
}
