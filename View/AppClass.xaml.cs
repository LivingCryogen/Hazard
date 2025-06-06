﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Interfaces.View;
using Shared.Services.Options;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace View;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App(IHost host, bool devMode, IOptions<AppConfig> appConfig) : Application
{
    public IHost Host { get; init; } = host;
    private readonly IBootStrapperService _bootService = host.Services.GetRequiredService<IBootStrapperService>();
    public bool DevMode { get; init; } = devMode;
    public string InstallPath { get; init; } = appConfig.Value.AppPath;
    public ReadOnlyDictionary<string, string> DataFileMap { get; } = new(appConfig.Value.DataFileMap);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        /// Unhandled Exception catcher for production
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _bootService.InitializeGame();
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
}
