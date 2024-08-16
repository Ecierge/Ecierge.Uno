namespace Ecierge.Uno.App.Presentation;

using Microsoft.UI.Xaml.Controls;

using global::Uno.Extensions.Hosting;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public Shell()
    {
        this.InitializeComponent();
    }
    public ContentControl ContentControl => Splash;
}
