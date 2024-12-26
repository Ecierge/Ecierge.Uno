namespace Ecierge.Uno.App.Presentation;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        var folder5 = new Folder("Folder5");
        var folder4 = new Folder("Folder4", new ObservableCollection<Folder> { folder5 });
        var folder3 = new Folder("Folder3", new ObservableCollection<Folder> { folder4, folder5 });
        var folder2 = new Folder("Folder2", new ObservableCollection<Folder> { folder3, folder4, folder5 });
        var folder1 = new Folder("Folder1", new ObservableCollection<Folder> { folder2, folder3, folder4, folder5 });
        var home = new Folder("Home", new ObservableCollection<Folder> { folder1, folder2, folder3, folder4, folder5 });

        BreadcrumbBar.ItemsSource = new ObservableCollection<Folder> { home };
        LocationBreadcrumbBar.ItemsSource = new ObservableCollection<Folder> { home, folder1, folder2, folder3, folder4, folder5 };

        this.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnGlobalPointerPressed), true);
    }

    private void OnGlobalPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var originalSource = e.OriginalSource as FrameworkElement;
        System.Diagnostics.Debug.WriteLine($"Pointer pressed on: {originalSource?.Name ?? "Unnamed"} ({originalSource?.GetType().Name})");
    }
}

public class Folder
{
    public string Name { get; set; }
    public ObservableCollection<Folder> SubFolders { get; set; }

    public Folder(string name, ObservableCollection<Folder> subFolders = null)
    {
        Name = name;
        SubFolders = subFolders ?? new ObservableCollection<Folder>();
    }
}
