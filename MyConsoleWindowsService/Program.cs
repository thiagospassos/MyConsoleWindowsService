using System;
using System.Linq;
using System.Threading;
using Autofac;
using AutofacSerilogIntegration;
using CommandLine;
using DasMulli.Win32.ServiceUtils;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace MyConsoleWindowsService
{
    class Program
    {
        class Options
        {
            [Option('p', "process", Required = true, HelpText = "Process name")]
            public ProcessToRun Process { get; set; }

            [Option(Default = false, HelpText = "Run as Service")]
            public bool RunAsService { get; set; }

            [Option("period", Default = 0, HelpText = "Period for process to run")]
            public int Period { get; set; }
        }

        private static IConfiguration Configuration { get; set; }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts =>
                {
                    var configBuilder = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.Local.json", optional: true);

                    Configuration = configBuilder.Build();

                    var container = IocConfig();

                    ICommand cmd = null;
                    switch (opts.Process)
                    {
                        case ProcessToRun.Method1:
                            cmd = container.Resolve<Method1Command>();
                            break;
                        case ProcessToRun.Method2:
                            Console.WriteLine("Do whatever method two wants");
                            break;
                        case ProcessToRun.Method3:
                            Console.WriteLine("Do whatever method three wants");
                            break;
                    }

                    if (cmd != null)
                    {
                        if (opts.RunAsService)
                        {
                            var service = new RunAsService(cmd);
                            var host = new Win32ServiceHost(service);
                            host.Run();
                        }
                        else
                        {
                            cmd.Execute();
                        }
                    }
                });

            Console.WriteLine("---------- PRESS ANY KEY TO EXIT ----------");
            Console.ReadKey();
        }

        public static IContainer IocConfig()
        {
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(typeof(Method1Command).Assembly)
                .Where(x => x.Name.EndsWith("Command"))
                .AsSelf();

            Log.Logger = CreateLogger(Configuration);
            builder.RegisterLogger();

            IContainer container = builder.Build();
            return container;
        }

        public static Logger CreateLogger(IConfiguration configuration)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("ApplicationName", typeof(Program).Assembly.GetName().Name)
                .Enrich.WithProperty("AppDomain", AppDomain.CurrentDomain)
                .Enrich.WithProperty("RuntimeVersion", Environment.Version)
                .CreateLogger();
        }
    }
}
