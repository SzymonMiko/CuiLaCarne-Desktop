using QuiLaCarne.ViewModels;
using System.Windows;

namespace QuiLaCarne.Ui;

public partial class KitchenMonitor : Window
{
    public KitchenMonitor(KitchenMonitorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadOrdersAsync();
    }
}
