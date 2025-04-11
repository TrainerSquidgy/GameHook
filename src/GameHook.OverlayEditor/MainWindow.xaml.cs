using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameHook.OverlayEditor
{
    public partial class MainWindow : Window

    {
        private OverlayLayout CurrentLayout;
        private string OverlayFilePath;

        private Stack<OverlayLayout> UndoStack = new Stack<OverlayLayout>();
        private Stack<OverlayLayout> RedoStack = new Stack<OverlayLayout>();
        private readonly HttpClient _httpClient = new HttpClient();
        private FrameworkElement _selectedElement;
        private Point _dragStartPoint;
        private bool _isDragging;

        public MainWindow()
        {
            InitializeComponent();
            LoadMetaDataAsync(); // Call on load
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _selectedElement = sender as FrameworkElement;
            _dragStartPoint = e.GetPosition(PreviewCanvas);
            _isDragging = true;
            _selectedElement.CaptureMouse();
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedElement != null)
            {
                Point currentPoint = e.GetPosition(PreviewCanvas);
                double offsetX = currentPoint.X - _dragStartPoint.X;
                double offsetY = currentPoint.Y - _dragStartPoint.Y;

                double left = Canvas.GetLeft(_selectedElement) + offsetX;
                double top = Canvas.GetTop(_selectedElement) + offsetY;

                Canvas.SetLeft(_selectedElement, left);
                Canvas.SetTop(_selectedElement, top);

                _dragStartPoint = currentPoint;
            }
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            if (_selectedElement != null)
                _selectedElement.ReleaseMouseCapture();
        }


        public void AssignMapperPathToSelectedElement(string path)
        {
            MessageBox.Show($"Mapper path assigned: {path}");
        }

        private void CreateOverlay_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Create Overlay clicked");
        }

        private void SaveOverlay_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save Overlay clicked");
        }

        private void SaveAsOverlay_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save As clicked");
        }

        private void LoadOverlay_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Load Overlay clicked");
        }

        private void CloneOverlay_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Clone clicked");
        }

        private void ApplyResolution_Click(object sender, RoutedEventArgs e)
        {
            if (ResolutionDropdown.SelectedItem is ComboBoxItem selectedItem)
            {
                string[] dimensions = selectedItem.Content.ToString().Split('x');
                if (dimensions.Length == 2 && int.TryParse(dimensions[0], out int width) && int.TryParse(dimensions[1], out int height))
                {
                    PreviewCanvas.Width = width;
                    PreviewCanvas.Height = height;
                }
            }
        }

        public abstract class OverlayElement
        {
            public FrameworkElement UIElement { get; set; }
            public string Type { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        private void DeleteOverlay_Click(object sender, RoutedEventArgs e) 
        {
            MessageBox.Show("Delete clicked");
        }

        private void ViewMapperValues_Click(object sender, RoutedEventArgs e)
        {
            var browser = new MapperBrowserWindow(this); // pass `this`
            browser.Owner = this;
            browser.Show();
        }

        private void Toolbox_AddText_Click(object sender, RoutedEventArgs e)
        {
            var textBlock = new TextBlock
            {
                Text = "New Text",
                FontSize = 24,
                Foreground = Brushes.White,
                Background = Brushes.Transparent
            };

            Canvas.SetLeft(textBlock, 100);
            Canvas.SetTop(textBlock, 100);
            PreviewCanvas.Children.Add(textBlock);

            RegisterElementEvents(textBlock);
        }

        private void Toolbox_AddShape_Click(object sender, RoutedEventArgs e)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = 100,
                Height = 50,
                Fill = Brushes.DodgerBlue
            };

            Canvas.SetLeft(rect, 150);
            Canvas.SetTop(rect, 150);
            PreviewCanvas.Children.Add(rect);

            RegisterElementEvents(rect);
        }

        private void RegisterElementEvents(FrameworkElement element)
        {
            element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            element.MouseMove += Element_MouseMove;
            element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
        }



        private async Task LoadMetaDataAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:8085/mapper/meta");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var metaData = JsonSerializer.Deserialize<JsonElement>(json);

                

                if (metaData.TryGetProperty("gameName", out var gameNameElement))
                {
                    MapperNameLabel.Content = $"Game: {gameNameElement.GetString()}";
                }
                else
                {
                    MapperNameLabel.Content = "Game: (Unknown)";
                }

                foreach (var property in metaData.EnumerateObject())
                {
                    var label = new Label
                    {
                        Content = $"{property.Name}: {property.Value}",
                        Foreground = Brushes.White,
                        FontSize = 14,
                        Margin = new Thickness(4, 2, 4, 2)
                    };

                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading metadata: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMetaDataAsync();
            
        }
    }
}

