namespace Ecierge.Uno.Controls;

partial class RecyclePool
{
    public static DependencyProperty PoolInstanceProperty { get; } = DependencyProperty.RegisterAttached(
        "PoolInstance", typeof(RecyclePool), typeof(RecyclePool), new PropertyMetadata(default(RecyclePool)));

    public static RecyclePool GetPoolInstance(DataTemplate dataTemplate)
        => (RecyclePool)dataTemplate.GetValue(PoolInstanceProperty);

    public static void SetPoolInstance(DataTemplate dataTemplate, RecyclePool value)
        => dataTemplate.SetValue(PoolInstanceProperty, value);
}
