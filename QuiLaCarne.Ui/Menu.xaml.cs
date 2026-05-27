using System.Windows.Controls;
using QuiLaCarne.ViewModels;

namespace QuiLaCarne.Ui;

public partial class Menu : Page
{
    public Menu(MenuViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
