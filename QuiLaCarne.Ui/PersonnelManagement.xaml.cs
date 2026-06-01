using System.Windows.Controls;
using QuiLaCarne.ViewModels;

namespace QuiLaCarne.Ui;

public partial class PersonnelManagement : Page
{
    private readonly PersonnelManagementViewModel _viewModel;

    public PersonnelManagement(PersonnelManagementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        await _viewModel.RefreshAsync();
    }
}
