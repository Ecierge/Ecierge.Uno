namespace Ecierge.Uno.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GroupedComboBoxGroupStyleSelector : GroupStyleSelector
{
    protected override GroupStyle SelectGroupStyleCore(object group, uint level)
    {
        var appResources = Application.Current.Resources;
        var dataTemplate = appResources["GroupedComboBoxGroupHeaderTemplate"] as DataTemplate;

        return new GroupStyle
        {
            HeaderTemplate = dataTemplate
        };
    }
}
