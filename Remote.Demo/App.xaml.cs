using System;
using System.Windows;
using Neeo.Discovery;

namespace Remote.Demo;

/// <summary>
/// Interaction logic for Page1.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (await BrainDiscovery.DiscoverAsync() is not { } brain)
        {
            MessageBox.Show("Failed to resolve Brain.");
            Environment.Exit(1);
            return;
        }
        MainWindowViewModel viewModel = new(brain);
        await viewModel.StartServerAsync();
        (this.MainWindow = new MainWindow { DataContext = viewModel }).Show();
    }


}