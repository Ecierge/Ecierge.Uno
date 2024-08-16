namespace Ecierge.Uno.App.Presentation;

using Ecierge.Uno.Navigation;

public class ShellViewModel
{
    private readonly Navigator _navigator;

    public ShellViewModel(
        Navigator navigator)
    {
        _navigator = navigator;
        // Add code here to initialize or attach event handlers to singleton services
    }
}
