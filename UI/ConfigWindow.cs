using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace QuestTools.UI
{
    public class ConfigWindow
    {
        private static Window _configWindow;

        public static void CloseWindow()
        {
            _configWindow.Close();
        }

        private static string replaceNamespace(string xaml, string xmlns)
        {
            var asmName = Assembly.GetExecutingAssembly().GetName().Name;
            string newxmlns = xmlns.Insert(xmlns.Length - 1, ";assembly=" + asmName);
            return xaml.Replace(xmlns, newxmlns);
        }

        public static Window GetDisplayWindow()
        {
            if (_configWindow == null)
            {
                _configWindow = new Window();
            }
            try
            {

                string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                if (assemblyPath != null)
                {
                    string xamlPath = Path.Combine(assemblyPath, "Plugins", "QuestTools","UI", "ConfigWindow.xaml");

                    string xamlContent = File.ReadAllText(xamlPath);

                    xamlContent = replaceNamespace(xamlContent, "xmlns:qt=\"clr-namespace:QuestTools\"");
                    xamlContent = replaceNamespace(xamlContent, "xmlns:ui=\"clr-namespace:QuestTools.UI\"");
                    xamlContent = replaceNamespace(xamlContent, "xmlns:nav=\"clr-namespace:QuestTools.Navigation\"");
                    xamlContent = replaceNamespace(xamlContent, "xmlns:h=\"clr-namespace:QuestTools.Helpers\"");

                    //xamlContent = xamlContent.Replace("xmlns:qt=\"clr-namespace:QuestTools\"",
                    //    "xmlns:qt=\"clr-namespace:QuestTools;assembly=" + asmName + "\"");
                    //xamlContent = xamlContent.Replace("xmlns:ui=\"clr-namespace:QuestTools.UI\"",
                    //    "xmlns:ui=\"clr-namespace:QuestTools.UI;assembly=" + asmName + "\"");
                    //xamlContent = xamlContent.Replace("xmlns:nav=\"clr-namespace:QuestTools.Navigation\"",
                    //    "xmlns:nav=\"clr-namespace:QuestTools.Navigation;assembly=" + asmName + "\"");
                    //xamlContent = xamlContent.Replace(,
                    //    "xmlns:nav=\"clr-namespace:QuestTools.Helpers;assembly=" + asmName + "\"");

                    // This hooks up our object with our UserControl DataBinding
                    _configWindow.DataContext = QuestToolsSettings.Instance;
                    _configWindow.Resources["LegendaryGems"] = DataDictionary.LegendaryGems;

                    UserControl mainControl = (UserControl)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xamlContent)));
                    _configWindow.Content = mainControl;
                }
                _configWindow.MinWidth = 600;
                _configWindow.Width = 600;
                _configWindow.MinHeight = 450;
                _configWindow.Height = 450;
                _configWindow.ResizeMode = ResizeMode.CanResize;
                _configWindow.Foreground = Brushes.White;
                _configWindow.Background = Brushes.DarkGray;

                _configWindow.Title = "QuestTools";

                _configWindow.Closed += ConfigWindow_Closed;
                Application.Current.Exit += ConfigWindow_Closed;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error opening QuestTools Config Window: {0}", ex);
            }
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
