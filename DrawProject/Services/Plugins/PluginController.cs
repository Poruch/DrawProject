using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DrawProject.Services.Plugins
{
    public class PluginController
    {
        private readonly IPlugin _plugin;
        private AssemblyLoadContext PlugContext { get; }
        public bool CanUnload => PlugContext != null;
        private bool _isEnabled = true;
        private bool _isUpload = true;

        public event EventHandler IsEnabledChanged;
        public event EventHandler IsUploadChanged;
        public ICommand UnloadCommand { get; }
        public string PluginPath
        {
            get
            {
                if (PlugContext == null) return "";
                return PlugContext.Assemblies.FirstOrDefault()?.Location?.ToString() ?? "";
            }
        }
        public string Name => _plugin.Name;
        public string Description => _plugin.Description;
        public string Author => _plugin.Author;
        public List<Type> InstrumentTypes => _plugin.Instruments;
        public List<Tool> Tools = new List<Tool>();
        public List<UIElement> UIInstruments = new List<UIElement>();
        public List<Type> FilterTypes => _plugin.Filters;
        public List<Filter> Filters = new List<Filter>();
        public List<UIElement> UIFilters = new List<UIElement>();
        public void UploadPlugin()
        {
            PlugContext.Unload();
            IsUpload = false;

        }

        public IPlugin Plugin => _plugin;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnIsEnabledChanged();
                }
            }
        }
        public bool IsUpload
        {
            get => _isUpload;
            set
            {
                if (_isUpload != value)
                {
                    _isUpload = value;
                    OnIsUploadChanged();
                }
            }
        }
        public PluginController(IPlugin plugin, AssemblyLoadContext plugDomain = null)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            this.PlugContext = plugDomain;
            UnloadCommand = new RelayCommand(UploadPlugin);
        }

        protected virtual void OnIsEnabledChanged()
        {
            IsEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnIsUploadChanged()
        {
            IsUploadChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
