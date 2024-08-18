namespace Ecierge.Uno.App.Presentation;

public class SecondViewModel(Entity name)
{
    public string Name { get; } = name.Name;
    public string Title { get; } = "Second";
}
