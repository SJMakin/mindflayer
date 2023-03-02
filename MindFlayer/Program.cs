using System.Text;

namespace MindFlayer;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Main());


    }
    public static string EscapePipe(string input)
    {
        return input.Replace(@"\", @"\\").Replace("¦", @"\¦").Replace("|", "¦");
    }

    public static string UnescapePipe(string input)
    {
        return input.Replace(@"\\", @"\").Replace(@"\¦", @"¦").Replace("¦", "¦"); ;
    }


}
