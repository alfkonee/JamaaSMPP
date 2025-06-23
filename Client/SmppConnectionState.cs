namespace Elyfe.Smpp.Client;

public enum SmppConnectionState
{
    Closed,
    Connecting,
    Connected,
    Binding,
    Bound,
    Unbinding
}
