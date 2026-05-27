using System.Windows.Controls;
using QuiLaCarne.ViewModels;

namespace QuiLaCarne.Ui;

public partial class PersonnelManagement : Page
{
    public PersonnelManagement(PersonnelManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
