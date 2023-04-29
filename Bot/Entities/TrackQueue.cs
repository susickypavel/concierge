using System.Collections;

namespace Bot.Entities;

public class TrackQueue : IEnumerable<ExtendedLavaTrack>
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

    public bool IsEmpty()
    {
        return _tracks.Count < 1;
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
        if (IsEmpty())
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

    public IEnumerator<ExtendedLavaTrack> GetEnumerator()
    {
        for (var node = _tracks.First; node != null; node = node.Next) {
            yield return node.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
