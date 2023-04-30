using Bot.Entities;
using Discord;
using Moq;
using Victoria.Player;

namespace BotTests;

[TestClass]
public class TrackQueueTest
{
    private ExtendedLavaTrack GetTrack()
    {
        var user = new Mock<IUser>();
        var track = new Mock<LavaTrack>();

        return new ExtendedLavaTrack(track.Object, user.Object);
    }

    [TestMethod]
    public void Should_Dequeue_On_Empty_Queue()
    {
        var queue = new TrackQueue();

        var isSuccessful = queue.TryDequeue(out var track);

        Assert.IsFalse(isSuccessful);
        Assert.IsNull(track);
    }

    [TestMethod]
    public void Should_Enqueue_New_Track()
    {
        var queue = new TrackQueue();

        queue.Enqueue(GetTrack());

        Assert.IsFalse(queue.IsEmpty());
    }

    [TestMethod]
    public void Should_Successfully_Dequeue()
    {
        var queue = new TrackQueue();
        var expectedTrack = GetTrack();

        queue.Enqueue(expectedTrack);

        var isSuccessful = queue.TryDequeue(out var nextTrack);

        Assert.IsTrue(isSuccessful);
        Assert.AreEqual(expectedTrack, nextTrack);
    }

    [TestMethod]
    public void Should_Throw_On_Null_Track_Enqueue()
    {
        var queue = new TrackQueue();

        Assert.ThrowsException<ArgumentNullException>(() => queue.Enqueue(null));
    }

    [TestMethod]
    public void Should_Enqueue_On_Top()
    {
        var queue = new TrackQueue();

        queue.Enqueue(GetTrack());
        queue.Enqueue(GetTrack());
        queue.Enqueue(GetTrack());

        var expectedTrack = GetTrack();

        queue.Enqueue(expectedTrack, true);

        var isSuccessful = queue.TryDequeue(out var nextTrack);

        Assert.IsTrue(isSuccessful);
        Assert.AreEqual(expectedTrack, nextTrack);
    }

    [TestMethod]
    public void Should_Throw_On_RemoveAt_Invalid_Index()
    {
        var queue = new TrackQueue();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.RemoveAt(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.RemoveAt(2));
    }

    [TestMethod]
    public void Should_Remove_Track_On_Index()
    {
        var queue = new TrackQueue();

        var trackToDelete = GetTrack();
        
        queue.Enqueue(GetTrack());
        queue.Enqueue(GetTrack());
        queue.Enqueue(trackToDelete);
        queue.Enqueue(GetTrack());
        
        queue.RemoveAt(2);
        
        Assert.IsFalse(queue.Contains(trackToDelete));
    }
}
