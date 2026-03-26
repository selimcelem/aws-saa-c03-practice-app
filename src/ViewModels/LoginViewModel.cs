using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AwsSaaC03Practice.Services;

namespace AwsSaaC03Practice.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _errorMessage = "";

    public LoginViewModel(AuthService auth) => _auth = auth;

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SignInAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var ok = await _auth.SignInWithGoogleAsync();
            if (ok)
                await Shell.Current.GoToAsync("//dashboard");
            else
                ErrorMessage = "Sign-in cancelled or failed. Please try again.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
