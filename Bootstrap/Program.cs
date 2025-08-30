using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model.Assets;
using Model.Core;
using Model.DataAccess;
using Model.Entities;
using Model.EventArgs;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using Shared.Services;
using Shared.Services.Configuration;
using Shared.Services.Helpers;
using Shared.Services.Registry;
using Shared.Services.Serializer;
using System.IO;
using System.Runtime.Versioning;
using View;
using View.Services;
using ViewModel;
using Windows.ApplicationModel;

[assembly: SupportedOSPlatform("windows10.0.10240.0")]

namespace Bootstrap
{
    public class Program
    {
        [STAThread]
        static void Main()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            bool devMode = environmentName == Environments.Development;

            string appPath = DetermineAppPath();

            using var appHost = BuildAppHost(devMode, environmentName, appPath);
            InitializeStaticLoggers(appHost);
            var app = appHost.Services.GetRequiredService<App>();
            app.Host = appHost;
            app.InitializeComponent();
            app.Run();
        }


        public static string DetermineAppPath()
        {
            // Check if application is running as MSIX package (ie, was installed and running after public distribution)
            // If so, it's containerized and Environment.CurrentDirectory won't work.
            try
            {
                return Package.Current.InstalledLocation.Path;
            }
            catch (Exception)
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            }
        }

        private static protected IHost BuildAppHost(bool devMode, string environmentName, string appPath)
        {
            var host = Host.CreateDefaultBuilder();

            return host.ConfigureAppConfiguration((context, config) =>
                   {
                       config.SetBasePath(appPath);
                       config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                           .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
                   })
                   .ConfigureServices((context, services) =>
                   {
                       services.Configure<AppConfig>(context.Configuration);
                       services.PostConfigure<AppConfig>(options =>
                       {
                           options.AppPath = appPath;
                           options.DevMode = devMode;
                           options.StatRepoFilePath = DataFileFinder.FindFiles(appPath, options.StatRepoFileName)[0];
                           foreach (string dataFileName in options.DataFileNames)
                           {
                               string dataFile = DataFileFinder.FindFiles(appPath, dataFileName)[0];
                               options.DataFileMap.Add(dataFileName, dataFile);
                           }
                           foreach (string soundFileName in options.SoundFileNames)
                           {
                               string soundFile = DataFileFinder.FindFiles(appPath, soundFileName)[0];
                               options.SoundFileMap.Add(soundFileName, soundFile);
                           }
                       });
                       services.AddSingleton<IRegistryInitializer, RegistryInitializer>();
                       services.AddTransient<ITypeRelations, TypeRelations>();
                       services.AddSingleton<ITypeRegister<ITypeRelations>, TypeRegister>();
                       services.AddSingleton<IDataProvider, DataProvider>();
                       services.AddTransient<IAssetFetcher, AssetFetcher>();
                       services.AddTransient<IAssetFactory, AssetFactory>();
                       services.AddTransient<IRuleValues, RuleValues>();
                       services.AddTransient<IGameService, ViewModel.Services.GameService>();
                       services.AddTransient<ITerritoryChangedEventArgs, TerritoryChangedEventArgs>();
                       services.AddTransient<IContinentOwnerChangedEventArgs, ContinentOwnerChangedEventArgs>();
                       services.AddTransient<IRuleValues, RuleValues>();
                       services.AddTransient<IBoard, EarthBoard>();
                       services.AddTransient<IRegulator, Regulator>();
                       services.AddTransient<IStatTracker, Model.Stats.Services.StatTracker>();
                       services.AddTransient<IMainVM, MainVM>();
                       services.AddTransient<IDialogState, DialogService>();
                       services.AddTransient<IDispatcherTimer, View.Services.Timer>();
                       services.AddSingleton<App>();
                       services.AddSingleton<IAppCommander>(provider => provider.GetRequiredService<App>());
                       services.AddSingleton<StatRepo>();
                   })
                   .ConfigureLogging(logging =>
                   {
                       logging.ClearProviders();
                       logging.AddConsole();
                       logging.AddDebug();
                       if (devMode)
                           logging.AddFilter(logLevel => logLevel >= LogLevel.Trace);
                       else
                           logging.AddFilter(logLevel => logLevel >= LogLevel.Information);
                   })
                   .Build();
        }

        private static protected void InitializeStaticLoggers(IHost host)
        {
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            BinarySerializer.InitializeLogger(loggerFactory);

            // More here if/when needed....
        }
    }
}
