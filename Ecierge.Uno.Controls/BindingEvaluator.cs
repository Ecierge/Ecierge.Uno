using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace Ecierge.Uno.Controls;
public class BindingEvaluator
{
    public object? Evaluate(object source, string path)
    {
        var tb = new TextBlock();

        var binding = new Binding
        {
            Path = new PropertyPath(path),
            Source = source,
            Mode = BindingMode.OneTime
        };

        tb.SetBinding(TextBlock.TextProperty, binding);

        var result = tb.Text;

        tb.ClearValue(TextBlock.TextProperty);

        return result;
    }
}
