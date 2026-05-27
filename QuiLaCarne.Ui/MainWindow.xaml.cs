using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Navigate(Page page, string title)
    {
        Title = title;
        ShellTitle.Text = title;
        ShellNavigationBar.Visibility = page is Menu ? Visibility.Collapsed : Visibility.Visible;
        MainFrame.Navigate(page);
    }

    private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
    {
        App.AppHost.Services.GetRequiredService<INavigationService>().ShowMenu();
    }
}
