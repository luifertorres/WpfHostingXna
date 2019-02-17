using System.Windows;
using TestApplication.Controls;

namespace TestApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private XnaControl _xnaControl;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeXnaControl()
        {
            _xnaControl = new XnaControl();

            if (!mainGrid.Children.Contains(_xnaControl))
            {
                mainGrid.Children.Add(_xnaControl);
            }

            _xnaControl.BeginInit();
        }

        private void Window_SourceInitialized(object sender, System.EventArgs e)
        {
            InitializeXnaControl();
        }

        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var backRectColor = _xnaControl.BackRectColor;
            var frontRectColor = _xnaControl.FrontRectColor;

            backRectColor.R += 50;
            backRectColor.G += 50;
            backRectColor.B += 50;
            frontRectColor.R += 50;
            frontRectColor.G += 50;
            frontRectColor.B += 50;
            _xnaControl.BackRectColor = backRectColor;
            _xnaControl.FrontRectColor = frontRectColor;
        }
    }
}
