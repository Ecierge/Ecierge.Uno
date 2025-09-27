namespace Ecierge.Uno.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GroupedComboBoxGroupStyleSelector : GroupStyleSelector
{
    public GroupStyle DefaultStyle { get; set; }
    protected override GroupStyle SelectGroupStyleCore(object group, uint level)
        => DefaultStyle;
}
