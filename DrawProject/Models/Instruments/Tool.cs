using System.Windows.Input;
using System.Windows.Media;

namespace DrawProject.Models.Instruments
{
    public abstract class Tool
    {
        public abstract void OnMouseDown(InstrumentContext context);
        public abstract void OnMouseMove(InstrumentContext context);
        public abstract void OnMouseUp(InstrumentContext context);
        public abstract void OnMouseLeave(InstrumentContext context);

    }
}
