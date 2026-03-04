using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DrawProject.Models;
using DrawProject.Services.Plugins;

namespace DrawProject.Controls
{
    public partial class PluginManagerDialog : Window
    {
        public ObservableCollection<PluginController> Plugins { get; set; }

        public PluginManagerDialog()
        {
            InitializeComponent();
            Plugins = new ObservableCollection<PluginController>();
            DataContext = this;
        }

        public PluginManagerDialog(List<PluginController> plugins) : this()
        {
            foreach (var plugin in plugins)
            {
                Plugins.Add(plugin);
                plugin.IsUploadChanged += Plugin_IsUploadChanged;
            }
        }

        private void Plugin_IsUploadChanged(object sender, EventArgs e)
        {
            var plugin = sender as PluginController;
            if (plugin != null && !plugin.IsUpload)
            {
                Plugins.Remove(plugin);
                plugin.IsUploadChanged -= Plugin_IsUploadChanged;
            }
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}