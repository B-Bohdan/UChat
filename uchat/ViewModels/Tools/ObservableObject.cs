using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace uchat.ViewModels.Tools
{
    /// <summary>
    /// Зручний спосіб реалізації INotifyPropertyChanged
    /// </summary>
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
