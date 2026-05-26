using System.Windows.Controls;

namespace QuiLaCarne.Ui;

public partial class PersonnelManagement : Page
{
    public PersonnelManagement(PersonnelManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
