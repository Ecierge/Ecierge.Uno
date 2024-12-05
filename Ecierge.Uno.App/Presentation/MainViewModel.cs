namespace Ecierge.Uno.App.Presentation;

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Ecierge.Uno.Controls.LocationBreadcrumb;
using Ecierge.Uno.Navigation;

public partial class MainViewModel : ObservableObject
{
    private Navigator navigator;

    [ObservableProperty]
    private string? name;


    public MainViewModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        Navigator navigator)
    {
        this.navigator = navigator;
        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
        GoToSecond = new AsyncRelayCommand(GoToSecondView);

    }
    public string? Title { get; }

    public ICommand GoToSecond { get; }

    private async Task GoToSecondView()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        await navigator.NavigateLocalSegmentAsync(this, "Second", new Entity(Name));
    }

}
