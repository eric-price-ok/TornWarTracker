using System.Windows;
using TornWarTracker.ViewModels;

namespace TornWarTracker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
