namespace Ecierge.Uno.App.Presentation;

using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Ecierge.Uno.Navigation;

/// <summary>
/// Shows the entity of the route and loads it again when the user clicks the last breadcrumb.
/// </summary>
public partial class SecondViewModel : ObservableObject, IRefreshable
{
    private readonly NavigationEntity<Entity> entity;

#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT
    [ObservableProperty]
    private string? name;

    [ObservableProperty]
    private string? loadedAt;
#pragma warning restore MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT

    public string Title { get; } = "Second";

    /// <summary>
    /// Gets the command that loads the entity again; the last breadcrumb runs it.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Creates the view model of the page of an entity.
    /// </summary>
    /// <param name="name">The handle of the entity of the data segment <c>name</c>.</param>
    public SecondViewModel(NavigationEntity<Entity> name)
    {
        entity = name;
        RefreshCommand = new AsyncRelayCommand(() => entity.ReloadAsync());
        // A reload, by this page or by anybody else, replaces the load that the page shows
        entity.Changed += (_, _) => _ = ShowAsync();
        _ = ShowAsync();
    }

    private async Task ShowAsync()
    {
        LoadedAt = "Loading…";
        try
        {
            var loaded = await entity.GetAsync();
            Name = loaded.Name;
            LoadedAt = $"Loaded at {DateTime.Now:T}";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            LoadedAt = $"Could not load: {ex.Message}";
        }
    }
}
