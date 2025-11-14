using System.Windows.Controls;

namespace uchat.Services.IServices
{
    public interface INavigationService
    {
        /// <summary>
        /// Ініціалізація кореневого фрейма для навігації.
        /// </summary>
        /// <param name="frame"></param>
        void InitializeRootFrame(Frame frame);

        /// <summary>
        /// Функція для переходу по кореневому фрейму.
        /// </summary>
        /// <typeparam name="TPage"></typeparam>
        void ChangePage<TPage>() where TPage : Page;

        /// <summary>
        /// Функція для повернення назад на кореневому фреймі.
        /// </summary>
        void GoBack();

        /// <summary>
        /// Функція для переходу по вказаному фрейму.
        /// </summary>
        /// <typeparam name="TPage"></typeparam>
        /// <param name="frame"></param>
        void ChangePage<TPage>(Frame frame) where TPage : Page; // <-- НОВЫЙ

        /// <summary>
        /// Функція для повернення назад на вказаному фреймі.
        /// </summary>
        /// <param name="frame"></param>
        void GoBack(Frame frame);
    }
}
