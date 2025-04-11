using System;

namespace GameHook.OverlayEditor
{
    // Represents a single element in the overlay (could be text, image, etc.)
    public class OverlayElement
    {
        // Type of element (text, image, etc.)
        public string Type { get; set; }

        // Unique identifier for the element (e.g., "healthText", "speciesImage")
        public string Id { get; set; }

        // Content for text elements (e.g., "Player Health: {player.hp}")
        public string Text { get; set; }

        // Path to image for image elements
        public string ImagePath { get; set; }

        // Position on the canvas
        public Position Position { get; set; }

        // Size of the element
        public Size Size { get; set; }

        // Link to a GameHook property (e.g., {player.hp})
        public string Link { get; set; }

        // Customization for fonts, colors, etc. for text elements
        public string Font { get; set; }
        public int FontSize { get; set; }
        public string Color { get; set; }
    }

    // Represents the position (X, Y) of an element on the canvas
    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    // Represents the size (width and height) of an element on the canvas
    public class Size
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
