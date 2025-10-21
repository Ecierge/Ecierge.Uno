namespace Ecierge.Uno.App.Presentation;

using System.Threading.Tasks;

public partial class ThirdViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;
    public string Title { get; } = "Third";

    public ThirdViewModel(Task<Entity> name)
    {
        LoadData(name);
    }

    private async void LoadData(Task<Entity> entityTask)
    {
        var entity = await entityTask;
        Name = entity.Name;
    }
}
