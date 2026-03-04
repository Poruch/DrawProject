using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawProject.Services.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        string Author { get; }

        List<Type> Instruments { get; }
        List<Type> Filters { get; }
    }
}
