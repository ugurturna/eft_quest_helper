using System.IO;
using System.Windows;
using System.Windows.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

public partial class App : Application
{
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
        Loc.Set("tr");  // the profile's own UI language is applied when the view model loads it
        MainViewModel vm;
        try
        {
            vm = new MainViewModel();
        }
        catch (FileNotFoundException ex)
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
    }
}
