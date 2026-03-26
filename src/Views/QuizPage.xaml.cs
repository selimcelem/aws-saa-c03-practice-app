using AwsSaaC03Practice.ViewModels;

namespace AwsSaaC03Practice.Views;

public partial class QuizPage : ContentPage
{
    private readonly QuizViewModel _vm;

    public QuizPage(QuizViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitialiseAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Dispose();
    }
}
