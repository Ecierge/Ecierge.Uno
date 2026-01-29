namespace Ecierge.Uno.Navigation;

using System;

public sealed class NavigatorChangedEventArgs(Navigator? oldNavigator, Navigator? newNavigator) : EventArgs
{
    public Navigator? OldNavigator { get; } = oldNavigator;
    public Navigator? NewNavigator { get; } = newNavigator;
}
