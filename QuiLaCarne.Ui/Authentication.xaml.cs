using System.Windows.Controls;
using QuiLaCarne.ViewModels;

namespace QuiLaCarne.Ui;

public partial class Authentication : Page
{
    public Authentication(AuthenticationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
