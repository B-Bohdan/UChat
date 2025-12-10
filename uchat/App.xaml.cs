using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
using uchat.Views.Dialogs;
using uchat.Views.Pages;

namespace uchat
{
    // 1. Завершить увправление чатами
    // + 1.1 - Реализовать поиск пользователя по почте, и создание с ним чата
    // + 1.2 - Обновить создание чата, добавив возможность задавать название
    // + 1.3 - Реализовать инструменты лива из чата и приглашения в чат
    // + 1.4 - Реализовать отображение списка участников чата
    // 2. Добавить обмен сообщениями в чате
    // 2.1 Реализовать контекстное меню, и функционал обновления и удаления сообщений в чате

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
            string token = await Services.GetRequiredService<IConnectionService>().GetGoogleIdTokenAsync();
            await Services.GetRequiredService<IConnectionService>().AuthorizeUser(token);
        }

        private async void ConnectionService_OnAuthorizationConfirmed(User user)
        {
            var dbContextFactoty = Services.GetRequiredService<IDbContextFactory<ApplicationContext>>();
            var configurationServices = Services.GetRequiredService<IConfigurationService>();
            var navigationService = Services.GetRequiredService<INavigationService>();
            var connectionService = Services.GetRequiredService<IConnectionService>();
            var appState = Services.GetRequiredService<AppState>();

            // Перевіряємо існування користувача у локальній БД, додаємо або оновлюємо
            using (var appcontext = dbContextFactoty.CreateDbContext())
            {
                var userInLocal = await appcontext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

                if (userInLocal == null)
                {
                    appcontext.Users.Add(user);
                    await appcontext.SaveChangesAsync();

                    // Встановлюємо у конфіг що користувача тепер аторизовано
                    configurationServices.Set("AuthorizedUserId", user.Id);
                    configurationServices.Set("IsAuthorized", "true");
                }
                else
                {
                    // Тут оновлюємо інформацію про користувача
                    // Поки оновлювати нічого, але потенційно...
                }

                //Для зміни даних вертаємось у UI потік через Dispatcher
                Current?.Dispatcher.InvokeAsync(() =>
                {
                    appState.LoggedUser = new UserViewModel(user);
                    navigationService.ChangePage<MainPage>();
                });

                List<Chat>? userChats = await connectionService.GetUserChats(user.Id);

                if (userChats != null)
                {
                    await appState.SaveChatsDataAsync(userChats);
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
                var str = configurationService.Get<string>("ServerConnectionString");
                ServerBaseUrl = str;
            }

            // Ініціалізуємо головне вікно та показуємо його
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Ініціалізуємо навігацію, за замовчування відкриваємо головну сторінку
            var navigationService = Services.GetRequiredService<INavigationService>();
            navigationService.InitializeRootFrame(mainWindow.RootFrame);
            navigationService.ChangePage<MainPage>();

            var connectionService = Services.GetRequiredService<IConnectionService>();
            connectionService.OnConnected += ConnectionService_OnConnected;
            connectionService.OnAuthorizationConfirmed += ConnectionService_OnAuthorizationConfirmed;

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
            ErrorDialog errorDialog = new ErrorDialog(e.Exception.Message, Current.MainWindow);
            errorDialog.ShowDialog();
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
            serviceCollection.AddTransient<FoundedUserPage>();
            serviceCollection.AddTransient<SearchInviteChatPage>();
            #endregion

            // Підключення ViewModels у DI контейнер
            #region ViewModels

            serviceCollection.AddSingleton<AppState>();
            serviceCollection.AddSingleton<MainPageViewModel>();
            serviceCollection.AddSingleton<AuthorizationPageViewModel>();
            serviceCollection.AddSingleton<ChatPageViewModel>();
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
