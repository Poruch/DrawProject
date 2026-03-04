using DrawProject.Models.Filers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace DrawProject.Services.Plugins
{
    internal class MainPlugin : IPlugin
    {
        string IPlugin.Name => "Main plugin";

        string IPlugin.Description => "Main functional";

        string IPlugin.Author => "I";

        List<Type> IPlugin.Instruments
        {
            get
            {
                return Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Tool)))
                    .ToList();
            }
        }

        List<Type> IPlugin.Filters
        {
            get
            {
                return new List<Type>() { typeof(BlackBorder) };
            }
        }
    }
}
