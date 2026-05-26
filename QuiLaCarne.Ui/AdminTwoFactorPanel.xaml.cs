using System.Windows.Controls;

namespace QuiLaCarne.Ui;

public partial class AdminTwoFactorPanel : Page
{
    public AdminTwoFactorPanel(AdminTwoFactorPanelViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
