using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawProject.Models.Instruments
{
    public abstract class Tool
    {
        public string Name { get; set; }
        public string ToolTip { get; set; }
        public string CursorPath { get; set; } = "";

        public Image Icon { get; set; }
        public bool CommitOnMouseUp = true;
        public abstract void OnMouseDown(InstrumentContext context);
        public abstract void OnMouseMove(InstrumentContext context);
        public abstract void OnMouseUp(InstrumentContext context);
        public abstract void OnMouseLeave(InstrumentContext context);

    }
}
