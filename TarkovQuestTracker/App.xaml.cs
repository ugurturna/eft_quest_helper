using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

public partial class App : Application
{
    /// <summary>"--wait-pid &lt;pid&gt;": started by the updater, wait for the old version to exit first.</summary>
    const string WaitArg = "--wait-pid";

    /// <summary>After a self-update: wait for the old process, then delete the files it left as *.old.</summary>
    static void FinishUpdate(string[] args)
    {
        var i = Array.IndexOf(args, WaitArg);
        if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var pid))
        {
            try
            {
                using var old = Process.GetProcessById(pid);
                old.WaitForExit(15000);
            }
            catch (ArgumentException) { }  // already gone
        }
        if (MainViewModel.CanSelfUpdate) new AppUpdater(AppContext.BaseDirectory).CleanupOldFiles();
    }

    public App()
    {
        // Fluent dark theme; ThemeMode is still marked experimental (WPF0001)
#pragma warning disable WPF0001
        ThemeMode = ThemeMode.Dark;
#pragma warning restore WPF0001
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        FinishUpdate(e.Args);
        Loc.Set("tr");  // the profile's own UI language is applied when the view model loads it
        MainViewModel vm;
        try
        {
            vm = new MainViewModel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Tarkov Quest Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var overlay = new OverlayWindow(vm);
        var main = new MainWindow(vm, overlay);
        MainWindow = main;
        main.Show();
        // hotkeys live on the overlay's window handle, so create it even when the overlay starts hidden
        new WindowInteropHelper(overlay).EnsureHandle();
        if (vm.Settings.OverlayVisible) overlay.Show();
        // flea prices for the mode being played, once per start, in the background
        _ = vm.Flea.PrepareAsync(vm.Settings.Mode, vm.Settings.Language);
        vm.RestartRequested += () =>
        {
            // the new exe waits for this process to exit (hotkeys, files) before it starts
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"{WaitArg} {Environment.ProcessId}") { UseShellExecute = false });
            Shutdown();
        };
        _ = vm.CheckUpdatesAsync(manual: false);
    }
}
