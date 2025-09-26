using Microsoft.Extensions.DependencyInjection;
using MultipleAIApp.ViewModels;

namespace MultipleAIApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        DataContext = App.Current.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext;
}
