using JamaaTech.Smpp.Net.Lib.Protocol;
using System;

namespace Elyfe.Smpp.Client;

public class DeliverSmEventArgs : EventArgs
{
    public DeliverSm Pdu { get; }

    public DeliverSmEventArgs(DeliverSm pdu)
    {
        Pdu = pdu;
    }
}
