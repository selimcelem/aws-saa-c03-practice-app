using CommunityToolkit.Mvvm.ComponentModel;

namespace AwsSaaC03Practice.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "";

    public bool IsNotBusy => !IsBusy;
}
