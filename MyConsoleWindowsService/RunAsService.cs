using DasMulli.Win32.ServiceUtils;
using Serilog;

namespace MyConsoleWindowsService
{
    public class RunAsService : IWin32Service
    {
        private readonly ICommand _command;

        public RunAsService(ICommand command)
        {
            _command = command;
        }

        public void Start(string[] startupArguments, ServiceStoppedCallback serviceStoppedCallback)
        {
            _command.Execute();
        }

        public void Stop()
        {
            Log.CloseAndFlush();
        }

        public string ServiceName { get; } = "My Console Windows Service";
    }
}