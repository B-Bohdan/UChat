using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace uchat
{
    public partial class MainWindow : Window
    {
        // Потрібний для передачи кореневого фрейма в сервіс навігації
        public Frame RootFrameProperty
        {
            get { return this.RootFrame; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            // Скасовуємо закриття
            e.Cancel = true;
            // Просто ховаємо вікно з екрану
            this.Hide();
        }
    }
}