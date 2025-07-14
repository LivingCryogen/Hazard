using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Assets;
using Model.Core;
using Model.DataAccess;
using Model.Entities;
using Model.EventArgs;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using Shared.Services.Helpers;
using Shared.Services.Options;
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
        public static BootStrapper? BootService { get; private set; } = null;

        [STAThread]
        static void Main()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            bool devMode = environmentName == Environments.Development;

            string appPath = DetermineAppPath();

            using var appHost = BuildAppHost(devMode, environmentName, appPath);
            BootService = (BootStrapper)appHost.Services.GetRequiredService<IBootStrapperService>();
            InitializeStaticLoggers(appHost);
            var app = new App(appHost, devMode, appHost.Services.GetRequiredService<IOptions<AppConfig>>());
            BootService.MainApp = app;
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
            string statFileName = string.Empty;
            int statVersionNo = 0;
            List<string> settingDataFileNames = [];
            List<string> settingSoundFileNames = [];
            Dictionary<string, string> dataFileLocations = [];
            Dictionary<string, string> soundFileLocations = [];
            string cardDataSearchString = string.Empty;
            return
                host.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(appPath);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

                    var builtConfig = config.Build();

                    statFileName = (string?)builtConfig.GetValue(typeof(string), "StatRepoFileName") ?? string.Empty;
                    statVersionNo = (int?)builtConfig.GetValue(typeof(int), "StatVersionNo") ?? 1;
                    settingDataFileNames.AddRange(builtConfig.GetSection("DataFileNames").Get<string[]>() ?? []);
                    settingSoundFileNames.AddRange(builtConfig.GetSection("SoundFileNames").Get<string[]>() ?? []);
                    cardDataSearchString = (string?)(builtConfig.GetValue(typeof(string), "CardDataSearchString")) ?? string.Empty;

                    for (int i = 0; i < settingDataFileNames.Count; i++)
                    {
                        // Appconfig should only have 1 path discovered per file name, so we take only the first in the returned collection.
                        dataFileLocations.Add(
                            settingDataFileNames[i],
                            DataFileFinder.FindFiles(appPath, settingDataFileNames[i])[0]);
                    }
                    for (int i = 0; i < settingSoundFileNames.Count; i++)
                    {
                        // Appconfig should only have 1 path discovered per file name, so we take only the first in the returned collection.
                        soundFileLocations.Add(
                            settingSoundFileNames[i],
                            DataFileFinder.FindFiles(appPath, settingSoundFileNames[i])[0]);
                    }
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
                .ConfigureServices(services =>
                {
                    services.Configure<AppConfig>(options =>
                    {
                        options.AppPath = appPath;
                        options.StatRepoFilePath = Path.Combine(statFileName, appPath);
                        options.StatVersion = statVersionNo;
                        if (dataFileLocations.Count > 0)
                            options.DataFileMap = dataFileLocations;
                        if (soundFileLocations.Count > 0)
                            options.SoundFileMap = soundFileLocations;
                        options.CardDataSearchString = cardDataSearchString;
                    });
                    services.AddSingleton<IRegistryInitializer, RegistryInitializer>();
                    services.AddTransient<ITypeRelations, TypeRelations>();
                    services.AddSingleton<ITypeRegister<ITypeRelations>, TypeRegister>();
                    services.AddSingleton<IDataProvider, DataProvider>();
                    services.AddTransient<IAssetFetcher, AssetFetcher>();
                    services.AddTransient<IAssetFactory, AssetFactory>();
                    services.AddSingleton<IBootStrapperService>(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<BootStrapper>>();
                        return new BootStrapper(logger);
                    });
                    services.AddTransient<IGameService, ViewModel.Services.GameService>();
                    services.AddTransient<ITerritoryChangedEventArgs, TerritoryChangedEventArgs>();
                    services.AddTransient<IContinentOwnerChangedEventArgs, ContinentOwnerChangedEventArgs>();
                    services.AddTransient<IRuleValues, RuleValues>();
                    services.AddTransient<IBoard, EarthBoard>();
                    services.AddTransient<IRegulator, Regulator>();
                    services.AddTransient<IGame, Game>();
                    services.AddTransient<IMainVM, MainVM>();
                    services.AddTransient<IDialogState, DialogService>();
                    services.AddTransient<IDispatcherTimer, View.Services.Timer>();
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
