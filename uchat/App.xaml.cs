using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using uchat.Models;
using uchat.Services;
using uchat.Services.IServices;
using uchat.ViewModels;
using uchat.Views.Pages;

namespace uchat
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            // Ініціалізуємо головне вікно та показуємо його
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            IninitializeServices();

            // Ініціалізуємо конфігурацію додатку
            IConfigurationService configurationService = Services.GetRequiredService<IConfigurationService>();
            await configurationService.InitAsync();

            // Ініціалізуємо навігацію, за замовчування відкриваємо головну сторінку
            var navigationService = Services.GetRequiredService<INavigationService>();
            navigationService.InitializeRootFrame(mainWindow.RootFrame);

            TagChecker(configurationService.Get<string>("StartPageTag"), navigationService);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Тут будемо виводити повідомленя про будь-які помилки, даби не писати try catch скрізь
        }

        private void IninitializeServices()
        {
            var serviceCollection = new ServiceCollection();

            // Підключення фабрики для контексту БД
            serviceCollection.AddDbContextFactory<ApplicationContext>(options =>
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folderPath = Path.Combine(appDataFolder, "UChat");

                // Перевіряє або створює директорію
                Directory.CreateDirectory(folderPath);

                string dbPath = Path.Combine(folderPath, "ApplicationDB.db");

                options.UseSqlite($"Data Source={dbPath}");
            });

            // Підключення сторінок у DI контейнер
            #region Pages

            serviceCollection.AddTransient<MainPage>();
            serviceCollection.AddTransient<AuthorizationPage>();
            #endregion

            // Підключення ViewModels у DI контейнер
            #region ViewModels

            serviceCollection.AddSingleton<AppState>();
            serviceCollection.AddTransient<MainPageViewModel>();
            serviceCollection.AddTransient<AuthorizationPageViewModel>();
            serviceCollection.AddTransient<LoadingPage>();
            #endregion

            // Підключення сервісів у DI контейнер
            #region Services

            serviceCollection.AddSingleton<IConfigurationService, ConfigurationService>();
            serviceCollection.AddSingleton<IConnectionService, ConnectionService>();
            serviceCollection.AddSingleton<INavigationService, NavigationService>();
            #endregion

            Services = serviceCollection.BuildServiceProvider();
        }

        private void TagChecker(string? startPageTag, INavigationService navigationService)
        {
            switch (startPageTag)
            {
                case nameof(NavigationService.NavigationTags.MainPage):
                    navigationService.ChangePage<MainPage>();
                    break;
                case nameof(NavigationService.NavigationTags.AuthorizationPage):
                    navigationService.ChangePage<AuthorizationPage>();
                    break;
                default:
                    navigationService.ChangePage<AuthorizationPage>();
                    break;
            }
        }
    }

}
