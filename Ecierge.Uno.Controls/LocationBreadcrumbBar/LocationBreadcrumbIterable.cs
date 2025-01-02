#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Ecierge.Uno.Controls.LocationBreadcrumbBar;
internal partial class LocationBreadcrumbIterable : IEnumerable<object?>, INotifyCollectionChanged
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
    object? itemsSource;

    public object? ItemsSource
    {
        get => itemsSource;
        set
        {
            void OnCollectionChanged(object? s, NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

            if (value is INotifyCollectionChanged oldNotifyCollectionChanged)
            {
                oldNotifyCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }

            itemsSource = value;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            if (itemsSource is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged += OnCollectionChanged;
            }
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public IEnumerator<object?> GetEnumerator() => new LocationBreadcrumbIterator(ItemsSource);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
