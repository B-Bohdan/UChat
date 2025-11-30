using Microsoft.EntityFrameworkCore;
using System.Windows.Controls;
using uchat.modelbase.Models;
using uchat.Models;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class MainPage : Page
    {
        private IDbContextFactory<ApplicationContext> _databaseModelFactory;

        public MainPage(MainPageViewModel viewModel, IDbContextFactory<ApplicationContext> databaseModelFactory)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            _databaseModelFactory = databaseModelFactory;
        }

        // Не казино
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //await Task.Run(() =>
            //{
            //    Process.Start(new ProcessStartInfo
            //    {
            //        FileName = "https://vegas.ua",
            //        UseShellExecute = true
            //    });
            //});


            // Тест бази даних
            using (var dbContext = _databaseModelFactory.CreateDbContext())
            {
                User user = new User
                {
                    FirstName = "TestUser",
                    LastName = "Test User",
                    Email = "test@test.com",
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(user);
                dbContext.SaveChanges();
            }
        }
    }
}
