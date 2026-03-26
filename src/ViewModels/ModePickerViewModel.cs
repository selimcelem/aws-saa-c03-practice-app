using CommunityToolkit.Mvvm.Input;
using AwsSaaC03Practice.Models;

namespace AwsSaaC03Practice.ViewModels;

public partial class ModePickerViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task SelectModeAsync(string modeStr)
    {
        var mode = Enum.Parse<QuizMode>(modeStr);
        // Pass mode as navigation parameter
        await Shell.Current.GoToAsync($"quiz?mode={modeStr}");
    }

    [RelayCommand]
    private async Task GoBackAsync() =>
        await Shell.Current.GoToAsync("//dashboard");
}
