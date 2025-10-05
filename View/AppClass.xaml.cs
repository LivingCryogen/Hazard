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
public partial class App : Application, IAppCommander
{
    private readonly IOptions<AppConfig> _options;
    private readonly ILogger<App> _logger;

    public IHost? Host { get; set; }
    public bool DevMode { get; init; }
    public string InstallPath { get; init; }
    public string SaveFilePath { get; set; } = string.Empty;
    public ReadOnlyDictionary<string, string> DataFileMap { get; }

    public App(IOptions<AppConfig> options, ILogger<App> logger)
    {
        _options = options;
        _logger = logger;
        DevMode = _options.Value.DevMode;
        InstallPath = _options.Value.AppPath;
        DataFileMap = new(_options.Value.DataFileMap);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        /// Unhandled Exception catcher for production
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        Initialize(null, null); // Default to no parameters
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

    /// <inheritdoc cref="IAppCommander.Initialize(string?, (string Name, string Color)[]?)"/>/>
    public void Initialize(string? fileName, (string Name, string Color)[]? userSelections)
    {
        if (Host == null)
        {
            _logger.LogError("The App's AppHost was null when attempting to initialize Game.");
            return;
        }
        var viewModel = Host.Services.GetRequiredService<IMainVM>();
        if (viewModel.StatRepo == null)
        {
            _logger.LogWarning("The IMainVM's StatRepo was null when attempting initialization.");
            return;
        }
        else
        {
            if (!viewModel.StatRepo.Load())
                _logger.LogWarning("StatRepo failed to load from configured file path ({StatRepoFilePath}). Starting with empty repository.", _options.Value.StatRepoFilePath);
            else
                _logger.LogInformation("StatRepo successfully loaded from configured file path ({StatRepoFilePath}).", _options.Value.StatRepoFilePath);
        }

        MainWindow mainWindow = new()
        {
            AppOptions = _options
        };

        _logger.LogInformation($"Closing old Windows...");
        foreach (Window window in Current.Windows)
        {
            if (window == mainWindow)
                continue;
            if (window is MainWindow oldWindow)
                oldWindow.SetShutDown(false);
            window.Close();
        }

        // Initialize ViewModel from Save, or initialize from New Game Window user selections
        if (!string.IsNullOrEmpty(fileName))
        {
            SaveFilePath = fileName;
            _logger.LogInformation("Initializing game from source: {FileName}.", fileName);
            viewModel.Initialize([], [], fileName);
        }
        else if (userSelections != null && userSelections.Length > 0)
            viewModel.Initialize(
                [.. userSelections.Select(item => item.Name)],
                [.. userSelections.Select(item => item.Color)],
                null);

        mainWindow.Initialize(viewModel);
        MainWindow = mainWindow;
        MainWindow.Show();
    }
}
