using System.Windows;

namespace uchat.Views.Dialogs
{
    public partial class InfoDialog : Window
    {
        public InfoDialog(string message, Window owner)
        {
            InitializeComponent();
            this.Message = message;
            this.Owner = owner;
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(InfoDialog), new PropertyMetadata(string.Empty));

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}