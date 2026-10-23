using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class JournalTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<string> _published = new();
    private readonly Journal _journal;

    public JournalTests()
    {
        _bus.Subscribe<JournalEntryAdded>(e => _published.Add(e.EntryKey));
        _journal = new Journal(_state, _bus);
    }

    [Fact]
    public void Add_StoresEntryInWorldStateAndPublishes()
    {
        var added = _journal.Add("obj.anfora.examinar");

        Assert.True(added);
        Assert.Equal(new[] { "obj.anfora.examinar" }, _state.JournalEntries);
        Assert.Equal(new[] { "obj.anfora.examinar" }, _published);
    }

    [Fact]
    public void Add_SameEntryTwice_DoesNotDuplicateOrRepublish()
    {
        _journal.Add("obj.anfora.examinar");

        var addedAgain = _journal.Add("obj.anfora.examinar");

        Assert.False(addedAgain);
        Assert.Single(_journal.Entries);
        Assert.Single(_published);
    }

    [Fact]
    public void Entries_KeepWritingOrder()
    {
        _journal.Add("b");
        _journal.Add("a");

        Assert.Equal(new[] { "b", "a" }, _journal.Entries);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_BlankKey_Throws(string key)
    {
        Assert.ThrowsAny<ArgumentException>(() => _journal.Add(key));
    }
}
