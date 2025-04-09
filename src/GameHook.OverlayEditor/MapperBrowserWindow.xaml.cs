using Esprima.Ast;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using YamlDotNet.Core.Tokens;

namespace GameHook.OverlayEditor
{
    public partial class MapperBrowserWindow : Window
    {
        private bool _hasSubscribedToUpdates = false;
        private readonly MainWindow _main;
        private readonly Dictionary<string, TextBlock> _propertyBlocks = new();
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();
        private readonly HashSet<string> _expandedPaths = new();

        private class Node

        {
            public string Name { get; set; }
            public string FullPath { get; set; } = "";
            public string? Value { get; set; } // Only non-null at leaf nodes
            public Dictionary<string, Node> Children { get; } = new();
            
            public Node(string name)
            {
                Name = name;
            }

            public Node GetOrAddChild(string name)
            {
                if (!Children.ContainsKey(name))
                {
                    Children[name] = new Node(name);
                }
                return Children[name];
            }
        }

        public MapperBrowserWindow(MainWindow main)
        {
            InitializeComponent();
            _main = main;
            _refreshTimer.Interval = TimeSpan.FromMilliseconds(2000);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            string debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapperUpdateLog.txt");


            Loaded += MapperBrowserWindow_Loaded;

            File.AppendAllText(debugPath, $"[{DateTime.Now}] Mapper window loaded.\n");
        }

        private async void MapperBrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMapperPropertiesAsync();
            foreach (var key in _propertyBlocks.Keys)
            {
                LogToFile($"Tracking UI element: {key}");
            }
        }

        private async Task LoadMapperPropertiesAsync()

        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:8085/mapper/properties");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Failed to load mapper properties.");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var propertiesArray = JArray.Parse(json);

                // Clear the UI list (will repopulate after rendering tree later)
                PropertyListPanel.Children.Clear();

                // Parse properties into (path, value) tuples
                var flatProperties = new List<(string path, string value)>();
                foreach (var item in propertiesArray)
                {
                    var path = item["path"]?.ToString() ?? "(no path)";
                    var value = item["value"]?.ToString() ?? "(no value)";
                    flatProperties.Add((path, value));
                }

                // Build the tree structure (but don't render it yet)
                Node root = BuildTreeFromPaths(flatProperties);

                RenderNodeToPanel(root, PropertyListPanel);
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading mapper properties: {ex.Message}");
            }
        }

        private Node BuildTreeFromPaths(List<(string path, string value)> flatProperties)
        {
            var root = new Node("root") { FullPath = "" };

            foreach (var (path, value) in flatProperties)
            {
                var parts = path.Split('.');
                var current = root;

                foreach (var part in parts.Take(parts.Length - 1))
                {
                    current = current.GetOrAddChild(part);
                }

                var leaf = parts.Last();
                var leafNode = current.GetOrAddChild(leaf);
                leafNode.Value = value;
                leafNode.FullPath = path;
            }

            return root;
        }



        private void RenderNodeToPanel(Node node, Panel panel)
        {
            foreach (var child in node.Children.Values)
            {
                if (child.Children.Count > 0)
                {
                    // Group/folder
                    var expander = new Expander
                    {
                        Header = child.Name,
                        Foreground = Brushes.LightGray,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(6, 2, 6, 2)
                    };

                    var innerPanel = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
                    RenderNodeToPanel(child, innerPanel);
                    expander.Content = innerPanel;

                    panel.Children.Add(expander);
                }
                else
                {
                    // Leaf/value
                    var text = new TextBlock
                    {
                        Text = $"{child.FullPath}: {child.Value}",
                        Foreground = Brushes.LightGreen,
                        Margin = new Thickness(12, 2, 6, 2),
                        Cursor = Cursors.Hand
                    };

                    // Track this block by full path
                    _propertyBlocks[child.FullPath] = text;

                    // Context menu
                    var menu = new ContextMenu();
                    var assignItem = new MenuItem { Header = "Assign to selected element" };
                    assignItem.Click += (s, e) =>
                    {
                        MessageBox.Show($"Assigned {child.FullPath}");
                    };
                    menu.Items.Add(assignItem);
                    text.ContextMenu = menu;

                    panel.Children.Add(text);
                }
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                File.AppendAllText("MapperUpdateLog.txt", $"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Logging failed: " + ex.Message);
            }

           
        }

        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Save expanded paths before clearing
                _expandedPaths.Clear();
                SaveExpandedStates(PropertyListPanel);

                var response = await _httpClient.GetAsync("http://localhost:8085/mapper/properties");
                if (!response.IsSuccessStatusCode) return;

                var json = await response.Content.ReadAsStringAsync();
                var propertiesArray = JArray.Parse(json);

                var flatProperties = new List<(string path, string value)>();
                foreach (var item in propertiesArray)
                {
                    var path = item["path"]?.ToString() ?? "(no path)";
                    var value = item["value"]?.ToString() ?? "(no value)";
                    flatProperties.Add((path, value));
                }

                var root = BuildTreeFromPaths(flatProperties);
                foreach (var (path, value) in flatProperties)
{
    if (_propertyBlocks.TryGetValue(path, out var textBlock))
    {
        if (textBlock.Text != $"{path}: {value}")
        {
            textBlock.Text = $"{path}: {value}";
        }
    }
    else
    {
        // If new property was added (e.g., hot reload), rebuild everything (or you can add-in here)
        await LoadMapperPropertiesAsync();
        return;
    }
}

                RestoreExpandedStates(PropertyListPanel);
            }
            catch (Exception ex)
            {
                LogToFile($"[ERROR] RefreshTimer_Tick failed: {ex.Message}");
            }
        }

        private void SaveExpandedStates(Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Expander expander && expander.IsExpanded)
                {
                    _expandedPaths.Add(expander.Header?.ToString() ?? "");
                    if (expander.Content is Panel innerPanel)
                        SaveExpandedStates(innerPanel);
                }
            }
        }

        private void RestoreExpandedStates(Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Expander expander)
                {
                    if (_expandedPaths.Contains(expander.Header?.ToString() ?? ""))
                        expander.IsExpanded = true;

                    if (expander.Content is Panel innerPanel)
                        RestoreExpandedStates(innerPanel);
                }
            }
        }


        // Bottom of Class Here! Don't put anything below if it needs to be in the class!
        public class PropertyChangedEvent
        {
            public string path { get; set; }
            public string value { get; set; }
            public string address { get; set; }
            public string[] bytes { get; set; }
            public bool frozen { get; set; }
            public List<string> fieldsChanged { get; set; }
        }

    }
    }

    
