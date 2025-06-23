using JamaaTech.Smpp.Net.Lib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using JamaaTech.Smpp.Net.Lib.Protocol;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace Elyfe.Smpp.Client;

public class SmscClient : ISmscClient
{
    private readonly ILogger<SmscClient> _logger;
    private readonly SmscOptions _options;
    private SmppClientSession? _session;
    private Timer? _reconnectTimer;
    private SmppConnectionState _connectionState;
    private readonly Lock _syncRoot = new();

    public SmppConnectionState ConnectionState => _connectionState;

    public event EventHandler<SmppConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<DeliverSmEventArgs>? DeliverSmReceived;

    public SmscClient(ILogger<SmscClient> logger, IOptions<SmscOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _connectionState = SmppConnectionState.Closed;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (_connectionState != SmppConnectionState.Closed)
            {
                _logger.LogWarning("ConnectAsync called while not in a closed state. Current state: {ConnectionState}", _connectionState);
                return Task.CompletedTask;
            }

            ChangeState(SmppConnectionState.Connecting);
        }

        try
        {
            StopReconnectTimer();

            var bindInfo = new SessionBindInfo
            {
                ServerName = _options.Host,
                Port = _options.Port,
                SystemID = _options.SystemId,
                Password = _options.Password
            };
            var encodingService = new SmppEncodingService();

            _logger.LogInformation("Binding SMPP session...");
            _session = CreateAndBindSession(bindInfo, encodingService);
            _logger.LogInformation("SMPP session bound successfully.");

            _session.SessionClosed += OnSessionClosed;
            _session.PduReceived += OnPduReceived;

            ChangeState(SmppConnectionState.Bound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bind SMPP session.");
            _session = null; 
            ChangeState(SmppConnectionState.Closed);
            StartReconnectTimer();
            throw;
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (_connectionState == SmppConnectionState.Closed)
            {
                return Task.CompletedTask;
            }
        }

        StopReconnectTimer();
        _session?.EndSession();

        return Task.CompletedTask;
    }

    public Task<SubmitSmResp> SendSingleSmsAsync(SubmitSm pdu, CancellationToken cancellationToken = default)
    {
        if (ConnectionState != SmppConnectionState.Bound)
            throw new InvalidOperationException($"Cannot send SMS while connection state is {ConnectionState}. Client must be bound.");

        if (_session == null)
            throw new InvalidOperationException("Session is not initialized. Cannot send SMS.");

        _logger.LogDebug("Sending SubmitSm: {Pdu}", pdu);

        return Task.Run(() => (SubmitSmResp)_session.SendPdu(pdu, 30000), cancellationToken); 
    }

    protected virtual SmppClientSession CreateAndBindSession(SessionBindInfo bindInfo, SmppEncodingService encodingService)
    {
        return SmppClientSession.Bind(bindInfo, 30000, encodingService);
    }

    private void OnSessionClosed(object? sender, SmppSessionClosedEventArgs e)
    {
        _logger.LogInformation("SMPP session closed. Reason: {Reason}", e.Reason);

        if (e.Exception != null)
        {
            _logger.LogError(e.Exception, "The SMPP session was closed due to an exception.");
        }

        ChangeState(SmppConnectionState.Closed);
        
        if (e.Reason == SmppSessionCloseReason.TcpIpSessionError)
            StartReconnectTimer();
    }

    private void OnPduReceived(object? sender, PduReceivedEventArgs e)
    {
        if (e.Request is DeliverSm deliverSm)
        {
            var eventArgs = new DeliverSmEventArgs(deliverSm);
            DeliverSmReceived?.Invoke(this, eventArgs);
        }
    }

    private void ChangeState(SmppConnectionState newState)
    {
        SmppConnectionState oldState;
        lock (_syncRoot)
        {
            if (_connectionState == newState) return;
            oldState = _connectionState;
            _connectionState = newState;
        }
        ConnectionStateChanged?.Invoke(this, new SmppConnectionStateChangedEventArgs(newState, oldState));
    }

    private void StartReconnectTimer()
    {
        if (!_options.Reconnect || _options.ReconnectInterval <= 0) return;

        lock (_syncRoot)
        {
            if (_reconnectTimer == null)
            {
                _reconnectTimer = new Timer(async _ => await ReconnectAsync(), null, _options.ReconnectInterval, Timeout.Infinite);
                _logger.LogInformation("Reconnect timer started. Will attempt to reconnect in {ReconnectInterval}ms.", _options.ReconnectInterval);
            }
            else
            {
                _reconnectTimer.Change(_options.ReconnectInterval, Timeout.Infinite);
            }
        }
    }

    private void StopReconnectTimer()
    {
        _reconnectTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private async Task ReconnectAsync()
    {
        _logger.LogInformation("Attempting to reconnect...");
        try
        {
            await ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reconnect attempt failed.");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopReconnectTimer();
            _reconnectTimer?.Dispose();
            _session?.EndSession();
            _session = null;
            ChangeState(SmppConnectionState.Closed);
        }
    }
}
