using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using uchat.Models;
using uchat.Services;
using uchat.Services.IServices;
using uchat.ViewModels;
using uchat.ViewModels.VMPages;
using uchat.Views.Pages;

namespace uchat
{
    // 1. Сверстать MainPage
    // 2. Прописать логику авторизации по порядку, измення страницы и устанавливая подключение

    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public static string? ServerBaseUrl { get; private set; }

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            IninitializeServices();

            // Ініціалізуємо конфігурацію додатку
            IConfigurationService configurationService = Services.GetRequiredService<IConfigurationService>();
            await configurationService.InitAsync();

            if (e.Args.Length == 2)
            {
                string ip = e.Args[0];
                string port = e.Args[1];
                // Формируем адрес из аргументов
                ServerBaseUrl = $"http://{ip}:{port}/chatHub";
            }
            else
            {
                ServerBaseUrl = configurationService.Get<string>("ServerConnectionString");
            }

            // Ініціалізуємо головне вікно та показуємо його
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Ініціалізуємо навігацію, за замовчування відкриваємо головну сторінку
            var navigationService = Services.GetRequiredService<INavigationService>();
            navigationService.InitializeRootFrame(mainWindow.RootFrame);

            //TagChecker(configurationService.Get<string>("StartPageTag"), navigationService);
            TagChecker("MainPage", navigationService);

            if (string.IsNullOrEmpty(ServerBaseUrl))
            {
                throw new InvalidDataException("Server base URL is null or empty.");
            }

            //var connectionService = Services.GetRequiredService<IConnectionService>();
            //await connectionService.InitializeConnection(ServerBaseUrl);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Тут будемо виводити повідомленя про будь-які помилки, даби не писати try catch скрізь
            e.Handled = true;
            MessageBox.Show(e.Exception.Message);
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
            serviceCollection.AddTransient<LoadingPage>();
            serviceCollection.AddTransient<ChatPage>();
            #endregion

            // Підключення ViewModels у DI контейнер
            #region ViewModels

            serviceCollection.AddSingleton<AppState>();
            serviceCollection.AddTransient<MainPageViewModel>();
            serviceCollection.AddTransient<AuthorizationPageViewModel>();
            serviceCollection.AddTransient<ChatPageViewModel>();
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
