using System.Windows.Controls;

namespace QuiLaCarne.Ui;

public partial class SecurityDashboard : Page
{
    public SecurityDashboard(SecurityDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
