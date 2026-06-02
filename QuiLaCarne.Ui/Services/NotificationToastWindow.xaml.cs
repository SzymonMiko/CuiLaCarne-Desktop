using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QuiLaCarne.Ui.Services;

public partial class NotificationToastWindow : Window
{
    private readonly DispatcherTimer _closeTimer;
    private bool _isClosing;

    public NotificationToastWindow(string title, string message)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;
        TimeText.Text = DateTime.Now.ToString("HH:mm");

        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _closeTimer.Tick += (_, _) => CloseWithAnimation();
    }

    public void ShowAt(double left, double top)
    {
        Top = top;
        Left = SystemParameters.WorkArea.Right;
        Opacity = 0;
        Show();

        BeginAnimation(LeftProperty, new DoubleAnimation(left, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));
        _closeTimer.Start();
    }

    public void MoveTo(double left, double top)
    {
        BeginAnimation(LeftProperty, new DoubleAnimation(left, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        BeginAnimation(TopProperty, new DoubleAnimation(top, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    public void CloseWithAnimation()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        _closeTimer.Stop();

        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(160));
        animation.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, animation);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseWithAnimation();
    }
}
