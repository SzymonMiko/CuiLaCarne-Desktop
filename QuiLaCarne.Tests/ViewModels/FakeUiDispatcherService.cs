using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeUiDispatcherService : IUiDispatcherService
{
    public Task InvokeAsync(Func<Task> action)
    {
        return action();
    }
}
