using System.Runtime.InteropServices;

namespace MoveHelper;

internal static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int processId);

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(arg => arg.Equals("--probe", StringComparison.OrdinalIgnoreCase)))
        {
            _ = AttachConsole(-1);
            Console.WriteLine(WindowFinder.ProbeForeground());
            return;
        }

        using var mutex = new Mutex(true, @"Local\EricLeigh.MoveHelper", out bool created);
        if (!created)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
