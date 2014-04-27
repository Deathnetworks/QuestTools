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

        private static Window configWindow;

        public static void CloseWindow()
        {
            configWindow.Close();
        }

        public static Window GetDisplayWindow()
        {
            if (configWindow == null)
            {
                configWindow = new Window();
            }

            string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string xamlPath = Path.Combine(assemblyPath, "Plugins", "QuestTools", "Config.xaml");

            string xamlContent = File.ReadAllText(xamlPath);

            // This hooks up our object with our UserControl DataBinding
            configWindow.DataContext = QuestToolsSettings.Instance;

            UserControl mainControl = (UserControl)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xamlContent)));
            configWindow.Content = mainControl;
            configWindow.Width = 200;
            configWindow.Height = 175;
            configWindow.ResizeMode = ResizeMode.NoResize;
            configWindow.Background = Brushes.DarkGray;

            configWindow.Title = "QuestTools";

            configWindow.Closed += ConfigWindow_Closed;
            Demonbuddy.App.Current.Exit += ConfigWindow_Closed;

            return configWindow;
        }

        static void ConfigWindow_Closed(object sender, System.EventArgs e)
        {
            QuestToolsSettings.Instance.Save();
            if (configWindow != null)
            {
                configWindow.Closed -= ConfigWindow_Closed;
                configWindow = null;
            }
        }
    }
}
