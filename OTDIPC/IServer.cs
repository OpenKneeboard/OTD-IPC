namespace OTDIPC;

public interface IServer : IDisposable
{
    public bool HaveClient { get; }
}