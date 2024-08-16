namespace Ecierge.Uno.App.Presentation;

using System.Threading.Tasks;
using System.Windows.Input;

using Ecierge.Uno.Navigation;

public partial class MainViewModel : ObservableObject
{
    private Navigator _navigator;

    [ObservableProperty]
    private string? name;

    public MainViewModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        Navigator navigator)
    {
        _navigator = navigator;
        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
        GoToSecond = new AsyncRelayCommand(GoToSecondView);
    }
    public string? Title { get; }

    public ICommand GoToSecond { get; }

    private async Task GoToSecondView()
    {
        //await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new Entity(Name!));
    }

}
