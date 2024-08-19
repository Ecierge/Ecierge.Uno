namespace Ecierge.Uno.App.Presentation;

using System.Threading.Tasks;

public partial class SecondViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;
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
