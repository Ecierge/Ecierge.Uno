#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ecierge.Uno.Controls.LocationBreadcrumb;
internal partial class LocationBreadcrumbIterable : IEnumerable<object?>
{
    public LocationBreadcrumbIterable()
    {
    }

#if !HAS_UNO
    public LocationBreadcrumbIterable(object? itemsSource)
    {
    
        if (itemsSource is IEnumerable<object?> sourceEnumerable)
        {
    
            var list = new ObservableCollection<object?>(sourceEnumerable);
    
    
            list.Insert(0, null);
    
    
            ItemsSource = list;
        }
        else
        {
            ItemsSource = new ObservableCollection<object?> { null };
        }
    }
#else
    public LocationBreadcrumbIterable(object? itemsSource)
    {
        ItemsSource = itemsSource;
    }
#endif

    public object? ItemsSource { get; }

    public IEnumerator<object?> GetEnumerator() => new LocationBreadcrumbIterator(ItemsSource);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
