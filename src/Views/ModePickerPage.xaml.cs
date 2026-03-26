using AwsSaaC03Practice.ViewModels;

namespace AwsSaaC03Practice.Views;

public partial class ModePickerPage : ContentPage
{
    public ModePickerPage(ModePickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
