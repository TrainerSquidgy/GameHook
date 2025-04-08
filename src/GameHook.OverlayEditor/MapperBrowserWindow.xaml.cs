using Esprima.Ast;
using Microsoft.AspNetCore.SignalR.Client;
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
        private HubConnection? _hubConnection;
        private static readonly HttpClient _httpClient = new HttpClient();
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
                await ConnectToSignalR();
                string debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapperUpdateLog.txt");
                File.AppendAllText(debugPath, $"[{DateTime.Now}] ConnectToSignalR() started.\n");
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

        private async Task ConnectToSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:8085/updates")
                .WithAutomaticReconnect()
                .Build();


            _hubConnection.On<List<PropertyChangedEvent>>("PropertiesChanged", (propertiesChanged) =>
            {
                // This goes first — summary of how many properties changed
                LogToFile($"Received PropertiesChanged event with {propertiesChanged.Count} item(s).");

                foreach (var propertyChanged in propertiesChanged)
                {
                    LogToFile($"Received update: {propertyChanged.path} → {propertyChanged.value}");

                    if (_propertyBlocks.TryGetValue(propertyChanged.path, out var element))
                    {
                        LogToFile($"Updating UI for: {propertyChanged.path}");

                        Dispatcher.Invoke(() =>
                        {
                            element.Text = $"{propertyChanged.path}: {propertyChanged.value}";
                        });
                    }
                    else
                    {
                        LogToFile($"Property not found in _propertyBlocks: {propertyChanged.path}");
                    }
                }

            });

            _hubConnection.On<string, object>("ReceiveMessage", (key, value) =>
            {
                LogToFile($"[DEBUG] SignalR fallback - Key: {key}, Value: {value}");
            });

            try
            {
                await _hubConnection.StartAsync();
                LogToFile("SignalR connection established successfully.");

                if (!_hasSubscribedToUpdates)
                {
                    _hasSubscribedToUpdates = true;

                    Dispatcher.Invoke(async () =>
                    {
                        LogToFile("Calling LoadMapperPropertiesAsync() to subscribe for updates.");
                        await LoadMapperPropertiesAsync();
                    });
                }
                else
                {
                    LogToFile("Already subscribed to updates. Skipping LoadMapperPropertiesAsync().");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to SignalR:\n{ex.Message}");
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

    
