namespace Bot.Entities;

public class TrackQueue
{
    private readonly Queue<ExtendedLavaTrack> _tracks = new();

    public void Shuffle()
    {
        
    }

    public void RemoveAt()
    {
        
    }

    public void Enqueue(ExtendedLavaTrack track)
    {
        _tracks.Enqueue(track);
    }

    public void Dequeue()
    {
        
    }

    public bool TryDequeue(out ExtendedLavaTrack o)
    {
        o = _tracks.Dequeue();
        return true;
    }
}
