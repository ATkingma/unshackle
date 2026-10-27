using Xunit.Abstractions;

public class SinkUser
{
    private readonly IMessageSink _sink;

    public SinkUser(IMessageSink sink)
    {
        _sink = sink;
    }

    public IMessageSink Sink => _sink;
}
