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
- A data map loads an entity from its value with `LoadEntityAsync(string)`. An entity that lives inside a parent, such as an asset inside a property, is not found by its value alone: the map overrides `LoadEntityAsync(string, INavigationData)`, which gets the data of the route that the handle was made in and reads the value of the parent from the handle of the parent without loading it. The handle keeps that data, so a reload finds the entity too.

The pages below a data segment are created after the entity has loaded. The page that owns the segment is created at once, so it can show progress, and `INavigationStatus.EntityLoading` tells which load is awaited. If the load fails, the navigation ends with a `NavigationFailedResponse` that carries the exception in `Error`, the requested route and a `Retry` function. A retry shows only what is not shown yet, and the entity loads again.

`INavigationStatus.NavigationFailed` is raised for every failure after `NavigationFinished`. A refusal by the navigation rules is a `NavigationDeniedResponse`, so that the application can send the user to sign in instead of offering a retry.

## History

Every window has one `NavigationJournal`, a list of the whole routes from the root to the page, and a position in it. A navigation of a route adds one entry, however many regions took part in it.

- `HistoryMode.Push` adds an entry, `Replace` takes the place of the current one (for a redirect) and `Skip` records nothing. A route that is equal to the current entry does not add an entry.
- `GoBackAsync`, `GoForwardAsync` and `GoAsync` show the page of another entry. The shell and the layout that are already right stay, and only the pages that differ change. The position moves after the page was shown.
- `NavigationMode` (`New`, `Back`, `Forward`, `Refresh`) tells a page why it is shown.
- The history keeps `NavigationOptions.JournalCapacity` entries.
- An open dialog is an entry of the history by default, so Back closes it and closing it with its own buttons moves the history back. `NavigationOptions.DialogsInHistory = false` keeps the page below the dialog in the history instead.

In XAML, `NavigateBackAction` and `NavigateForwardAction` go back and forward, and the attached properties `Navigation.CanGoBack` and `Navigation.CanGoForward` of the content of the window can enable a button. A `NavigationView` goes back from its own back button, whichever of `NavigationViewNavigator` and `NavigationViewContentNavigator` serves it; the button is enabled while the journal has an entry to go back to, and the view shows it when its `IsBackButtonVisible` says so.

## Deep links

`DeepLinkMapper.ToRoutePath` turns a link into the text of a route. A link of the custom scheme (`NavigationOptions.DeepLinkScheme`, for example `ecierge-console://Building/3f2a/Assets`) and a link of the web (`https://host/Building/3f2a/Assets`, under `NavigationOptions.BasePath`) name pages. Any other link does not. A link cannot carry the commands of the route text (`-`, `-/`, `/`, `../`, `./`), because it comes from outside.

- `IDeepLinkSource` gives the link that started the application (`LaunchUri`) and the links that arrive while it runs (`Received`). Windows reads the protocol activation, the desktop reads the command line and the browser reads its address. Other platforms have no source.
- `WindowExtensions.NavigateAsync` puts the page of the launch link into `IPendingNavigation`. When the application has no initial navigation of its own, it opens that page, replacing the first entry of the history. An application with its own initial navigation takes the route with `IPendingNavigation.Consume()`, for example after the user has signed in.
- `Navigator.NavigateToLinkAsync` opens the page of a link at run time. The rules still decide, like for any other navigation.

## Browser history (WebAssembly)

On WebAssembly the history of the browser follows the journal in both directions, and the address bar shows the route under the base path.

- A page that is added to the journal is added to the history of the browser. The first page takes the place of the entry the page was loaded with.
- Back and Forward of the browser show the page of the entry through the navigators, and the journal follows once the page is shown. A page that cannot be shown returns the browser to the current entry.
- An entry the application did not make, such as the entry of an earlier run after a reload, is opened from its address and takes the place of the current entry.
- The state of an entry is the object `{ ecierge: token }`, so it does not clash with the dummy entry that Uno keeps for the back button of the system. The library never makes that button visible on WebAssembly.
- The host of the page has to serve the application for every path. Console does this with the rewrite rule of its static web app.

## Back button of the system

On Android and iOS the back button of the system goes back in the journal while there is an entry to go back to, and leaves the application otherwise. `NavigationOptions.UseNativeBackButton` turns it on or off explicitly. It is never used on WebAssembly.

## Options

|Option|Default|Meaning|
|:--|:--|:--|
|`DialogsInHistory`|`true`|An open dialog is an entry of the history|
|`JournalCapacity`|`100`|The number of entries the history keeps|
|`AddressBarUpdateEnabled`|`true`|The browser history follows the journal (WebAssembly)|
|`UseNativeBackButton`|`true` on Android and iOS|The back button of the system goes back in the journal|
|`DeepLinkScheme`|none|The scheme of the links that open a page, such as `ecierge-console`|
|`BasePath`|`/`|The path under which the application is hosted on the web|

The options are bound from the `NavigationOptions` section of the configuration, and the callback of `AddNavigation` adjusts them after that.

## Tests

`Ecierge.Uno.Navigation.Tests` runs on plain `net10.0` with MSTest on Microsoft.Testing.Platform. It needs no user interface: a fake navigator stands for the control of a region and an immediate dispatcher stands for the thread of the user interface.

```powershell
dotnet build Ecierge.Uno.Navigation.Tests
./Ecierge.Uno.Navigation.Tests/bin/Debug/net10.0/Ecierge.Uno.Navigation.Tests.exe
```

## Known limits

- A navigator that shows a page skips a navigation to the page that is already shown. A page that was created from an older load refreshes through its own refresh action, or takes `NavigationEntity<T>` and listens to `Changed`.
- A link that reaches an application that is already running is not handed to it on Windows and on the desktop, where the system starts another process with the link.
- A dialog is a step of the route, but the entries of the browser before the first page of a session cannot be removed, so Back of the browser can leave the application.
- Navigation guards (a confirmation before the user leaves a page with unsaved changes) are not part of the library yet.
