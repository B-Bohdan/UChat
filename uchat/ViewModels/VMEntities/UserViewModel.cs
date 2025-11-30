using uchat.modelbase.Models;

namespace uchat.ViewModels.VMEntities
{
    public class UserViewModel : WrapperViewModel<User>
    {
        public UserViewModel(User model) : base(model) { }

        public int Id => Model.Id;

        public string FirstName
        {
            get => Model.FirstName;
            set
            {
                if (Model.FirstName != value)
                {
                    Model.FirstName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }
        
        public string LastName
        {
            get => Model.LastName;
            set
            {
                if (Model.LastName != value)
                {
                    Model.LastName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }

        /// <summary>
        /// Represent full name of user
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string Email
        {
            get => Model.Email;
            set
            {
                if (Model.Email != value)
                {
                    Model.Email = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreatedAt => Model.CreatedAt;

        public User.Status UserStatus
        {
            get => Model.UserStatus;
            set
            {
                if (Model.UserStatus != value)
                {
                    Model.UserStatus = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
