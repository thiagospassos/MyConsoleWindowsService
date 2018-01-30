Alright, it seems like over and over again, for one reason or another, we end up creating a Windows service in the projects I work on. And even though I've done it so many time, I still dig into previous projects to refresh my memory and get started. 

The idea of this post is to show some of the things I think are essential to setup in a console app that will eventually turn into a Windows service. 

> The whole source code covered in this post is [here](https://github.com/thiagospassos/MyConsoleWindowsService)

##Dependencies  

####[CommandLineParser](https://www.nuget.org/packages/CommandLineParser/)  
What is a console application without a nice way of handling its arguments. It's amazing how many times I had to dig into the code to remember what argument I needed to pass to run some specific process. This package will help with that. 

With very little configuration, you will get a very nice console experience. First you create a class with all sort of arguments you're going to need for your console app. That's what mine looks like:
```csharp
class Options
{
    [Option('p', "process", Required = true, HelpText = "Process name")]
    public ProcessToRun Process { get; set; }

    [Option(Default = false, HelpText = "Run as Service")]
    public bool RunAsService { get; set; }

    [Option("period", Default = 0, HelpText = "Period for process to run")]
    public int Period { get; set; }
}
```

And in our `Main` method, we will parse and handle the arguments:  
```csharp
CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed(opts =>
{
    switch (opts.Process)
    {
        case ProcessToRun.Method1:
            Console.WriteLine("Do whatever method one wants");
            break;
        case ProcessToRun.Method2:
            Console.WriteLine("Do whatever method two wants");
            break;
    }
});
```

The beauty of doing this is that your console app will actually help you to run it.  
![2018-01-30_21-31-35](/content/images/2018/01/2018-01-30_21-31-35.jpg)

####[Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/)  
Always getting my settings from a json file. And having the ability to bring some local configuration as some devs will have some special connection strings or some peculiarities.
```csharp
private static IConfiguration Configuration { get; set; }

//This is inside the main method
var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.Local.json", optional: true);

Configuration = configBuilder.Build();
```

####[Serilog](https://www.nuget.org/packages/Serilog/2.7.1-dev-00950) + [Seq](https://www.nuget.org/packages/Serilog.Sinks.Seq/)
If you haven't heard about Serilog and Seq, you really should. It's an amazing way to log everything coming out of your application. There's also some interesting facts about using Seq in a console application, here's a nice [post](https://jkdevblog.wordpress.com/2018/01/22/why-is-serilog-not-writting-to-seq/) about it by [Jernej Kavka](https://twitter.com/jernej_kavka).

Basically I create a method to setup Serilog globally so every single log includes some details of the application.
```csharp
public static Logger CreateLogger(IConfiguration configuration)
{
    return new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.WithProperty("ApplicationName", typeof(Program).Assembly.GetName().Name)
        .Enrich.WithProperty("AppDomain", AppDomain.CurrentDomain)
        .Enrich.WithProperty("RuntimeVersion", Environment.Version)
        .CreateLogger();
}
```

####[Autofac](https://www.nuget.org/packages/Autofac/)  
Whenever I talk about dependency injection, the first one that comes to mind is Autofac. The out-of-the-box IoC for dotnet core is pretty good now a days, but some of the features are not there, so I end up always bringing Autofac. And that's what I do to start with. I usually create a method to register all my dependency injections:
```csharp
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

//then inside the main method
var container = IocConfig();

//then I can resolve any dependency
container.Resolve<Method1Command>().Execute();
```

####[dasMulli/dotnet-win32-service](https://github.com/dasMulli/dotnet-win32-service)  
Finally, we need a way to run as a service. There are few ways to achieve that, but after trying several ways, I still prefer this one. So, here what happens. 

First I create a class inheriting from `IWin32Service` that will basically run my command.
```csharp
public class RunAsService : IWin32Service
{
    private readonly ICommand _command;

    public RunAsService(ICommand command)
    {
        _command = command;
    }

    public void Start(string[] startupArguments, ServiceStoppedCallback serviceStoppedCallback)
    {
        // Here's the command I'm running
        _command.Execute();
    }

    public void Stop()
    {
        //todo: whatever
    }

    public string ServiceName { get; } = "My Console Windows Service";
}
```

If you remember in my `Options`, I've got the flag `RunAsService` which is default to `false`, so when I get to the point of setting up the service, I'll set it to `true` and handle that in my main method:

```csharp
ICommand cmd = null;
switch (opts.Process)
{
    case ProcessToRun.Method1:
        cmd = container.Resolve<Method1Command>();
        break;
}

if (cmd != null)
{
    if (opts.RunAsService)
    {
        //this will host my console app as a service
        var service = new RunAsService(cmd);
        var host = new Win32ServiceHost(service);
        host.Run();
    }
    else
    {
        cmd.Execute();
    }
}
```

Now, what about we give it a try and create our Windows service. Open Cmder or Cmd with elevated privileges and run try this command (make sure you remove breaks/new lines and the place holders)

```
sc.exe create [SERVICE NAME] binPath= "C:\Program Files\dotnet\dotnet.exe 
[FULL PATH OF YOUR DLL] -p Method1 --runasservice" 
DisplayName= "[DISPLAY NAME OF YOUR SERVICE]" start= delayed-auto
```

Hooray!!! It's good to go
![2018-01-30_22-53-46](/content/images/2018/01/2018-01-30_22-53-46.jpg)

Hope it helps. 

It will definitely help me in the future.

Cheers.