using System.Collections;

namespace Bot.Entities;

public class TrackQueue : IEnumerable<ExtendedLavaTrack>
{
    private readonly LinkedList<ExtendedLavaTrack> _tracks = new();
    private readonly Random _random = new();

    public void Shuffle()
    {
        var tracksCount = _tracks.Count;

        if (tracksCount < 2) return;

        var shadow = _tracks.ToArray();
        
        _tracks.Clear();

        for (var i = tracksCount - 1; i > 0; i--)
        {
            var r = _random.Next(i + 1);
            (shadow[i], shadow[r]) = (shadow[r], shadow[i]);
        }
        
        foreach (var track in shadow)
        {
            _tracks.AddFirst(track);
        }
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _tracks.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        for (var currentNode = _tracks.First; currentNode != null; currentNode = currentNode.Next)
        {
            if (index == 0)
            {
                _tracks.Remove(currentNode);
                break;
            }
            
            index--;
        }
    }

    public bool IsEmpty()
    {
        return _tracks.Count < 1;
    }
    
    public void Enqueue(ExtendedLavaTrack track, bool top = false)
    {
        if (track == null) throw new ArgumentNullException(nameof(track));

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
        if (tracks == null) throw new ArgumentNullException(nameof(tracks));

        foreach (var extendedLavaTrack in tracks)
        {
            Enqueue(extendedLavaTrack);
        }
    }

    public bool TryDequeue(out ExtendedLavaTrack? nextTrack)
    {
        nextTrack = default;
        
        if (IsEmpty())
        {
            return false;
        }

        nextTrack = _tracks.First?.Value;
        _tracks.RemoveFirst();

        return nextTrack != null;
    }

    public IEnumerator<ExtendedLavaTrack> GetEnumerator()
    {
        for (var node = _tracks.First; node != null; node = node.Next)
        {
            yield return node.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
