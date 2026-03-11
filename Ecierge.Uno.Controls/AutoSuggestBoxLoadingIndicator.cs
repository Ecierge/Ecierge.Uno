namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml;
using CommunityToolkit.WinUI;

public static class AutoSuggestBoxLoadingIndicator
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached(
        "IsActive",
        typeof(bool),
        typeof(AutoSuggestBoxLoadingIndicator),
        new PropertyMetadata(false, OnIsActiveChanged));

    public static void SetIsActive(DependencyObject obj, bool value)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        obj.SetValue(IsActiveProperty, value);
    }

    public static bool GetIsActive(DependencyObject obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        return (bool)obj.GetValue(IsActiveProperty);
    }

    private static readonly DependencyProperty CachedRingProperty =
    DependencyProperty.RegisterAttached(
        "_ProgressRing_",
        typeof(ProgressRing),
        typeof(AutoSuggestBoxLoadingIndicator),
        new PropertyMetadata(null));

    private static void SetCachedRing(DependencyObject obj, ProgressRing value)
    {
        obj.SetValue(CachedRingProperty, value);
    }

    private static ProgressRing GetCachedRing(DependencyObject obj)
    {
        return (ProgressRing)obj.GetValue(CachedRingProperty);
    }

    private static void OnIsActiveChanged(
    DependencyObject d,
    DependencyPropertyChangedEventArgs e)
    {
        if (d is AutoSuggestBox box)
        {
            var ring = GetCachedRing(box);

            if (ring is null)
            {
                ring = box.FindDescendant<ProgressRing>();

                if (ring is not null)
                    SetCachedRing(box, ring);
            }

            if (ring is not null)
                SetIsActive(ring, (bool)e.NewValue);
        }
        else if (d is ProgressRing ring)
        {
            ring.IsActive = (bool)e.NewValue;
        }
    }
}
