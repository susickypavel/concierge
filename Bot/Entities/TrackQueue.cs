namespace Bot.Entities;

public class TrackQueue
{
    private readonly LinkedList<ExtendedLavaTrack> _tracks = new();

    public void Shuffle()
    {
        throw new NotImplementedException();
    }

    public void RemoveAt()
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        _tracks.Clear();
    }

    public void Enqueue(ExtendedLavaTrack track, bool top = false)
    {
        if (track == null)
        {
            throw new ArgumentNullException(nameof(track));
        }
        
        if (top)
        {
            _tracks.AddFirst(track);
        }
        else
        {
            _tracks.AddLast(track);
        }
    }

    public void Enqueue(IEnumerable<ExtendedLavaTrack> tracks)
    {
        foreach (var extendedLavaTrack in tracks)
        {
            Enqueue(extendedLavaTrack);
        }
    }

    public bool TryDequeue(out ExtendedLavaTrack? o)
    {
        if (_tracks.Count < 1)
        {
            o = default;
            return false;
        }
        
        var nextTrack = _tracks.First?.Value;
        _tracks.RemoveFirst();

        if (nextTrack == null)
        {
            o = default;
            return false;
        }

        o = nextTrack;

        return true;
    }
}
