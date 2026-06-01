using Microsoft.Win32;
using QuiLaCarne.Services.IServices;

namespace QuiLaCarne.Ui.Services;

public sealed class WpfFilePickerService : IFilePickerService
{
    public string? PickImageFile(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
