namespace Ecierge.Uno.Navigation;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

[DebuggerDisplay("{Qualifier}{Path}")]
public record Route(string Qualifier = Qualifiers.None, string? Path = null, IDictionary<string, object>? Data = null, bool Refresh = false)
{
    public static readonly DependencyProperty SegmentProperty = DependencyProperty.RegisterAttached("Segment", typeof(string), typeof(Route), new PropertyMetadata(null, new PropertyChangedCallback(OnSegmentChanged)));

    public static string? GetSegment([NotNull] Control control) => (string?)control.GetValue(SegmentProperty);

    public static void SetSegment([NotNull] Control control, string value) => control.SetValue(SegmentProperty, value);

    private static void OnSegmentChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        OnSegmentChanged(o, (string)e.OldValue, (string)e.NewValue);
    }

    private static void OnSegmentChanged(DependencyObject o, string oldValue, string newValue)
    {
        // TODO: Add your property changed side-effects. Descendants can override as well.
    }

}
