namespace Ecierge.Uno.Controls;

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls.Primitives;



public partial class GroupedComboBoxGridViewItem : GridViewItem
{
    public GroupedComboBoxGridViewItem()
    {
        Initialize();

        DefaultStyleKey = typeof(GroupedComboBoxGridViewItem);
        IsGeneratedContainer = true;
    }

    partial void Initialize();

    protected bool IsGeneratedContainer { get; }

#if __UNO__

    public GridViewItemTemplateSettings TemplateSettings { get; } = new();

#endif
}

