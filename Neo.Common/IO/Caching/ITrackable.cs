namespace Neo.Common.IO.Caching
{
    public interface ITrackable<TKey>
    {
        TKey Key { get; }
        TrackState TrackState { get; set; }
    }
}
