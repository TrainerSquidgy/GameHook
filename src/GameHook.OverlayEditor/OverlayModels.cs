using System;
using System.Collections.Generic;

namespace GameHook.OverlayEditor.Models
{
    public class OverlayLayout
    {
        public string OverlayName { get; set; } = "Untitled Overlay";
        public string MapperId { get; set; } = ""; // e.g., "pokemon-crystal"
        public int CanvasWidth { get; set; } = 1920;
        public int CanvasHeight { get; set; } = 1080;
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public List<OverlayElement> Elements { get; set; } = new();
     
    }

    public class OverlayElement
    {
        public string Type { get; set; } = "text"; // text, image, shape
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; }
        public string ImagePath { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 200;
        public int Height { get; set; } = 50;
        public string Link { get; set; } // e.g. mapper.properties.player.name
        public string Font { get; set; }
        public int FontSize { get; set; } = 16;
        public string Color { get; set; } = "#FFFFFF";
    }
}
