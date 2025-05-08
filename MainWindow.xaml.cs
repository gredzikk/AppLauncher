using System.Reflection;
using System.Windows;

namespace app_launcher
{
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();

            this.Title = $"AppLauncher v{GetVersionString()}";
            FileLogger.LogInfo($"Application starting. Version: {GetVersionString()}");
        }

        /// <summary>
        /// Pobierz wersję aplikacji z atrybutów zestawu
        /// </summary>
        /// <returns>String w postaci major.minor.build.revision</returns>
        private string GetVersionString() {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }


    }
}