namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationJournalTests
{
    private static Routing.Route RouteOf(string name) => Routing.Route.Empty.Add(new NameSegment(name));

    private static List<JournalChangedEventArgs> Listen(NavigationJournal journal)
    {
        var changes = new List<JournalChangedEventArgs>();
        journal.Changed += (_, args) => changes.Add(args);
        return changes;
    }

    [TestMethod]
    public void A_new_journal_must_be_empty()
    {
        var journal = new NavigationJournal();

        Assert.IsEmpty(journal.Entries, "Expected no entries before the first visit.");
        Assert.AreEqual(-1, journal.CurrentIndex, "Expected no position in an empty journal.");
        Assert.IsNull(journal.Current, "Expected no current entry in an empty journal.");
        Assert.IsFalse(journal.CanGoBack, "Expected nothing to go back to.");
        Assert.IsFalse(journal.CanGoForward, "Expected nothing to go forward to.");
    }

    [TestMethod]
    public void Record_must_push_the_first_entry_and_make_it_current()
    {
        var journal = new NavigationJournal();
        var changes = Listen(journal);

        var entry = journal.Record(RouteOf("A"), HistoryMode.Push);

        Assert.IsNotNull(entry, "Expected the recorded entry.");
        Assert.AreEqual("A", entry.Path, "Expected the entry to hold the text of the route.");
        Assert.AreSame(entry, journal.Current, "Expected the new entry to be current.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the position of the first entry.");
        Assert.ContainsSingle(changes, "Expected one change.");
        Assert.AreEqual(JournalChange.Pushed, changes[0].Change, "Expected the change to be a push.");
        Assert.IsFalse(journal.CanGoBack, "Expected nothing before the first entry.");
    }

    [TestMethod]
    public void Record_must_push_entries_in_the_order_of_the_visits()
    {
        var journal = new NavigationJournal();

        journal.Record(RouteOf("A"), HistoryMode.Push);
        journal.Record(RouteOf("B"), HistoryMode.Push);
        journal.Record(RouteOf("C"), HistoryMode.Push);

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, journal.Entries.Select(e => e.Path).ToArray(), "Expected the oldest entry first.");
        Assert.AreEqual(2, journal.CurrentIndex, "Expected the last visit to be current.");
        Assert.IsTrue(journal.CanGoBack, "Expected entries before the current one.");
        Assert.IsFalse(journal.CanGoForward, "Expected no entries after the current one.");
    }

    [TestMethod]
    public void Record_must_keep_the_current_entry_when_the_path_is_equal_and_use_the_fresher_route()
    {
        var journal = new NavigationJournal();
        var first = journal.Record(RouteOf("A"), HistoryMode.Push);
        var changes = Listen(journal);
        var fresher = RouteOf("A");

        var again = journal.Record(fresher, HistoryMode.Push);

        Assert.IsNotNull(first, "Expected the first entry.");
        Assert.IsNotNull(again, "Expected the entry to be returned.");
        Assert.AreEqual(first.Id, again.Id, "Expected the same entry, not a new one.");
        Assert.HasCount(1, journal.Entries, "Expected the journal not to grow for the same location.");
        Assert.AreSame(fresher, journal.Current!.Route, "Expected the entry to hold the route of the last visit.");
        Assert.IsEmpty(changes, "Expected the browser history to need no change for the same location.");
    }

    [TestMethod]
    public void Record_must_treat_paths_that_differ_in_case_only_as_the_same_location()
    {
        var journal = new NavigationJournal();
        var data = new DataSegment("BuildingInfo", null, true);
        _ = new NameSegment("Building", null, data);
        journal.Record(Routing.Route.Empty.Add(data, "3F2A"), HistoryMode.Push);

        journal.Record(Routing.Route.Empty.Add(data, "3f2a"), HistoryMode.Push);

        Assert.HasCount(1, journal.Entries, "Expected an id in another case to be the same location.");
    }

    [TestMethod]
    public void Record_with_Replace_must_swap_the_current_entry()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("Login"), HistoryMode.Push);
        var changes = Listen(journal);

        var entry = journal.Record(RouteOf("Home"), HistoryMode.Replace);

        CollectionAssert.AreEqual(new[] { "Home" }, journal.Entries.Select(e => e.Path).ToArray(), "Expected the redirect to leave no trace of the page it replaced.");
        Assert.AreSame(entry, journal.Current, "Expected the new entry to be current.");
        Assert.AreEqual(JournalChange.Replaced, changes.Single().Change, "Expected the change to be a replacement.");
        Assert.IsFalse(journal.CanGoBack, "Expected Back not to return to the replaced page.");
    }

    [TestMethod]
    public void Record_with_Replace_must_push_into_an_empty_journal()
    {
        var journal = new NavigationJournal();
        var changes = Listen(journal);

        journal.Record(RouteOf("Home"), HistoryMode.Replace);

        Assert.HasCount(1, journal.Entries, "Expected the first entry.");
        Assert.AreEqual(JournalChange.Pushed, changes.Single().Change, "Expected a push, as there was nothing to replace.");
    }

    [TestMethod]
    public void Record_with_Skip_must_leave_the_journal_alone()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        var changes = Listen(journal);

        var entry = journal.Record(RouteOf("B"), HistoryMode.Skip);

        Assert.IsNull(entry, "Expected no entry for a navigation that is not kept.");
        Assert.HasCount(1, journal.Entries, "Expected the journal not to change.");
        Assert.IsEmpty(changes, "Expected no change to be announced.");
    }

    [TestMethod]
    public void Record_must_drop_the_entries_ahead_of_the_position()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        journal.Record(RouteOf("B"), HistoryMode.Push);
        journal.Record(RouteOf("C"), HistoryMode.Push);
        journal.MoveTo(0);

        journal.Record(RouteOf("D"), HistoryMode.Push);

        CollectionAssert.AreEqual(new[] { "A", "D" }, journal.Entries.Select(e => e.Path).ToArray(), "Expected a new visit to cut off the forward history.");
        Assert.IsFalse(journal.CanGoForward, "Expected nothing to go forward to.");
    }

    [TestMethod]
    public void Record_must_drop_the_oldest_entry_when_the_capacity_is_exceeded()
    {
        var journal = new NavigationJournal(3);
        journal.Record(RouteOf("A"), HistoryMode.Push);
        journal.Record(RouteOf("B"), HistoryMode.Push);
        journal.Record(RouteOf("C"), HistoryMode.Push);
        var changes = Listen(journal);

        journal.Record(RouteOf("D"), HistoryMode.Push);

        CollectionAssert.AreEqual(new[] { "B", "C", "D" }, journal.Entries.Select(e => e.Path).ToArray(), "Expected the oldest entry to go.");
        Assert.AreEqual(2, journal.CurrentIndex, "Expected the position to follow the entries that moved.");
        Assert.AreEqual(1, changes.Single().PreviousIndex, "Expected the previous position to be where the previous page is now.");
    }

    [TestMethod]
    public void Record_must_not_repeat_an_id_after_the_journal_was_cleared()
    {
        var journal = new NavigationJournal();
        var before = journal.Record(RouteOf("A"), HistoryMode.Push);
        journal.Clear();

        var after = journal.Record(RouteOf("A"), HistoryMode.Push);

        Assert.AreNotEqual(before!.Id, after!.Id, "Expected an entry of the browser history that was kept to find no entry of the new journal.");
    }

    [TestMethod]
    public void Record_must_tell_the_new_and_the_previous_position()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        var changes = Listen(journal);

        journal.Record(RouteOf("B"), HistoryMode.Push);

        Assert.AreEqual(1, changes.Single().Index, "Expected the position of the new entry.");
        Assert.AreEqual(0, changes.Single().PreviousIndex, "Expected the position it came from.");
        Assert.AreEqual("B", changes.Single().Entry!.Path, "Expected the new entry.");
    }

    [TestMethod]
    public void Move_must_change_the_position_and_announce_it()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        journal.Record(RouteOf("B"), HistoryMode.Push);
        var changes = Listen(journal);

        journal.Move(-1);

        Assert.AreEqual("A", journal.Current!.Path, "Expected the previous entry to be current.");
        Assert.IsTrue(journal.CanGoForward, "Expected the entry that was left to be ahead.");
        Assert.AreEqual(JournalChange.Moved, changes.Single().Change, "Expected a move.");
        Assert.AreEqual(1, changes.Single().PreviousIndex, "Expected the position it came from.");
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void MoveTo_must_refuse_a_position_outside_of_the_journal(int index)
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        journal.Record(RouteOf("B"), HistoryMode.Push);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => journal.MoveTo(index), "Expected a position with no entry to be refused.");
        Assert.AreEqual(1, journal.CurrentIndex, "Expected the position not to change.");
    }

    [TestMethod]
    public void MoveTo_the_current_position_must_not_announce_a_change()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        var changes = Listen(journal);

        journal.MoveTo(0);

        Assert.IsEmpty(changes, "Expected no change when nothing moved.");
    }

    [TestMethod]
    public void TryMoveTo_must_find_an_entry_by_its_id()
    {
        var journal = new NavigationJournal();
        var first = journal.Record(RouteOf("A"), HistoryMode.Push)!;
        journal.Record(RouteOf("B"), HistoryMode.Push);

        Assert.IsTrue(journal.TryMoveTo(first.Id), "Expected the entry to be found.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the position of the entry.");
        Assert.IsFalse(journal.TryMoveTo(long.MaxValue), "Expected an id from the earlier run of the application not to be found.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the position not to change.");
    }

    [TestMethod]
    public void Clear_must_remove_the_entries_and_announce_it_once()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        journal.Record(RouteOf("B"), HistoryMode.Push);
        var changes = Listen(journal);

        journal.Clear();
        journal.Clear();

        Assert.IsEmpty(journal.Entries, "Expected no entries.");
        Assert.AreEqual(-1, journal.CurrentIndex, "Expected no position.");
        Assert.AreEqual(JournalChange.Cleared, changes.Single().Change, "Expected one clear; clearing an empty journal changes nothing.");
        Assert.IsNull(changes.Single().Entry, "Expected no current entry after the clear.");
    }

    [TestMethod]
    public void Entries_must_be_a_copy_that_later_visits_do_not_change()
    {
        var journal = new NavigationJournal();
        journal.Record(RouteOf("A"), HistoryMode.Push);
        var snapshot = journal.Entries;

        journal.Record(RouteOf("B"), HistoryMode.Push);

        Assert.HasCount(1, snapshot, "Expected the earlier list not to grow.");
    }

    [TestMethod]
    public void Every_journal_must_have_its_own_session_id()
    {
        Assert.AreNotEqual(new NavigationJournal().SessionId, new NavigationJournal().SessionId, "Expected the browser to tell an entry of an earlier run from its own.");
    }

    [TestMethod]
    public void A_journal_must_keep_at_least_one_entry()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NavigationJournal(0), "Expected a journal that keeps nothing to be refused.");
    }

    [TestMethod]
    public void Record_must_be_safe_to_call_from_several_threads()
    {
        var journal = new NavigationJournal(1000);

        Parallel.For(0, 200, i => journal.Record(RouteOf("Page" + i), HistoryMode.Push));

        Assert.HasCount(200, journal.Entries, "Expected every different location to be kept.");
        Assert.HasCount(200, journal.Entries.Select(e => e.Id).Distinct().ToArray(), "Expected every entry to have its own id.");
    }
}
