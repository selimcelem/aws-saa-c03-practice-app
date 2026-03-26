using AwsSaaC03Practice.ViewModels;

namespace AwsSaaC03Practice.Views;

public partial class ResultsPage : ContentPage
{
    private readonly ResultsViewModel _vm;

    public ResultsPage(ResultsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}
