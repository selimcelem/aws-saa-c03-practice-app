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
        try
        {
            // Call the method directly — RelayCommand.ExecuteAsync swallows exceptions
            await _vm.LoadResultsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ResultsPage] UNHANDLED in OnAppearing: {ex}");
            App.LogCrash("ResultsPage.OnAppearing", ex);
            await DisplayAlert("Error", $"Failed to load results: {ex.Message}", "OK");
            await Shell.Current.GoToAsync("//dashboard");
        }
    }
}
