using WallX.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace WallX.Views
{
    /// <summary>
    /// Interaction logic for DesktopView.xaml
    /// </summary>
    public partial class DesktopView : Window
    {
        public DesktopView()
        {
            InitializeComponent();
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
        }

        public DesktopView(MainWindow parent, string filePath)
        {
            InitializeComponent();
            DataContext = new DesktopViewModel(this, parent, filePath);
        }

        private void canv_main_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
