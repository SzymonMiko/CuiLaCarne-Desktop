using System.Windows.Controls;

namespace QuiLaCarne.Ui;

public partial class MenuRoomEditor : Page
{
    public MenuRoomEditor(MenuRoomEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
