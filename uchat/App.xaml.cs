using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using uchat.modelbase.Models;
using uchat.Models;
using uchat.Services;
using uchat.Services.IServices;
using uchat.ViewModels;
using uchat.ViewModels.VMEntities;
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

        // Ловимо подію підключення
        private async void ConnectionService_OnConnected()
        {
            int userInSystemId = 0;

            // Дістаємо користувача із локальної БД, якщо його немає, то виходимо з методу
            using (var appContext = Services.GetRequiredService<IDbContextFactory<ApplicationContext>>().CreateDbContext())
            {
                User? userInSystem = appContext.Users.FirstOrDefault();

                if (userInSystem == null)
                    return;

                // Зберігаємо його ID, бо далі він знадобиться
                userInSystemId = userInSystem.Id;
            }

            // Відкриваємо другий скоуп бази данних, щоб уникнути помилки подвійного відстежування
            // Запитуємо користувача з хоста, та оновлюємо його дані
            using (var appContext = Services.GetRequiredService<IDbContextFactory<ApplicationContext>>().CreateDbContext())
            {
                var userToUpdate = await Services.GetRequiredService<IConnectionService>().GetUserInfo(userInSystemId);

                if (userToUpdate != null)
                {
                    appContext.Users.Update(userToUpdate);
                    await appContext.SaveChangesAsync();

                    Current?.Dispatcher.InvokeAsync(() =>
                    {
                        Services.GetRequiredService<AppState>().LoggedUser = new UserViewModel(userToUpdate);
                        // Відкриваємо головну сторінку коли закінчили
                        Services.GetRequiredService<INavigationService>().ChangePage<MainPage>();
                    });
                }
            }
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

            var connectionService = Services.GetRequiredService<IConnectionService>();
            connectionService.OnConnected += ConnectionService_OnConnected;

            if (string.IsNullOrEmpty(ServerBaseUrl))
            {
                throw new InvalidDataException("Server base URL is null or empty.");
            }

            // Перевіряємо поточний статус авторизації, через конфігурацію
            if (bool.TryParse(configurationService.Get<string>("IsAuthorized"), out bool isAuthorized))
            {
                // Якщо ми вже авторизовані, то просто підключаємось
                if (isAuthorized)
                {
                    await connectionService.InitializeConnection(ServerBaseUrl);
                }
                else
                {
                    // Якщо користувач зайшов вперше, або "розлогінився" напрявляємо його на сторінку авторизації
                    // наступна логіка у AuthorizationPageViewModel.cs
                    navigationService.ChangePage<AuthorizationPage>();
                }
            }
            else
            {
                throw new Exception("Failed to parse authorization state, app loading can't be completed.");
            }
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
            serviceCollection.AddTransient<MenuPage>();
            serviceCollection.AddTransient<ChatInfoPage>();
            #endregion

            // Підключення ViewModels у DI контейнер
            #region ViewModels

            serviceCollection.AddSingleton<AppState>();
            serviceCollection.AddSingleton<MainPageViewModel>();
            serviceCollection.AddSingleton<AuthorizationPageViewModel>();
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
    }

}
