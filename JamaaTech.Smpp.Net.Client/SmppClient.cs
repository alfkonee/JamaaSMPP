/************************************************************************
 * Copyright (C) 2008 Jamaa Technologies
 *
 * This file is part of Jamaa SMPP Client Library.
 *
 * Jamaa SMPP Client Library is free software. You can redistribute it and/or modify
 * it under the terms of the Microsoft Reciprocal License (Ms-RL)
 *
 * You should have received a copy of the Microsoft Reciprocal License
 * along with Jamaa SMPP Client Library; See License.txt for more details.
 *
 * Author: Benedict J. Tesha
 * benedict.tesha@jamaatech.com, www.jamaatech.com
 *
 ************************************************************************/

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JamaaTech.Smpp.Net.Lib;
using JamaaTech.Smpp.Net.Lib.Logging;
using JamaaTech.Smpp.Net.Lib.Protocol;
using JamaaTech.Smpp.Net.Lib.Protocol.Tlv;
using JamaaTech.Smpp.Net.Lib.Util;

namespace JamaaTech.Smpp.Net.Client;

public sealed class SmppClient : IDisposable
{
    private static readonly ILog Log =
        LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

    #region Constructors

    public SmppClient()
    {
        Properties = new SmppConnectionProperties();
        SmppEncodingService = new SmppEncodingService();
        _vConnSyncRoot = new object();
        _vAutoReconnectDelay = 10000;
        ConnectionTimeout = 5000;
        //--
        _vTimer = new Timer(AutoReconnectTimerEventHandler, null, Timeout.Infinite,
            _vAutoReconnectDelay);
        //--
        Name = "";
        ConnectionState = SmppConnectionState.Closed;
        KeepAliveInterval = 30000;
        //--
        _vSendMessageCallBack += SendMessage;
    }

    #endregion

    #region Variables

    private SmppClientSession _vTrans;
    private SmppClientSession _vRecv;
    private readonly object _vConnSyncRoot;
    private readonly Timer _vTimer;
    private int _vAutoReconnectDelay;
    private readonly SendMessageCallBack _vSendMessageCallBack;

    //--
    private static readonly TraceSwitch _vTraceSwitch = new("SmppClientSwitch", "SmppClient trace switch");

    #endregion

    #region Events

    /// <summary>
    ///     Occurs when a message is received
    /// </summary>
    public event EventHandler<MessageEventArgs> MessageReceived;

    /// <summary>
    ///     Occurs when a message delivery notification is received
    /// </summary>
    public event EventHandler<MessageEventArgs> MessageDelivered;

    /// <summary>
    ///     Occurs when connection state changes
    /// </summary>
    public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

    /// <summary>
    ///     Occurs when a message is successfully sent
    /// </summary>
    public event EventHandler<MessageEventArgs> MessageSent;

    /// <summary>
    ///     Occurs when <see cref="SmppClient" /> is started or shut down
    /// </summary>
    public event EventHandler<StateChangedEventArgs> StateChanged;

    #endregion

    #region Properties

    /// <summary>
    ///     Gets or sets a value indicating the time in miliseconds to wait before attemping to reconnect after a connection is
    ///     lost
    /// </summary>
    public int AutoReconnectDelay
    {
        get => _vAutoReconnectDelay;
        set => _vAutoReconnectDelay = value;
    }

    /// <summary>
    ///     Indicates the current state of <see cref="SmppClient" />
    /// </summary>
    public SmppConnectionState ConnectionState { get; private set; }

    /// <summary>
    ///     Gets or sets a value that indicates the time in miliseconds in which Enquire Link PDUs are periodically sent
    /// </summary>
    public int KeepAliveInterval { get; set; }

    /// <summary>
    ///     Gets or sets a value that specifies the name for this <see cref="SmppClient" />
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets an instance of <see cref="SmppConnectionProperties" /> that represents connection properties for this
    ///     <see cref="SmppClient" />
    /// </summary>
    public SmppConnectionProperties Properties { get; }

    /// <summary>
    ///     Gets or sets a value that speficies the amount of time after which a synchronous
    ///     <see cref="SmppClient.SendMessage" /> call will timeout
    /// </summary>
    public int ConnectionTimeout { get; set; }

    /// <summary>
    ///     Gets a <see cref="System.Boolean" /> value indicating if an instance of <see cref="SmppClient" /> is started
    /// </summary>
    public bool Started { get; private set; }

    public SmppEncodingService SmppEncodingService { get; set; }

    /// <summary>
    ///     Gets a <see cref="System.Exception" /> indicating if an instance of <see cref="SmppClient" /> has an occurred
    ///     exception while connecting.
    /// </summary>
    /// <value>
    ///     The last exception.
    /// </value>
    public Exception LastException { get; private set; }

    #endregion

    #region Methods

    #region Interface Methods

    public ResponsePDU QueryMessageStatus(string messageId, string sourceAddress)
    {
        if (string.IsNullOrEmpty(messageId)) throw new ArgumentNullException(nameof(messageId));
        if (string.IsNullOrEmpty(sourceAddress)) throw new ArgumentNullException(nameof(sourceAddress));
        if (ConnectionState != SmppConnectionState.Connected)
            throw new SmppClientException(
                "Sending message operation failed because the SmppClient is not connected");
        var source = new SmppAddress
        {
            Address = sourceAddress,
            Npi = Properties.AddressNpi,
            Ton = Properties.AddressTon
        };
        var queryPdu = new QuerySm(SmppEncodingService, source)
        {
            MessageID = messageId
        };

        var response = SendPdu(queryPdu, _vTrans.DefaultResponseTimeout);
        if (response is QuerySmResp queryResp) return queryResp;
        throw new NotImplementedException("WIP");
    }

    /// <summary>
    ///     Sends a message to a remote SMPP server.
    /// </summary>
    /// <param name="message">The message to be sent.</param>
    /// <param name="timeOut">The timeout duration for the send operation, specified in milliseconds.</param>
    /// <param name="destinationTON">The type of number for the destination address (optional).</param>
    /// <param name="destinationNPI">The numbering plan for the destination address (optional).</param>
    public void SendMessage(ShortMessage message, int timeOut)
    {
        SendMessage(message, timeOut, TypeOfNumber.Unknown);
    }

    /// <summary>
    ///     Sends message to a remote SMPP server
    /// </summary>
    /// <param name="message">A message to send</param>
    /// <param name="timeOut">A value in miliseconds after which the send operation times out</param>
    public void SendMessage(ShortMessage message, int timeOut, TypeOfNumber destinationTON = TypeOfNumber.Unknown,
        NumberingPlanIndicator destinationNPI = NumberingPlanIndicator.Unknown)
    {
        if (message == null) throw new ArgumentNullException("message");

        //Check if connection is open
        if (ConnectionState != SmppConnectionState.Connected)
            throw new SmppClientException(
                "Sending message operation failed because the SmppClient is not connected");

        var srcAddress = new SmppAddress(Properties.AddressTon, Properties.AddressNpi,
            string.IsNullOrWhiteSpace(message.SourceAddress) ? Properties.SourceAddress : message.SourceAddress);
        var destAddress = new SmppAddress
            { Address = message.DestinationAddress, Ton = destinationTON, Npi = destinationNPI };
        var messagePdUs = message.GetMessagePDUs(Properties.DefaultEncoding, SmppEncodingService, destAddress,
            srcAddress);
        foreach (var pdu in messagePdUs)
        {
            string messageId = null;
            if (Log.IsDebugEnabled)
                Log.DebugFormat("SendMessage SendSmPDU: {0}",
                    LoggingExtensions.DumpString(pdu, SmppEncodingService));
            var resp = SendPdu(pdu, timeOut);
            if (resp is SubmitSmResp submitSmResp)
            {
                if (Log.IsDebugEnabled)
                    Log.DebugFormat("SendMessage Response: {0}",
                        LoggingExtensions.DumpString(resp, SmppEncodingService));
                messageId = submitSmResp.MessageID;
            }

            if (message.ReceiptedMessageId is { Length: > 0 })
                message.ReceiptedMessageId += $",{messageId}";
            else
                message.ReceiptedMessageId += messageId;
            RaiseMessageSentEvent(message);
        }
    }

    /// <summary>
    ///     Send PDU to a remote SMPP server
    /// </summary>
    /// <param name="pdu">
    ///     <see cref="RequestPDU" />
    /// </param>
    /// <param name="timeout">A value in miliseconds after which the send operation times out</param>
    /// <returns>
    ///     <see cref="ResponsePDU" />
    /// </returns>
    private ResponsePDU SendPdu(RequestPDU pdu, int timeout)
    {
        var resp = _vTrans.SendPdu(pdu, timeout);
        if (resp.Header.ErrorCode != SmppErrorCode.ESME_ROK) throw new SmppException(resp.Header.ErrorCode);

        return resp;
    }

    /// <summary>
    ///     Sends message to a remote SMPP server
    /// </summary>
    /// <param name="message">A message to send</param>
    public void SendMessage(ShortMessage message)
    {
        SendMessage(message, _vTrans.DefaultResponseTimeout);
    }

    /// <summary>
    ///     Sends message asynchronously to a remote SMPP server
    /// </summary>
    /// <param name="message">A message to send</param>
    /// <param name="timeout">A value in miliseconds after which the send operation times out</param>
    /// <param name="callback">An <see cref="AsyncCallback" /> delegate</param>
    /// <param name="state">An object that contains state information for this request</param>
    /// <returns>An <see cref="IAsyncResult" /> that references the asynchronous send message operation</returns>
    public IAsyncResult BeginSendMessage(ShortMessage message, int timeout, AsyncCallback callback,
        object state)
    {
#if NET40
            return vSendMessageCallBack.BeginInvoke(message, timeout, callback, state);

#else
        return Task.Run(() => _vSendMessageCallBack(message, timeout));
#endif
    }

    /// <summary>
    ///     Sends message asynchronously to a remote SMPP server
    /// </summary>
    /// <param name="message">A message to send</param>
    /// <param name="callback">An <see cref="AsyncCallback" /> delegate</param>
    /// <param name="state">An object that contains state information for this request</param>
    /// <returns>An <see cref="IAsyncResult" /> that references the asynchronous send message operation</returns>
    public IAsyncResult BeginSendMessage(ShortMessage message, AsyncCallback callback, object state)
    {
        var timeout = 0;
        timeout = _vTrans.DefaultResponseTimeout;
        return BeginSendMessage(message, timeout, callback, state);
    }

    /// <summary>
    ///     Ends a pending asynchronous send message operation
    /// </summary>
    /// <param name="result">An <see cref="IAsyncResult" /> that stores state information for this asynchronous operation</param>
    public void EndSendMessage(IAsyncResult result)
    {
        _vSendMessageCallBack.EndInvoke(result);
    }

    /// <summary>
    ///     Starts <see cref="SmppClient" /> and immediately connects to a remote SMPP server
    /// </summary>
    public void Start()
    {
        Started = true;
        _vTimer.Change(0, _vAutoReconnectDelay);
        RaiseStateChangedEvent(true);
    }

    /// <summary>
    ///     Starts <see cref="SmppClient" /> and waits for a specified amount of time before establishing connection
    /// </summary>
    /// <param name="connectDelay">A value in miliseconds to wait before establishing connection</param>
    public void Start(int connectDelay)
    {
        if (connectDelay < 0) connectDelay = 0;

        Started = true;
        _vTimer.Change(connectDelay, _vAutoReconnectDelay);
        RaiseStateChangedEvent(true);
    }

    /// <summary>
    ///     Immediately attempts to reestablish a lost connection without waiting for <see cref="SmppClient" /> to
    ///     automatically reconnect
    /// </summary>
    public void ForceConnect()
    {
        Open(ConnectionTimeout);
    }

    /// <summary>
    ///     Immediately attempts to reestablish a lost connection without waiting for <see cref="SmppClient" /> to
    ///     automatically reconnect
    /// </summary>
    /// <param name="timeout">A time in miliseconds after which a connection operation times out</param>
    public void ForceConnect(int timeout)
    {
        Open(timeout);
    }

    /// <summary>
    ///     Shuts down <see cref="SmppClient" />
    /// </summary>
    public void Shutdown()
    {
        if (!Started) return;

        Started = false;
        StopTimer();
        CloseSession();
        RaiseStateChangedEvent(false);
    }

    /// <summary>
    ///     Restarts <see cref="SmppClient" />
    /// </summary>
    public void Restart()
    {
        Shutdown();
        Start();
    }

    #endregion

    #region Helper Methods

    private void Open(int timeOut)
    {
        try
        {
            if (Monitor.TryEnter(_vConnSyncRoot))
            {
                //No thread is in a connecting or reconnecting state
                if (ConnectionState != SmppConnectionState.Closed)
                {
                    LastException =
                        new InvalidOperationException("You cannot open while the instance is already connected");
                    throw LastException;
                }

                //
                SessionBindInfo bindInfo = null;
                var useSepConn = false;
                lock (Properties.SyncRoot)
                {
                    bindInfo = Properties.GetBindInfo();
                    useSepConn = Properties.CanSeparateConnections;
                }

                try
                {
                    OpenSession(bindInfo, useSepConn, timeOut);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("OpenSession: {0}", ex, ex.Message);
                    if (_vTraceSwitch.TraceError) Trace.TraceError(ex.ToString());

                    LastException = ex;
                    throw;
                }

                LastException = null;
            }
            else
            {
                //Another thread is already in either a connecting or reconnecting state
                //Wait until the thread finishes
                Monitor.Enter(_vConnSyncRoot);
                //Now, the thread has finished connecting,
                //Check on the result if the thread encountered any problem during connection
                if (LastException != null) throw LastException;
            }
        }
        finally
        {
            Monitor.Exit(_vConnSyncRoot);
        }
    }

    private void OpenSession(SessionBindInfo bindInfo, bool useSeparateConnections, int timeOut)
    {
        ChangeState(SmppConnectionState.Connecting);
        var connStateConfig = (useSeparateConnections, bindInfo.AllowReceive, bindInfo.AllowTransmit);
        switch (connStateConfig)
        {
            case (true, true, true):
                //Create two separate sessions for sending and receiving
                //ReceiveConnection
                try
                {
                    bindInfo.AllowReceive = true;
                    bindInfo.AllowTransmit = false;
                    _vRecv = SmppClientSession.Bind(bindInfo, timeOut, SmppEncodingService);
                    InitializeSession(_vRecv);
                }
                catch
                {
                    ChangeState(SmppConnectionState.Closed);
                    //Start reconnect timer
                    StartTimer();
                    throw;
                }

                //Transmit Connection
                try
                {
                    bindInfo.AllowReceive = false;
                    bindInfo.AllowTransmit = true;
                    _vTrans = SmppClientSession.Bind(bindInfo, timeOut, SmppEncodingService);
                    InitializeSession(_vTrans);
                }
                catch
                {
                    try
                    {
                        _vRecv.EndSession();
                    }
                    catch
                    {
                        /*Silent catch*/
                    }

                    _vRecv = null;
                    ChangeState(SmppConnectionState.Closed);
                    //Start reconnect timer
                    StartTimer();
                    throw;
                }

                ChangeState(SmppConnectionState.Connected);
                break;
            case (_, true, false):
                //ReceiveConnection
                try
                {
                    bindInfo.AllowReceive = true;
                    bindInfo.AllowTransmit = false;
                    _vRecv = SmppClientSession.Bind(bindInfo, timeOut, SmppEncodingService);
                    InitializeSession(_vRecv);
                }
                catch
                {
                    ChangeState(SmppConnectionState.Closed);
                    //Start reconnect timer
                    StartTimer();
                    throw;
                }

                ChangeState(SmppConnectionState.Connected);
                break;
            case (_, false, true):
                //Transmit Connection
                try
                {
                    bindInfo.AllowReceive = false;
                    bindInfo.AllowTransmit = true;
                    _vTrans = SmppClientSession.Bind(bindInfo, timeOut, SmppEncodingService);
                    InitializeSession(_vTrans);
                }
                catch
                {
                    try
                    {
                        _vRecv.EndSession();
                    }
                    catch
                    {
                        /*Silent catch*/
                    }

                    _vRecv = null;
                    ChangeState(SmppConnectionState.Closed);
                    //Start reconnect timer
                    StartTimer();
                    throw;
                }

                ChangeState(SmppConnectionState.Connected);
                break;
            case (false, _, _):
                //BindTransceiver Connection
                try
                {
                    var session = SmppClientSession.Bind(bindInfo, timeOut, SmppEncodingService);
                    if (bindInfo.AllowTransmit)
                        _vTrans = session;
                    if (bindInfo.AllowReceive)
                        _vRecv = session;
                    InitializeSession(session);
                    ChangeState(SmppConnectionState.Connected);
                }
                catch (SmppException ex)
                {
                    if (ex.ErrorCode == SmppErrorCode.ESME_RINVCMDID)
                    {
                        //If SMSC returns ESME_RINVCMDID (Invalid command id)
                        //the SMSC might not be supporting the BindTransceiver PDU
                        //Therefore, we can try to use bind with separate connections
                        OpenSession(bindInfo, true, timeOut);
                    }
                    else
                    {
                        ChangeState(SmppConnectionState.Closed);
                        //Start background timer
                        StartTimer();
                        throw;
                    }
                }
                catch
                {
                    ChangeState(SmppConnectionState.Closed);
                    StartTimer();
                    throw;
                }

                break;
            case (_, false, false):
            default:
                _vTrans = null;
                _vRecv = null;
                ChangeState(SmppConnectionState.Closed);
                break;
        }
    }

    private void CloseSession()
    {
        var oldState = SmppConnectionState.Closed;

        oldState = ConnectionState;
        if (ConnectionState == SmppConnectionState.Closed) return;

        ConnectionState = SmppConnectionState.Closed;

        RaiseConnectionStateChangeEvent(SmppConnectionState.Closed, oldState);
        if (_vTrans != null) _vTrans.EndSession();

        if (_vRecv != null) _vRecv.EndSession();

        _vTrans = null;
        _vRecv = null;
    }

    private void InitializeSession(SmppClientSession session)
    {
        session.EnquireLinkInterval = KeepAliveInterval;
        session.PduReceived += PduReceivedEventHander;
        session.SessionClosed += SessionClosedEventHandler;
    }

    private void ChangeState(SmppConnectionState newState)
    {
        var oldState = SmppConnectionState.Closed;
        oldState = ConnectionState;
        ConnectionState = newState;
        Properties.SmscId = newState == SmppConnectionState.Connected ? _vTrans.SmscID : "";
        RaiseConnectionStateChangeEvent(newState, oldState);
    }

    private void RaiseMessageReceivedEvent(ShortMessage message)
    {
        if (MessageReceived != null) MessageReceived(this, new MessageEventArgs(message));
    }

    private void RaiseMessageDeliveredEvent(ShortMessage message)
    {
        if (MessageDelivered != null) MessageDelivered(this, new MessageEventArgs(message));
    }

    private void RaiseMessageSentEvent(ShortMessage message)
    {
        if (MessageSent != null) MessageSent(this, new MessageEventArgs(message));
    }

    private void RaiseConnectionStateChangeEvent(SmppConnectionState newState, SmppConnectionState oldState)
    {
        if (ConnectionStateChanged == null) return;

        var e =
            new ConnectionStateChangedEventArgs(newState, oldState, _vAutoReconnectDelay);
        ConnectionStateChanged(this, e);
        if (e.ReconnectInteval < 5000) e.ReconnectInteval = 5000;

        Interlocked.Exchange(ref _vAutoReconnectDelay, e.ReconnectInteval);
    }

    private void RaiseStateChangedEvent(bool started)
    {
        if (StateChanged == null) return;

        var e = new StateChangedEventArgs(started);
        StateChanged(this, e);
    }

    private void PduReceivedEventHander(object sender, PduReceivedEventArgs e)
    {
        //This handler is interested in SingleDestinationPDU only
        var pdu = e.Request as SingleDestinationPDU;
        if (pdu == null) return;

        if (Log.IsDebugEnabled)
            Log.DebugFormat("Received PDU: {0}", LoggingExtensions.DumpString(pdu, SmppEncodingService));

        if (_vTraceSwitch.TraceVerbose)
            Trace.WriteLine(string.Format("PduReceived: RequestType: {0}", e.Request?.GetType()?.Name));

        ShortMessage message = null;
        try
        {
            message = MessageFactory.CreateMessage(pdu);
        }
        catch (SmppException smppEx)
        {
            Log.ErrorFormat("200019:SMPP message decoding failure - {0} - {1} {2}", smppEx, smppEx.ErrorCode,
                new ByteBuffer(pdu.GetBytes()).DumpString(), smppEx.Message);
            if (_vTraceSwitch.TraceError)
                Trace.WriteLine(string.Format(
                    "200019:SMPP message decoding failure - {0} - {1} {2};",
                    smppEx.ErrorCode, new ByteBuffer(pdu.GetBytes()).DumpString(), smppEx.Message));

            //Notify the SMSC that we encountered an error while processing the message
            e.Response = pdu.CreateDefaultResponse();
            e.Response.Header.ErrorCode = smppEx.ErrorCode;
            return;
        }
        catch (Exception ex)
        {
            Log.ErrorFormat("200019:SMPP message decoding failure - {0}", ex,
                new ByteBuffer(pdu.GetBytes()).DumpString());
            if (_vTraceSwitch.TraceError)
                Trace.WriteLine(string.Format(
                    "200019:SMPP message decoding failure - {0} {1};",
                    new ByteBuffer(pdu.GetBytes()).DumpString(), ex.Message));

            //Let the receiver know that this message was rejected
            e.Response = pdu.CreateDefaultResponse();
            e.Response.Header.ErrorCode = SmppErrorCode.ESME_RX_P_APPN; //ESME Receiver Reject Message
            return;
        }

        if (message != null && Log.IsDebugEnabled)
            Log.DebugFormat("PduReceived: message: {0}",
                LoggingExtensions.DumpString(message, SmppEncodingService));

        if (_vTraceSwitch.TraceVerbose)
        {
#if DEBUG
            Console.WriteLine("PduReceived: pdu: Header:{0}, EsmClass:{1}, ServiceType:{2}, DataCoding:{3}", pdu.Header,
                pdu.EsmClass, pdu.ServiceType, pdu.DataCoding);
#endif
            Trace.WriteLine(string.Format(
                "PduReceived: pdu: Header:{0}, EsmClass:{1}, ServiceType:{2}, DataCoding:{3}", pdu.Header,
                pdu.EsmClass, pdu.ServiceType, pdu.DataCoding));
            if (message != null)
                Trace.WriteLine(string.Format(
                    "PduReceived: message: DestinationAddress:{0}, MessageCount:{1}, ReceiptedMessageId:{2}, RegisterDeliveryNotification:{3}, SegmentID:{4}, SequenceNumber:{5}, SourceAddress:{6}, UserMessageReference:{7}",
                    message.DestinationAddress, message.MessageCount, message.ReceiptedMessageId,
                    message.RegisterDeliveryNotification, message.SegmentId, message.SequenceNumber,
                    message.SourceAddress, message.UserMessageReference));
        }

        //If we have just a normal message
        if (((byte)pdu.EsmClass | 0xc3) == 0xc3)
        {
            RaiseMessageReceivedEvent(message);
        }
        //Or if we have received a delivery receipt
        else if ((pdu.EsmClass & EsmClass.DeliveryReceipt) == EsmClass.DeliveryReceipt)
        {
            // Extract receipted message id
            message.ReceiptedMessageId = pdu.GetOptionalParamString(Tag.receipted_message_id);
            // Extract receipted message state
            message.MessageState = pdu.GetOptionalParamByte<MessageState>(Tag.message_state);
            // Extract receipted network error code
            message.NetworkErrorCode = pdu.GetOptionalParamBytes(Tag.network_error_code);
            // Extract user message reference
            message.UserMessageReference = pdu.GetOptionalParamString(Tag.user_message_reference);
            RaiseMessageDeliveredEvent(message);
        }
    }

    private void SessionClosedEventHandler(object sender, SmppSessionClosedEventArgs e)
    {
        if (e.Reason != SmppSessionCloseReason.EndSessionCalled)
            //Start timer 
            StartTimer();

        CloseSession();
    }

    private void StartTimer()
    {
        _vTimer.Change(_vAutoReconnectDelay, _vAutoReconnectDelay);
    }

    private void StopTimer()
    {
        _vTimer.Change(Timeout.Infinite, _vAutoReconnectDelay);
    }

    private void AutoReconnectTimerEventHandler(object state)
    {
        //Do not reconnect if AutoReconnectDalay < 0 or if SmppClient is shutdown
        if (AutoReconnectDelay <= 0 || !Started) return;

        //Stop the timer from raising subsequent events before
        //the current thread exists
        StopTimer();

        var timeOut = 0;
        timeOut = ConnectionTimeout;
        try
        {
            Open(timeOut);
        }
        catch (Exception)
        {
            /*Do nothing*/
        }

        if (ConnectionState == SmppConnectionState.Closed)
            StartTimer();
        else
            StopTimer();
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposeManagedResorces)
    {
        try
        {
            Shutdown();
            if (_vTimer != null) _vTimer.Dispose();
        }
        catch
        {
            /*Sielent catch*/
        }
    }

    #endregion
}