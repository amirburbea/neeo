using System.ComponentModel;
using System.Windows;

namespace Remote.Demo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() => this.InitializeComponent();

    protected async override void OnClosing(CancelEventArgs e)
    {
        await ((MainWindowViewModel)this.DataContext).StopServerAsync();
        base.OnClosing(e);
    }
}