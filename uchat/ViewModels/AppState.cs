using uchat.ViewModels.Tools;
using uchat.ViewModels.VMEntities;

namespace uchat.ViewModels
{
    /// <summary>
    /// Зберігає глоабальні стани додатку, які не можно динамічно грузити з json конфігурації.
    /// </summary>
    public class AppState : ObservableObject
    {
        public AppState() { }

        private UserViewModel? _loggeduser;
        public UserViewModel? LoggedUser
        {
            get { return _loggeduser; } 
            set { _loggeduser = value; OnPropertyChanged();}
        }
    }
}
