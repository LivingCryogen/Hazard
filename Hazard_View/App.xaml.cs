using Hazard_Model.Assets;
using Hazard_Model.Core;
using Hazard_Model.DataAccess;
using Hazard_Model.Entities;
using Hazard_Model.EventArgs;
using Hazard_Share.Interfaces;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Interfaces.View;
using Hazard_Share.Interfaces.ViewModel;
using Hazard_Share.Services.Registry;
using Hazard_Share.Services.Serializer;
using Hazard_View.Services;
using Hazard_ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Hazard_View;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly string? _environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    private readonly bool _devMode;


    public App()
    {
        _devMode = DetermineDevMode();
        AppHost = BuildAppHost(out string[]? dataFileNames);

        /// Static class logger initiatialization
        var loggerFactory = AppHost.Services.GetRequiredService<ILoggerFactory>();
        BinarySerializer.InitializeLogger(loggerFactory);

        if (dataFileNames == null)
            throw new ArgumentNullException(nameof(dataFileNames));
        DataFileNames = dataFileNames;

        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    public IHost AppHost { get; init; }
    public string[] DataFileNames { get; init; }

    #region Methods
    private protected bool DetermineDevMode()
    {
        if (_environmentName != null && _environmentName.Equals(Environments.Development))
            return true;
        else
            return false;
    }
    private protected IHost BuildAppHost(out string[]? dataFileNames)
    {
        var host = Host.CreateDefaultBuilder();

        string[]? configuredDataFileNames = null;
        host.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{_environmentName}.json", optional: true, reloadOnChange: true);

            var builtConfig = config.Build();

            configuredDataFileNames = (string[]?)builtConfig.GetSection("DataFileNames").Get<string[]>();
        });

        if (_devMode) {
            host.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddFilter(logLevel => logLevel >= LogLevel.Trace);
                });
        }
        else {
            host.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddFilter(logLevel => logLevel >= LogLevel.Information);
                });
        }

        host.ConfigureServices(services =>
        {
            services.AddTransient<IRegistryInitializer, RegistryInitializer>();
            services.AddTransient<ITypeRelations, TypeRelations>();
            services.AddSingleton<ITypeRegister<ITypeRelations>, TypeRegister>();
            services.AddSingleton<IDataProvider>(serviceProvider =>
            {
                return new DataProvider(this.DataFileNames, serviceProvider.GetRequiredService<ITypeRegister<ITypeRelations>>(), serviceProvider.GetRequiredService<ILogger<DataProvider>>());
            });
            services.AddTransient<IAssetFetcher, AssetFetcher>();
            services.AddTransient<IAssetFactory, AssetFactory>();
            services.AddSingleton<IBootStrapperService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<BootStrapper>>();
                return new BootStrapper(this, logger);
            });
            services.AddTransient<ITerritoryChangedEventArgs, TerritoryChangedEventArgs>();
            services.AddTransient<IContinentOwnerChangedEventArgs, ContinentOwnerChangedEventArgs>();
            services.AddTransient<IRuleValues, RuleValues>();
            services.AddTransient<IBoard, EarthBoard>();
            services.AddTransient<IGeography, EarthBoard.EarthGeography>();
            services.AddTransient<IRegulator, Regulator>();
            services.AddTransient<IGame, Game>();
            services.AddTransient<IMainVM, MainVM>();
            services.AddTransient<IDialogState, DialogService>();
            services.AddTransient<IDispatcherTimer, Hazard_View.Services.Timer>();
        });

        var builtHost = host.Build();

        if (configuredDataFileNames != null) {
            dataFileNames = new string[configuredDataFileNames.Length];
            configuredDataFileNames.CopyTo(dataFileNames, 0);
        }
        else
            dataFileNames = null;

        return builtHost;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var bootStrapper = AppHost.Services.GetRequiredService<IBootStrapperService>();
        bootStrapper.InitializeGame();
    }
    protected void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (!_devMode) {
            string errorMsg = string.Format("An unhandled exception occurred: {0}. Source: {1}. Inner Exception: {2}. Data: {3}. HResult: {4}. StackTrace: {5}. The Application will now close.", e.Exception.Message, e.Exception.Source, e.Exception.InnerException, e.Exception.Data, e.Exception.HResult, e.Exception.StackTrace);
            MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            this.Shutdown();
        }
    }
    #endregion
}
