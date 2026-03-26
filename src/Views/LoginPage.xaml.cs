using AwsSaaC03Practice.Services;
using AwsSaaC03Practice.ViewModels;

namespace AwsSaaC03Practice.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly LoginViewModel _vm;

    public LoginPage(LoginViewModel vm, AuthService auth)
    {
        InitializeComponent();
        _vm = vm;
        _auth = auth;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Auto-login if a valid refresh token exists
        if (await _auth.TryAutoLoginAsync())
            await Shell.Current.GoToAsync("//dashboard");
    }
}
