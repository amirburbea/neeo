using System;
using System.Net;
using System.Windows;
using Neeo.Discovery;
using Neeo.Sdk;

namespace Remote.Demo;

/// <summary>
/// Interaction logic for Page1.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        //System.CommandLine

        //if (await BrainDiscovery.DiscoverAsync() is not { } brain)
        //{
        //    MessageBox.Show("Failed to resolve Brain.");
        //    Environment.Exit(1);
        //    return;
        //}
        Brain brain = new(IPAddress.Parse("192.168.253.143"));
        MainWindowViewModel viewModel = new(brain);
        await viewModel.StartServerAsync();
        (this.MainWindow = new MainWindow { DataContext = viewModel }).Show();
    }


}