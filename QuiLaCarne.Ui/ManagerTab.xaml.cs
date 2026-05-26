using QuiLaCarne.ViewModels;
using System.Windows.Controls;

namespace QuiLaCarne.Ui;

public partial class ManagerTab : Page
{
    public ManagerTab(ManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadDishesAsync();
    }
}
