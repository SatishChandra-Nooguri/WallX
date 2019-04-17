using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WallX.Services;
using WallX.ViewModel;

namespace WallX.Views
{
    /// <summary>
    /// Interaction logic for ScreenRecordingView.xaml
    /// </summary>
    public partial class ScreenRecordingView : UserControl
    {
        public ScreenRecordingView(Class currentClass)
        {
            InitializeComponent();
            this.DataContext = new ScreenRecordingViewModel(this, currentClass);
        }
    }
}
