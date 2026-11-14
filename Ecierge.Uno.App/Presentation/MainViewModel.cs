namespace Ecierge.Uno.App.Presentation;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;
using Microsoft.UI.Xaml.Data;

public partial class MainViewModel : ObservableObject
{
    private Navigator navigator;

#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT
    // The buttons that open the second page need a name: it is the id of the entity in the route
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToSecond))]
    [NotifyPropertyChangedFor(nameof(HasName))]
    private string? name;

    [ObservableProperty]
    private CollectionViewSource? gropedItemsSource;
#pragma warning restore MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT

    public MainViewModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        Navigator navigator)
    {
        this.navigator = navigator;
        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
        GoToSecond = new AsyncRelayCommand(GoToSecondView, () => HasName);

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

    public bool HasName => !string.IsNullOrWhiteSpace(Name);

    public IAsyncRelayCommand GoToSecond { get; }

    private async Task GoToSecondView()
    {
        await navigator.NavigateLocalSegmentAsync(this, "Second", Entity.FromName(Name!));
    }

}
