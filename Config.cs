using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace QuestTools
{
    class Config
    {
        public int ServerPort { get; set; }

        private static Window _configWindow;

        public static void CloseWindow()
        {
            _configWindow.Close();
        }

        public static Window GetDisplayWindow()
        {
            if (_configWindow == null)
            {
                _configWindow = new Window();
            }

            string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (assemblyPath != null)
            {
                string xamlPath = Path.Combine(assemblyPath, "Plugins", "QuestTools", "Config.xaml");

                string xamlContent = File.ReadAllText(xamlPath);

                // This hooks up our object with our UserControl DataBinding
                _configWindow.DataContext = QuestToolsSettings.Instance;

                UserControl mainControl = (UserControl)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xamlContent)));
                _configWindow.Content = mainControl;
            }
            _configWindow.Width = 200;
            _configWindow.Height = 175;
            _configWindow.ResizeMode = ResizeMode.NoResize;
            _configWindow.Background = Brushes.DarkGray;

            _configWindow.Title = "QuestTools";

            _configWindow.Closed += ConfigWindow_Closed;
            Application.Current.Exit += ConfigWindow_Closed;

            return _configWindow;
        }

        static void ConfigWindow_Closed(object sender, System.EventArgs e)
        {
            QuestToolsSettings.Instance.Save();
            if (_configWindow == null)
                return;
            _configWindow.Closed -= ConfigWindow_Closed;
            _configWindow = null;
        }
    }
}
