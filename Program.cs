namespace xbox_gamepad
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Check if running tests
            if (args.Length > 0 && args[0] == "--test")
            {
                Tests.TestRunner.Run();
                return;
            }

            // Normal GUI mode
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}