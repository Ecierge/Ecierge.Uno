# Ecierge.Uno.Navigation

Region based navigation for Uno Platform applications, modelled on Uno.Extensions.Navigation. A route is a tree of segments, every region of the user interface (a content control, a navigation view, a selector bar, a dialog) has a navigator, and a navigation shows a route one segment at a time, each segment in the navigator of its own region.

## Routes written as text

A route can be written as text, and the same text is what the address bar of a browser shows and what a link carries.

|Text|Meaning|
|:--|:--|
|`Menu`|The segment `Menu` in the region of the navigator that reads the text|
|`Building/3f2a/Details/Floors`|Segment names, and the value of a data segment (the id of the building) after the segment that owns it|
|`Building/3f2a/!AddAsset`|A dialog, written with the dialog qualifier|
|`/Login`|Starts in the root region|
|`./Info`|Starts in the region inside the content of the navigator|
|`../Menu`|Starts in the region that contains the navigator, one level per `../`|
|`-`|Goes back in the history, one entry per `-`|
|`-/Login`|Clears the history, then navigates, so that Back cannot return to what was before|

`Route.ToPath()` writes a route as this text and `NavigatorExtensions.TryParseRoute` reads it, so a route survives a round trip through an address or a link. A segment that has a default nested segment is completed with it.

## Data segments and the stale data problem

A data segment stands for an entity that the pages below it need, such as a building. A route holds the `NavigationEntity<T>` handle of the entity, not a bare `Task<T>`. All pages below the segment get the same handle, so what one page learns is seen by the others.

- The handle does not load until somebody asks, so reading a route costs no request.
- `GetAsync()` (and a constructor parameter of type `Task<T>`) returns the current attempt, and starts a new one when the last attempt failed or was canceled. A server that was down and came back is therefore not remembered as a failure.
- `ReloadAsync()` loads the entity again and raises `Changed`, so pages that show the entity can read it again.
- A constructor parameter of type `T` is resolved only while the entity is loaded. A parameter of type `NavigationEntity<T>` or `INavigationEntity` gets the handle.

The pages below a data segment are created after the entity has loaded. The page that owns the segment is created at once, so it can show progress, and `INavigationStatus.EntityLoading` tells which load is awaited. If the load fails, the navigation ends with a `NavigationFailedResponse` that carries the exception in `Error`, the requested route and a `Retry` function. A retry shows only what is not shown yet, and the entity loads again.

`INavigationStatus.NavigationFailed` is raised for every failure after `NavigationFinished`. A refusal by the navigation rules is a `NavigationDeniedResponse`, so that the application can send the user to sign in instead of offering a retry.

## History

Every window has one `NavigationJournal`, a list of the whole routes from the root to the page, and a position in it. A navigation of a route adds one entry, however many regions took part in it.

- `HistoryMode.Push` adds an entry, `Replace` takes the place of the current one (for a redirect) and `Skip` records nothing. A route that is equal to the current entry does not add an entry.
- `GoBackAsync`, `GoForwardAsync` and `GoAsync` show the page of another entry. The shell and the layout that are already right stay, and only the pages that differ change. The position moves after the page was shown.
- `NavigationMode` (`New`, `Back`, `Forward`, `Refresh`) tells a page why it is shown.
- The history keeps `NavigationOptions.JournalCapacity` entries.
- An open dialog is an entry of the history by default, so Back closes it and closing it with its own buttons moves the history back. `NavigationOptions.DialogsInHistory = false` keeps the page below the dialog in the history instead.

## Options

|Option|Default|Meaning|
|:--|:--|:--|
|`DialogsInHistory`|`true`|An open dialog is an entry of the history|
|`JournalCapacity`|`100`|The number of entries the history keeps|

The options are bound from the `NavigationOptions` section of the configuration, and the callback of `AddNavigation` adjusts them after that.

## Tests

`Ecierge.Uno.Navigation.Tests` runs on plain `net10.0` with MSTest on Microsoft.Testing.Platform. It needs no user interface: a fake navigator stands for the control of a region and an immediate dispatcher stands for the thread of the user interface.

```powershell
dotnet build Ecierge.Uno.Navigation.Tests
./Ecierge.Uno.Navigation.Tests/bin/Debug/net10.0/Ecierge.Uno.Navigation.Tests.exe
```

## Known limits

- A navigator that shows a page skips a navigation to the page that is already shown. A page that was created from an older load refreshes through its own refresh action, or takes `NavigationEntity<T>` and listens to `Changed`.
- Navigation guards (a confirmation before the user leaves a page with unsaved changes) are not part of the library yet.
