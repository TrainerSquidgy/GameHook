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
            MessageBox.Show("View Mapper Values clicked");
        }

        private void Toolbox_AddText_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Text clicked!");
        }

        private void Toolbox_AddShape_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add Shape clicked!");
        }

        private async Task LoadMapperMetaAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:8085/mapper");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(json);

                if (root is not JsonObject rootObject)
                    throw new Exception("Root JSON is not an object");

                var meta = rootObject["meta"];

                if (meta is not JsonObject metaObject)
                    throw new Exception("The node must be of type 'JsonObject'.");

                MetaPanel.Children.Clear();

                foreach (var kvp in metaObject)
                {
                    MetaPanel.Children.Add(new TextBlock
                    {
                        Text = $"{kvp.Key}: {kvp.Value}",
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       private async Task LoadMetaDataAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:8085/mapper/meta");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var metaData = JsonSerializer.Deserialize<JsonElement>(json);

                MetaPanel.Children.Clear();

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

                    MetaPanel.Children.Add(label);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading metadata: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
