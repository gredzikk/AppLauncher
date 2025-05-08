using System.Configuration;
using System.Data;
using System.Reflection;
using System.Windows;

namespace app_launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            FileLogger.Initialize();
            FileLogger.LogInfo($"Application starting.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            FileLogger.LogInfo("Application exiting.");
            base.OnExit(e);
        }
    }
}

