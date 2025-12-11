using System.Collections.Specialized;
using System.Windows.Controls;
using uchat.ViewModels.VMPages;

namespace uchat.Views.Pages
{
    public partial class ChatPage : Page
    {
        public ChatPage(ChatPageViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            this.Loaded += ChatPage_Loaded;
            ((INotifyCollectionChanged)ChatListBox.Items).CollectionChanged += ChatListBox_CollectionChanged;
        }

        private void ChatPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ChatListBox.Items.Count > 0)
            {
                var lastItem = ChatListBox.Items[ChatListBox.Items.Count - 1];

                ChatListBox.ScrollIntoView(lastItem);
            }
        }

        private void ChatListBox_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (ChatListBox.Items.Count > 0)
                {
                    ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
                }
            }
        }
    }
}
