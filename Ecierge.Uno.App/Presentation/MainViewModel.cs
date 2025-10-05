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

#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT
    [ObservableProperty]
    private string? name;

    [ObservableProperty]
    private List<(string, string)> itemsSource;

    [ObservableProperty]
    private CollectionViewSource gropedItemsSource;

    [ObservableProperty]
    private CollectionViewSource gropedItemsSource2;
#pragma warning restore MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT

    public class Person
    {
        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }
        public string Name { get; }
        public int Age { get; }
    }

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

        var groups = new List<(string, string)>();
        for(int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                groups.Add(($"Group {i}", $"Item {j}"));
            }
        }

        itemsSource = groups;

        gropedItemsSource = new CollectionViewSource()
        {
            IsSourceGrouped = true,
            Source = groups.GroupBy(x => x.Item1)
                           .ToList()
        };
        gropedItemsSource2 = new CollectionViewSource()
        {
            IsSourceGrouped = true,
            Source = new List<Person>()
            {
                new Person("Alice", 30),
                new Person("Bob", 25),
                new Person("Charlie", 35),
                new Person("David", 28),
                new Person("Eve", 22),
                new Person("Frank", 40),
                new Person("Grace", 27),
                new Person("Heidi", 32),
                new Person("Ivan", 29),
                new Person("Judy", 24)
            }.GroupBy(p => p.Age < 30 ? "Under 30" : "30 and Over")
             .ToList()
        };
    }
    public string? Title { get; }

    public ICommand GoToSecond { get; }

    private async Task GoToSecondView()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        await navigator.NavigateLocalSegmentAsync(this, "Second", new Entity(Name));
    }

}
