namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml;

public static class LoadingAutoSuggestBoxHelper
{
    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.RegisterAttached(
        "IsLoading",
        typeof(bool),
        typeof(LoadingAutoSuggestBoxHelper),
        new PropertyMetadata(false, OnIsLoadingChanged));

    public static void SetIsLoading(DependencyObject obj, bool value)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        obj.SetValue(IsLoadingProperty, value);
    }

    public static bool GetIsLoading(DependencyObject obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        return (bool)obj.GetValue(IsLoadingProperty);
    }

    private static readonly DependencyProperty CachedRingProperty =
    DependencyProperty.RegisterAttached(
        "CachedRing",
        typeof(ProgressRing),
        typeof(LoadingAutoSuggestBoxHelper),
        new PropertyMetadata(null));

    private static void SetCachedRing(DependencyObject obj, ProgressRing value)
    {
        obj.SetValue(CachedRingProperty, value);
    }

    private static ProgressRing GetCachedRing(DependencyObject obj)
    {
        return (ProgressRing)obj.GetValue(CachedRingProperty);
    }

    private static void OnIsLoadingChanged(
    DependencyObject d,
    DependencyPropertyChangedEventArgs e)
    {
        if (d is AutoSuggestBox box)
        {
            var ring = GetCachedRing(box);

            if (ring == null)
            {
                ring = FindDescendant<ProgressRing>(box);

                if (ring != null)
                    SetCachedRing(box, ring);
            }

            if (ring != null)
                SetIsLoading(ring, (bool)e.NewValue);
        }
        else if (d is ProgressRing ring)
        {
            ring.IsActive = (bool)e.NewValue;
        }
    }
    private static T FindDescendant<T>(DependencyObject parent)
    where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T result)
                return result;

            var descendant = FindDescendant<T>(child);
            if (descendant != null)
                return descendant;
        }

        return default;
    }
}
