namespace Ecierge.Uno.Controls;

partial class RecyclePool
{
    internal static DependencyProperty ReuseKeyProperty { get; } = DependencyProperty.RegisterAttached(
        "ReuseKey",
        typeof(string),
        typeof(RecyclePool),
        new PropertyMetadata(defaultValue: "" /* defaultValue */, propertyChangedCallback: null /* propertyChangedCallback */));

    internal static DependencyProperty OriginTemplateProperty { get; } = DependencyProperty.RegisterAttached(
        "OriginTemplate",
        typeof(DataTemplate),
        typeof(RecyclePool),
        new PropertyMetadata(defaultValue: null, propertyChangedCallback: null));

    #region IRecyclePoolStatics

    internal static string GetReuseKey(UIElement element)
    {
        return (string)element.GetValue(ReuseKeyProperty);
    }

    internal static void SetReuseKey(UIElement element, string value)
    {
        element.SetValue(ReuseKeyProperty, value);
    }

    #endregion
}
