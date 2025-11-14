using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Threading;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            // Ініціалізуємо головне вікно та показуємо його
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            IninitializeServices();

            // Ініціалізуємо навігацію, за замовчування відкриваємо головну сторінку
            var navigationService = Services.GetRequiredService<INavigationService>();
            navigationService.InitializeRootFrame(mainWindow.RootFrame);
            navigationService.ChangePage<MainPage>();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Тут будемо виводити повідомленя про будь-які помилки, даби не писати try catch скрізь
        }

        private void IninitializeServices()
        {
            var serviceCollection = new ServiceCollection();

            #region Pages

            serviceCollection.AddTransient<MainPage>();
            #endregion


            #region ViewModels

            serviceCollection.AddSingleton<AppState>();
            serviceCollection.AddTransient<MainPageViewModel>();
            #endregion

            #region Services

            serviceCollection.AddSingleton<IConfigurationService, ConfigurationService>();
            serviceCollection.AddSingleton<IConnectionService, ConnectionService>();
            serviceCollection.AddSingleton<INavigationService, NavigationService>();
            #endregion

            //... Додаємо сервіси сюди

            Services = serviceCollection.BuildServiceProvider();
        }
    }

}
