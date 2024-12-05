#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Ecierge.Uno.Controls.LocationBreadcrumb;
internal partial class LocationBreadcrumbIterable : IEnumerable<object?>
{
    public LocationBreadcrumbIterable()
    {
    }

    public LocationBreadcrumbIterable(object? itemsSource)
    {
        ItemsSource = itemsSource;
    }

    public object? ItemsSource { get; }

    public IEnumerator<object?> GetEnumerator() => new LocationBreadcrumbIterator(ItemsSource);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
