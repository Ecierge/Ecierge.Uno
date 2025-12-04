namespace Ecierge.Uno.App.Presentation;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Ecierge.Uno.Navigation;
using Microsoft.UI.Xaml.Data;

public partial class MainViewModel : ObservableObject
{
    private Navigator navigator;

    [ObservableProperty]
    private string? name;

    [ObservableProperty]
    private CollectionViewSource? gropedItemsSource;

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
        GoToPageContentControl = new AsyncRelayCommand(GoToPageContentControlView);

        var groups = new List<(string, string)>();
        for(int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                groups.Add(($"Group {i}", $"Item {j}"));
            }
        }

        gropedItemsSource = new CollectionViewSource()
        {
            IsSourceGrouped = true,
            Source = groups.GroupBy(x => x.Item1)
                           .ToList()
        };
    }
    public string? Title { get; }

    public ICommand GoToSecond { get; }
    public ICommand GoToPageContentControl { get; }

    private async Task GoToSecondView()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        await navigator.NavigateLocalSegmentAsync(this, "Second", new Entity(Name));
    }
    private async Task GoToPageContentControlView()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        await navigator.NavigateLocalSegmentAsync(this, "PageContentControl", new Entity(Name));
    }

}
