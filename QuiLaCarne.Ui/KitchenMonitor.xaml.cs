using System.Windows.Controls;
using QuiLaCarne.ViewModels;

namespace QuiLaCarne.Ui;

public partial class KitchenMonitor : Page
{
    public KitchenMonitor(KitchenMonitorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadOrdersAsync();
    }
}
