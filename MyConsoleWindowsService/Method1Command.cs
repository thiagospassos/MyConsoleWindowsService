using Serilog;

namespace MyConsoleWindowsService
{
    public class Method1Command : ICommand
    {
        public void Execute()
        {
            Log.Information("Logging using Serilog");
        }
    }
}