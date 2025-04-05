using System.Windows;

namespace GameHook.OverlayEditor
{
    public partial class MainWindow : Window
    {
        private PreviewWindow? _previewWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenPreview_Click(object sender, RoutedEventArgs e)
        {
            if (_previewWindow == null || !_previewWindow.IsVisible)
            {
                _previewWindow = new PreviewWindow();
                _previewWindow.Show();
            }
            else
            {
                _previewWindow.Activate();
            }
        }
    }
}
