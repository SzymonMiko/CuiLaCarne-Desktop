namespace QuiLaCarne.Services.IServices;

public interface IUiDispatcherService
{
    Task InvokeAsync(Func<Task> action);
}
