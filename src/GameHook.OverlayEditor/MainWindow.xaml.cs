using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using GameHook.OverlayEditor.Models;
using System.Windows.Controls.Primitives;

namespace GameHook.OverlayEditor
{
    public partial class MainWindow : Window

    {
        private OverlayLayout CurrentLayout = new OverlayLayout();
        private string CurrentFilePath = string.Empty;
        private string? SelectedElementId = null;

        private Stack<OverlayLayout> UndoStack = new();
        private Stack<OverlayLayout> RedoStack = new();
        private readonly HttpClient _httpClient = new HttpClient();
        private FrameworkElement _selectedElement;
        private Point _dragStartPoint;
        private bool _isDragging;


        public MainWindow()
        {
            InitializeComponent();
            LoadMetaDataAsync(); // Call on load
        }

        private void Undo_Click(object sender, RoutedEventArgs e) => Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Redo();

        private List<Thumb> _resizeHandles = new();

        private void ShowResizeHandles(FrameworkElement target)
        {
            // Clear old handles
            foreach (var handle in _resizeHandles)
                PreviewCanvas.Children.Remove(handle);
            _resizeHandles.Clear();

            if (target == null) return;

            double size = 16;
            double left = Canvas.GetLeft(target);
            double top = Canvas.GetTop(target);
            double width = target.Width;
            double height = target.Height;

            var positions = new (double x, double y, Cursor cursor, Action<double, double> resize)[]
            {
        (0, 0, Cursors.SizeNWSE, (dx, dy) => ResizeElement(target, -dx, -dy, dx, dy)),
        (width, 0, Cursors.SizeNESW, (dx, dy) => ResizeElement(target, dx, -dy, 0, dy)),
        (0, height, Cursors.SizeNESW, (dx, dy) => ResizeElement(target, -dx, dy, dx, 0)),
        (width, height, Cursors.SizeNWSE, (dx, dy) => ResizeElement(target, dx, dy, 0, 0))
            };

            foreach (var (x, y, cursor, resizeAction) in positions)
            {
                var thumb = new Thumb
                {
                    Width = size,
                    Height = size,
                    Background = Brushes.White,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Cursor = cursor
                };

                Canvas.SetLeft(thumb, left + x - size / 2);
                Canvas.SetTop(thumb, top + y - size / 2);

                thumb.DragDelta += (s, e) =>
                {
                    resizeAction(e.HorizontalChange, e.VerticalChange);
                    UpdateResizeHandles(target);
                };

                _resizeHandles.Add(thumb);
                PreviewCanvas.Children.Add(thumb);
            }
        }

        private void UpdateResizeHandles(FrameworkElement target)
        {
            ShowResizeHandles(target); // Reposition handles based on new size/position
        }

        private void ClearResizeHandles()
        {
            foreach (var handle in _resizeHandles)
                PreviewCanvas.Children.Remove(handle);
            _resizeHandles.Clear();
        }

        private void ResizeElement(FrameworkElement element, double deltaWidth, double deltaHeight, double offsetX, double offsetY)
        {
            var newWidth = Math.Max(10, element.Width + deltaWidth);
            var newHeight = Math.Max(10, element.Height + deltaHeight);

            Canvas.SetLeft(element, Canvas.GetLeft(element) - offsetX);
            Canvas.SetTop(element, Canvas.GetTop(element) - offsetY);

            element.Width = newWidth;
            element.Height = newHeight;

            PushUndoState();
        }

        private void PushUndoState()
        {
            string json = JsonSerializer.Serialize(CurrentLayout);
            var snapshot = JsonSerializer.Deserialize<OverlayLayout>(json);
            UndoStack.Push(snapshot);

            // Once we make a new change, the redo history is no longer valid
            RedoStack.Clear();
        }

        private void SelectElement(string elementId)
        {
            SelectedElementId = elementId;

            // Highlight it visually (you can update this later for fancier outlines)
            RenderOverlayToCanvas();
        }

        private void Undo()
        {
            if (UndoStack.Count > 0)
            {
                RedoStack.Push(CloneLayout(CurrentLayout));
                CurrentLayout = UndoStack.Pop();
                PreviewCanvas.Children.Clear();
                RenderOverlayToCanvas();
            }
        }

        private void Redo()
        {
            if (RedoStack.Count > 0)
            {
                UndoStack.Push(CloneLayout(CurrentLayout));
                CurrentLayout = RedoStack.Pop();
                PreviewCanvas.Children.Clear();
                RenderOverlayToCanvas();
            }
        }

        private OverlayLayout CloneLayout(OverlayLayout layout)
        {
            string json = JsonSerializer.Serialize(layout);
            return JsonSerializer.Deserialize<OverlayLayout>(json);
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _selectedElement = sender as FrameworkElement;
            ShowResizeHandles(_selectedElement);  
            _dragStartPoint = e.GetPosition(PreviewCanvas);
            _isDragging = true;
            _selectedElement.CaptureMouse();
        }

        private void PreviewCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == PreviewCanvas)
            {
                _selectedElement = null;
                ClearResizeHandles(); // ✅ Removes visual indicators
                Keyboard.ClearFocus(); // Optional: remove keyboard focus
            }
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
            UpdateResizeHandles(_selectedElement);
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
            SaveOverlay();
        }

        private void SaveAsOverlay_Click(object sender, RoutedEventArgs e)
        {
            SaveOverlayAs();
        }

        private void LoadOverlay_Click(object sender, RoutedEventArgs e)
        {
            LoadOverlay();
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

        private void SaveOverlay()
        {
            if (string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                SaveOverlayAs();
                return;
            }

            string json = JsonSerializer.Serialize(CurrentLayout, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(CurrentFilePath, json);
            CurrentLayout.LastSaved = DateTime.Now;
        }

        private void SaveOverlayAs()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Overlay Layout (*.json)|*.json",
                FileName = CurrentLayout.OverlayName.Replace(" ", "_") + ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                CurrentFilePath = dialog.FileName;
                SaveOverlay();
            }
        }

        private void LoadOverlay()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Overlay Layout (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                string json = File.ReadAllText(dialog.FileName);
                CurrentLayout = JsonSerializer.Deserialize<OverlayLayout>(json);
                CurrentFilePath = dialog.FileName;

                PreviewCanvas.Children.Clear();
                RenderOverlayToCanvas();
            }
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (ctrl && e.Key == Key.Z)
            {
                if (shift)
                    Redo();
                else
                    Undo();

                e.Handled = true;
                return;
            }

            if (ctrl && e.Key == Key.Y)
            {
                Redo();
                e.Handled = true;
                return;
            }

            if (ctrl && e.Key == Key.C)
            {
                CopySelectedElement();
                e.Handled = true;
                return;
            }

            if (ctrl && e.Key == Key.X)
            {
                CutSelectedElement();
                e.Handled = true;
                return;
            }

            if (ctrl && e.Key == Key.V)
            {
                if (shift)
                    PasteElementAsReference();
                else
                    PasteElementAsDuplicate();

                e.Handled = true;
                return;
            }

            // Arrow key movement
            if (SelectedElementId != null)
            {
                var element = CurrentLayout.Elements.FirstOrDefault(el => el.Id == SelectedElementId);
                if (element != null)
                {
                    int delta = shift ? 10 : 1;

                    switch (e.Key)
                    {
                        case Key.Up: element.Y -= delta; break;
                        case Key.Down: element.Y += delta; break;
                        case Key.Left: element.X -= delta; break;
                        case Key.Right: element.X += delta; break;
                        default: return;
                    }

                    RenderOverlayToCanvas();
                    e.Handled = true;
                }
            }
        }

        private OverlayElement? ClipboardElement = null;

        private void CopySelectedElement()
        {
            if (SelectedElementId == null) return;
            var element = CurrentLayout.Elements.FirstOrDefault(el => el.Id == SelectedElementId);
            if (element == null) return;

            ClipboardElement = CloneElement(element);
        }

        private void PasteElementAsDuplicate()
        {
            if (ClipboardElement == null) return;

            PushUndoState();

            var clone = CloneElement(ClipboardElement);
            clone.Id = Guid.NewGuid().ToString();
            clone.X += 10; // offset so it's visible
            clone.Y += 10;
            CurrentLayout.Elements.Add(clone);
            SelectElement(clone.Id);
            RenderOverlayToCanvas();
        }

        private void PasteElementAsReference()
        {
            // For now, treat it the same as duplicate until reference linking is implemented
            PasteElementAsDuplicate();
        }

        private void CutSelectedElement()
        {
            CopySelectedElement();
            DeleteSelectedElement();
        }

        private void DeleteSelectedElement()
        {
            if (SelectedElementId == null) return;
            PushUndoState();
            CurrentLayout.Elements.RemoveAll(el => el.Id == SelectedElementId);
            SelectedElementId = null;
            RenderOverlayToCanvas();
        }

        private OverlayElement CloneElement(OverlayElement original)
        {
            string json = JsonSerializer.Serialize(original);
            return JsonSerializer.Deserialize<OverlayElement>(json);
        }

        private void RenderOverlayToCanvas()
        {
            PreviewCanvas.Children.Clear();

            foreach (var element in CurrentLayout.Elements)
            {
                UIElement uiElement = null;

                if (element.Type == "text")
                {
                    var tb = new TextBlock
                    {
                        Text = element.Text ?? "[Empty]",
                        FontSize = element.FontSize,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.Color ?? "#FFFFFF"))
                    };

                    // Optional border for selected
                    if (element.Id == SelectedElementId)
                    {
                        tb.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)); // semi-transparent highlight
                    }

                    Canvas.SetLeft(tb, element.X);
                    Canvas.SetTop(tb, element.Y);

                    tb.MouseLeftButtonDown += (s, e) =>
                    {
                        SelectElement(element.Id);
                        e.Handled = true;
                    };

                    uiElement = tb;
                }

                // Add other types like images/shapes here later

                if (uiElement != null)
                    PreviewCanvas.Children.Add(uiElement);
            }
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
            PushUndoState();
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
            PushUndoState();
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

