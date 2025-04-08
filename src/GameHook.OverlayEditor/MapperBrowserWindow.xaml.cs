using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameHook.OverlayEditor
{
    public partial class MapperBrowserWindow : Window
    {
        private readonly MainWindow _main;
        private HubConnection _hubConnection;
        private static readonly HttpClient _httpClient = new HttpClient();

        public MapperBrowserWindow(MainWindow main)
        {
            InitializeComponent();
            _main = main;

            _ = LoadMapperPropertiesAsync();



            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:8085/mapperhub")
                .Build();

            _hubConnection.On<string, JToken>("PropertyChanged", (path, newValue) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateTreeValue(path, newValue?.ToString() ?? "(no value)");
                });
            });

            _ = _hubConnection.StartAsync();
        }

        private async Task LoadMapperPropertiesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:8085/mapper/properties");
                var json = await response.Content.ReadAsStringAsync();
                var jObj = JObject.Parse(json);
                var properties = jObj["mapper"]?["properties"] as JObject;
                if (properties == null) return;

                var flat = FlattenJObject(properties, "mapper.properties");

                var expanded = GetExpandedPaths(PropertyTree);
                

                // NEW PATCHING LOGIC
                foreach (var kvp in flat)
                {
                    PatchOrInsert(PropertyTree, kvp.Key.Split('.'), kvp.Key, kvp.Value);
                }

                // Restore expanded state
                RestoreExpandedPaths(PropertyTree, expanded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading mapper properties: {ex.Message}");
            }
        }

        private Dictionary<string, string> FlattenJObject(JObject obj, string prefix)
        {
            var result = new Dictionary<string, string>();

            foreach (var prop in obj.Properties())
            {
                var path = $"{prefix}.{prop.Name}";
                if (prop.Value is JObject nested)
                {
                    var sub = FlattenJObject(nested, path);
                    foreach (var kv in sub)
                        result[kv.Key] = kv.Value;
                }
                else
                {
                    result[path] = prop.Value?.ToString() ?? "(null)";
                }
            }

            return result;
        }

        private void PatchOrInsert(ItemsControl parent, string[] segments, string fullPath, string value, int index = 0)
        {
            if (index >= segments.Length) return;

            string current = segments[index];
            TreeViewItem? nextNode = null;

            foreach (TreeViewItem child in parent.Items)
            {
                // Check for value node
                if (index == segments.Length - 1)
                {
                    if (child.Header is TextBlock tb && tb.Text.StartsWith(current))
                    {
                        tb.Text = $"{current}: {value}";
                        return;
                    }
                }
                // Check for branch node
                else if (child.Header is string header && header == current)
                {
                    nextNode = child;
                    break;
                }
            }

            // Not found, insert it
            if (nextNode == null)
            {
                if (index == segments.Length - 1)
                {
                    var textBlock = new TextBlock
                    {
                        Text = $"{current}: {value}",
                        Foreground = Brushes.LightGreen
                    };

                    var item = new TreeViewItem
                    {
                        Header = textBlock,
                        Tag = fullPath
                    };

                    item.MouseRightButtonDown += TreeViewItem_RightClick;
                    parent.Items.Add(item);
                    return;
                }
                else
                {
                    nextNode = new TreeViewItem
                    {
                        Header = current,
                        Foreground = Brushes.LightGray
                    };
                    parent.Items.Add(nextNode);
                }
            }

            PatchOrInsert(nextNode, segments, fullPath, value, index + 1);
        }
        

        private void TreeViewItem_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is TreeViewItem item && item.Tag is string fullPath)
            {
                _main.AssignMapperPathToSelectedElement(fullPath);
                Close();
            }
        }

        private HashSet<string> GetExpandedPaths(ItemsControl parent, string currentPath = "")
        {
            var expanded = new HashSet<string>();

            foreach (var item in parent.Items)
            {
                if (item is TreeViewItem treeItem)
                {
                    string thisPath = string.IsNullOrEmpty(currentPath) ? treeItem.Header.ToString() : $"{currentPath}.{treeItem.Header}";
                    if (treeItem.IsExpanded)
                        expanded.Add(thisPath);

                    foreach (var sub in GetExpandedPaths(treeItem, thisPath))
                        expanded.Add(sub);
                }
            }

            return expanded;
        }

        private void RestoreExpandedPaths(ItemsControl parent, HashSet<string> expanded, string currentPath = "")
        {
            foreach (var item in parent.Items)
            {
                if (item is TreeViewItem treeItem)
                {
                    string thisPath = string.IsNullOrEmpty(currentPath) ? treeItem.Header.ToString() : $"{currentPath}.{treeItem.Header}";

                    treeItem.IsExpanded = expanded.Contains(thisPath);
                    RestoreExpandedPaths(treeItem, expanded, thisPath);
                }
            }
        }

        private void UpdateTreeValue(string fullPath, string newValue)
        {
            string[] segments = fullPath.Split('.');
            TreeViewItem? currentNode = PropertyTree.Items[0] as TreeViewItem;

            for (int i = 1; i < segments.Length; i++)
            {
                if (currentNode == null)
                    return;

                TreeViewItem? next = null;
                foreach (TreeViewItem child in currentNode.Items)
                {
                    if (i == segments.Length - 1)
                    {
                        if (child.Header is TextBlock tb && tb.Text.StartsWith(segments[i]))
                        {
                            tb.Text = $"{segments[i]}: {newValue}";
                            return;
                        }
                    }
                    else if (child.Header is string header && header == segments[i])
                    {
                        next = child;
                        break;
                    }
                }

                currentNode = next;
            }
        }
    }
}
