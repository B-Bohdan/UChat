using System.Windows;
using System.Windows.Input;
using uchat.ViewModels.Tools;

namespace uchat.ViewModels.VMWindows
{
    public class MainWindowViewModel : ObservableObject
    {
        public ICommand ShowWindowCommand { get; }
        public ICommand ExitApplicationCommand {  get; }
        public MainWindowViewModel()
        {
            ShowWindowCommand = new RelayCommand(_ => ShowWindow());
            ExitApplicationCommand = new RelayCommand(_ => ExitApplication());
        }
        private void ShowWindow()
        {
            var window = Application.Current.MainWindow;
            if (window != null)
            {
                // Якщо вікно сховане або згорнуте - показуємо його
                if (window.Visibility != Visibility.Visible) window.Show();
                if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
                // Виводимо на передній план
                window.Activate();
            }
        }

        private void ExitApplication()
        {
            // Повне закриття програми
            Application.Current.Shutdown();
        }
    }
}
