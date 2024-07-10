using log4net;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;

namespace MindFlayer;

public partial class App : System.Windows.Application
{
    private static readonly ILog log = LogManager.GetLogger(typeof(App));

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var logConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
        log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(logConfigFilePath));
        log.Debug("Application started.");

        AppDomain.CurrentDomain.FirstChanceException += FirstChanceHandler;
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
    }

    private static void FirstChanceHandler(object source, FirstChanceExceptionEventArgs e)
    {
        log.Warn($"FirstChanceException event raised in {AppDomain.CurrentDomain.FriendlyName}: {e.Exception.Message}");
    }

    private static void MyHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Exception e = (Exception)args.ExceptionObject;
        log.Error("MyHandler caught : " + e.Message);
        log.Error($"Runtime terminating: {args.IsTerminating}");
    }
}
