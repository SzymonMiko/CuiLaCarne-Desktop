using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using QuiLaCarne.Ui.Services;
using Xunit;

namespace QuiLaCarne.Tests.Services;

public sealed class WpfLocalizationServiceTests
{
    [Fact]
    public void ToggleLanguage_UpdatesLanguageSwitchTextAndTranslatesKnownText()
    {
        var service = new WpfLocalizationService();

        service.ToggleLanguage();

        Assert.Equal("en", service.CurrentLanguage);
        Assert.Equal("Polski", service.LanguageSwitchText);
        Assert.Equal("Login", service.Translate("Zaloguj"));
    }

    [Fact]
    public void TranslateTextBlock_TranslatesRunsWithoutMutatingEnumeration()
    {
        RunOnStaThread(() =>
        {
            var service = new WpfLocalizationService();
            service.ToggleLanguage();
            var textBlock = new TextBlock();
            textBlock.Inlines.Add(new Run("Zaloguj"));
            textBlock.Inlines.Add(new Run("Hasło"));

            var method = typeof(WpfLocalizationService).GetMethod(
                "TranslateTextBlock",
                BindingFlags.Instance | BindingFlags.NonPublic);

            method!.Invoke(service, [textBlock]);

            Assert.Equal(["Login", "Password"], textBlock.Inlines.OfType<Run>().Select(x => x.Text));
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }
}
