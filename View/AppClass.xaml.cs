using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Stats;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using Shared.Services;
using Shared.Services.Configuration;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace View;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App(IOptions<AppConfig> options, ILogger<App> logger) : Application, IAppCommander
{
    private readonly IOptions<AppConfig> _options = options;
    private readonly ILogger<App> _logger = logger;

    public IHost? Host { get; set; }
    public bool DevMode { get; init; } = options.Value.DevMode;
    public string InstallPath { get; init; } = options.Value.AppPath;
    public string SaveFilePath { get; set; } = string.Empty;
    public ReadOnlyDictionary<string, string> DataFileMap { get; } = new(options.Value.DataFileMap);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        /// Unhandled Exception catcher for production
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        InitializeGame();
    }

    protected void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (DevMode)
            return;

        string errorMsg = string.Format(
            "An unhandled exception occurred: {0}. Source: {1}. Inner Exception: {2}. Data: {3}. HResult: {4}. StackTrace: {5}. The Application will now close.",
            e.Exception.Message, e.Exception.Source, e.Exception.InnerException, e.Exception.Data, e.Exception.HResult, e.Exception.StackTrace);
        MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }

    /// <summary>
    /// Initialize without save game or new game parameters, this is the default state (no game running).
    /// </summary>  
    public void InitializeGame()
    {
        if (Host == null)
        {
            _logger.LogError("The App's AppHost was null when attempting to initialize Game.");
            return;
        }
        var viewModel = Host.Services.GetRequiredService<IMainVM>();
        MainWindow mainWindow = new()
        {
            AppOptions = _options
        };
        mainWindow.Initialize(viewModel);
        MainWindow = mainWindow;
        MainWindow.Show();
    }
    public void InitializeGame(string fileName)
    {
        if (Host == null)
        {
            _logger.LogError("The App's AppHost was null when attempting to initialize Game.");
            return;
        }
        if (string.IsNullOrEmpty(fileName))
            return;
        SaveFilePath = fileName;
        MainWindow mainWindow = new()
        {
            AppOptions = _options
        };
        MainWindow = mainWindow;

        _logger.LogInformation($"Closing old Windows...");
        foreach (Window window in Current.Windows)
        {
            if (window == mainWindow)
                continue;
            if (window is MainWindow oldWindow)
                oldWindow.SetShutDown(false);
            window.Close();
        }

        var viewModel = Host.Services.GetRequiredService<IMainVM>();
        _logger.LogInformation("Initializing game from source: {FileName}.", fileName);
        viewModel.Initialize([], [], fileName);
        ((MainWindow)MainWindow).Initialize(viewModel);
        MainWindow.Show();
    }
    public void InitializeGame((string Name, string Color)[] namesAndColors)
    {
        if (Host == null)
        {
            _logger.LogError("The App's AppHost was null when attempting to initialize Game.");
            return;
        }
        var playerNames = namesAndColors.Select(item => item.Name).ToArray();
        var playerColors = namesAndColors.Select(item => item.Color).ToArray();
        SaveFilePath = string.Empty;

        MainWindow mainWindow = new()
        {
            AppOptions = _options
        };
        MainWindow = mainWindow;
        foreach (Window window in Current.Windows)
        {
            if (window == mainWindow)
                continue;
            if (window is MainWindow oldWindow)
                oldWindow.SetShutDown(false);
            window.Close();
        }

        var viewModel = Host.Services.GetRequiredService<IMainVM>();
        viewModel.Initialize(playerNames, playerColors, null);
        ((MainWindow)MainWindow).Initialize(viewModel);
        MainWindow.Show();
    }
}
