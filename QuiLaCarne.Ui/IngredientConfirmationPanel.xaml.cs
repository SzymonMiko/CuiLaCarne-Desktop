using System.Windows.Controls;

namespace QuiLaCarne.Ui;

public partial class IngredientConfirmationPanel : Page
{
    public IngredientConfirmationPanel(IngredientConfirmationPanelViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadIngredientsAsync();
    }
}
