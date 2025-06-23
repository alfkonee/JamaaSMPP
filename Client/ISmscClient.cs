using JamaaTech.Smpp.Net.Lib.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elyfe.Smpp.Client;

public interface ISmscClient : IDisposable
{
    SmppConnectionState ConnectionState { get; }
    event EventHandler<SmppConnectionStateChangedEventArgs> ConnectionStateChanged;
    event EventHandler<DeliverSmEventArgs> DeliverSmReceived;
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<SubmitSmResp> SendSingleSmsAsync(SubmitSm pdu, CancellationToken cancellationToken = default);
}
