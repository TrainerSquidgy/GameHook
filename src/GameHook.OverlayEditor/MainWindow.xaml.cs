using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameHook.OverlayEditor
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            LoadMetaDataAsync(); // Call on load
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
            MessageBox.Show("Add Text clicked!");
        }

        private void Toolbox_AddShape_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Shape clicked!");
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

