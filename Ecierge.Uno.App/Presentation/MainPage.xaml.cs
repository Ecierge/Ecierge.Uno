namespace Ecierge.Uno.App.Presentation;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

        BreadcrumbBar2.ItemsSource = new ObservableCollection<Folder> { home };
        BreadcrumbBar3.ItemsSource = new ObservableCollection<Folder> { home, folder1, folder2, folder3, folder4, folder5 };
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
