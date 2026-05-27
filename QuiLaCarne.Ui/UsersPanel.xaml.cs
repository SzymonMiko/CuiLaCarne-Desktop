using System.Windows.Controls;
using QuiLaCarne.ViewModels;

namespace QuiLaCarne.Ui;

public partial class UsersPanel : Page
{
    public UsersPanel(UsersPanelViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            await viewModel.LoadAsync();
        };
    }
}
