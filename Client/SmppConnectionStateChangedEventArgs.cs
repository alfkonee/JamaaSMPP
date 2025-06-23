using System;

namespace Elyfe.Smpp.Client;

public class SmppConnectionStateChangedEventArgs : EventArgs
{
    public SmppConnectionState NewState { get; }
    public SmppConnectionState OldState { get; }

    public SmppConnectionStateChangedEventArgs(SmppConnectionState newState, SmppConnectionState oldState)
    {
        NewState = newState;
        OldState = oldState;
    }
}
