namespace Ecierge.Uno.Behaviors;

using Microsoft.Xaml.Interactivity;



public sealed class NumberBoxValidationBehavior : Behavior<NumberBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ValueChanged += OnValueChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ValueChanged -= OnValueChanged;
        base.OnDetaching();
    }

    private void OnValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        sender.GetBindingExpression(NumberBox.ValueProperty)
              ?.UpdateSource();
    }
}
