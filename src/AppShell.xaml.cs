using AwsSaaC03Practice.Views;

namespace AwsSaaC03Practice;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Register routes for pages navigated to with parameters
        Routing.RegisterRoute("quiz",    typeof(QuizPage));
        Routing.RegisterRoute("results", typeof(ResultsPage));
    }
}
