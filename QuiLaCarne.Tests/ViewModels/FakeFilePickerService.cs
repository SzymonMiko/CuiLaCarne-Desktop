using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Tests.ViewModels;

internal sealed class FakeFilePickerService : IFilePickerService
{
    public string? NextImagePath { get; set; }

    public string? PickImageFile(string title)
    {
        return NextImagePath;
    }
}
