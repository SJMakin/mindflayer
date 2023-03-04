using log4net;
using System.IO;

namespace MindFlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(App));

        public App()
        {
            var logConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(logConfigFilePath));
            log.Debug("Application started.");
        }
    }
}
