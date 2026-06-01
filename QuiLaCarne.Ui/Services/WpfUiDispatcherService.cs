using System.Windows;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui.Services;

public sealed class WpfUiDispatcherService : IUiDispatcherService
{
    public async Task InvokeAsync(Func<Task> action)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher == null || dispatcher.CheckAccess())
        {
            await action();
            return;
        }

        var task = await dispatcher.InvokeAsync(action);
        await task;
    }
}
