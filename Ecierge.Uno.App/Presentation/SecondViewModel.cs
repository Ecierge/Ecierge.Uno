namespace Ecierge.Uno.App.Presentation;

using System.Threading.Tasks;

public partial class SecondViewModel : ObservableObject
{
#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT
    [ObservableProperty]
    private string? name;
#pragma warning restore MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT
    public string Title { get; } = "Second";

    public SecondViewModel(Task<Entity> name)
    {
        LoadData(name);
    }

    private async void LoadData(Task<Entity> entityTask)
    {
        var entity = await entityTask;
        Name = entity.Name;
    }
}
