namespace Ecierge.Uno.Controls;

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;

public partial class GroupedComboBoxItem : ListViewItem
{
    public GroupedComboBoxItem()
    {
        Initialize();

        DefaultStyleKey = typeof(GroupedComboBoxItem);
        IsGeneratedContainer = true;
    }

    partial void Initialize();

    protected bool IsGeneratedContainer { get; }

#if HAS_UNO
    public GridViewItemTemplateSettings TemplateSettings { get; } = new();
#endif
}

