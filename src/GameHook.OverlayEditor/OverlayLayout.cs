using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameHook.OverlayEditor
{
    public class OverlayLayout
    {
        // Name of the overlay (user-defined)
        public string Name { get; set; }

        // Reference to the mapper file (this will link the overlay to a specific mapper)
        public string Mapper { get; set; }

        // Dimensions of the canvas (1920x1080 by default)
        public int CanvasWidth { get; set; }
        public int CanvasHeight { get; set; }

        // List of elements (text, images, shapes, etc.)
        public List<OverlayElement> Elements { get; set; } = new List<OverlayElement>();
    }

}
