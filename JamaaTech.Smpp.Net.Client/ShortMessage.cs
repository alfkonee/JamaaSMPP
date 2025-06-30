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
using System.Collections.Generic;
using JamaaTech.Smpp.Net.Lib;
using JamaaTech.Smpp.Net.Lib.Protocol;

namespace JamaaTech.Smpp.Net.Client;

/// <summary>
/// Defines a base class for different types of messages that can be used with <see cref="SmppClient"/>
/// </summary>
public abstract class ShortMessage
{
    #region Variables
    protected string _sourceAddress;
    protected string _destinationAddress;
    private int _messageCount;
    private int _segmentId;
    private int _sequenceNumber;
    private bool _registerDeliveryNotification;
    private string _receiptedMessageId;
    private string _userMessageReference;
    private bool _submitUserMessageReference;
    private MessageState? _messageState;
    private byte[] _networkErrorCode;
    #endregion

    #region Constructors
    protected ShortMessage()
    {
        _sourceAddress = string.Empty;
        _destinationAddress = string.Empty;
        _segmentId = -1;
        _submitUserMessageReference = true;
    }

    protected ShortMessage(int segmentId, int messageCount, int sequenceNumber)
        : this()
    {
        _segmentId = segmentId;
        _messageCount = messageCount;
        _sequenceNumber = sequenceNumber;
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets a <see cref="ShortMessage"/> source address
    /// </summary>
    public string SourceAddress
    {
        get => _sourceAddress;
        set => _sourceAddress = value;
    }

    /// <summary>
    /// Gets or sets a <see cref="ShortMessage"/> destination address
    /// </summary>
    public string DestinationAddress
    {
        get => _destinationAddress;
        set => _destinationAddress = value;
    }

    /// <summary>
    /// Gets or sets a <see cref="ShortMessage"/> receipted message identifier.
    /// </summary>
    /// <value>
    /// The receipted message identifier.
    /// </value>
    public string ReceiptedMessageId
    {
        get => _receiptedMessageId;
        set => _receiptedMessageId = value;
    }

    /// <summary>
    /// Gets or sets a <see cref="ShortMessage"/> user message reference.
    /// </summary>
    /// <value>
    /// The user message reference.
    /// </value>
    public string UserMessageReference
    {
        get => _userMessageReference;
        set => _userMessageReference = value;
    }

    /// <summary>
    /// Gets or sets a <see cref="System.Boolean"/> value that indicates if the <see cref="UserMessageReference"/> should be sent to SMSC.
    /// </summary>
    public bool SubmitUserMessageReference
    {
        get => _submitUserMessageReference;
        set => _submitUserMessageReference = value;
    }

    /// <summary>
    /// Gets the index of this message segment in a group of concatenated message segments
    /// </summary>
    public int SegmentId => _segmentId;

    /// <summary>
    /// Gets the sequence number for a group of concatenated message segments
    /// </summary>
    public int SequenceNumber => _sequenceNumber;

    /// <summary>
    /// Gets a value indicating the total number of message segments in a concatenated message
    /// </summary>
    public int MessageCount => _messageCount;

    /// <summary>
    /// Gets or sets a <see cref="System.Boolean"/> value that indicates if a delivery notification should be sent for <see cref="ShortMessage"/>
    /// </summary>
    public bool RegisterDeliveryNotification
    {
        get => _registerDeliveryNotification;
        set => _registerDeliveryNotification = value;
    }

    /// <summary>
    /// Gets or sets a <see cref="MessageStateType"/> value that indicates the ESME the final message state for an SMSC Delivery Receipt.
    /// </summary>
    public MessageState? MessageState
    {
        get => _messageState;
        set => _messageState = value;
    }

    /// <summary>
    /// Gets or sets a <see cref="Byte[]"/> value that indicates Network error code.  May be present for SMSC Delivery Receipts and
    /// Intermediate Notifications.  See section 5.3.2.31 for more information.
    /// </summary>
    public byte[] NetworkErrorCode
    {
        get => _networkErrorCode;
        set => _networkErrorCode = value;
    }
    #endregion

    #region Methods
    internal IEnumerable<SendSmPDU> GetMessagePDUs(DataCoding defaultEncoding, SmppEncodingService smppEncodingService,
        SmppAddress destAddress, SmppAddress srcAddress)
    {
        return GetPDUs(defaultEncoding, smppEncodingService, destAddress, srcAddress);
    }

    protected abstract IEnumerable<SendSmPDU> GetPDUs(DataCoding defaultEncoding, SmppEncodingService smppEncodingService,
        SmppAddress destAddress = null, SmppAddress srcAddress = null);
    #endregion
}