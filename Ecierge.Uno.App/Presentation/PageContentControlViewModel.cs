namespace Ecierge.Uno.App.Presentation;

using System.Threading.Tasks;

public partial class PageContentControlViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;
    public string Title { get; } = "PageContentControl";

    public PageContentControlViewModel(Task<Entity> name)
    {
        LoadData(name);
    }

    private async void LoadData(Task<Entity> entityTask)
    {
        var entity = await entityTask;
        Name = entity.Name;
    }
}
