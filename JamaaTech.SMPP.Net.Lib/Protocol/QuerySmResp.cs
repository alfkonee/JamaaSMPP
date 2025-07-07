/************************************************************************
 * Copyright (C) 2007 Jamaa Technologies
 *
 * This file is part of Jamaa SMPP Library.
 *
 * Jamaa SMPP Library is free software. You can redistribute it and/or modify
 * it under the terms of the Microsoft Reciprocal License (Ms-RL)
 *
 * You should have received a copy of the Microsoft Reciprocal License
 * along with Jamaa SMPP Library; See License.txt for more details.
 *
 * Author: Benedict J. Tesha
 * benedict.tesha@jamaatech.com, www.jamaatech.com
 *
 ************************************************************************/

using JamaaTech.Smpp.Net.Lib.Util;

namespace JamaaTech.Smpp.Net.Lib.Protocol;

public sealed class QuerySmResp : ResponsePDU
{
    internal QuerySmResp(PDUHeader header, SmppEncodingService smppEncodingService)
        : base(header, smppEncodingService)
    {
        MessageId = string.Empty;
        FinalDate = string.Empty;
        MessageState = MessageState.Unknown;
        ErrorCode = 0;
    }

    public override SmppEntityType AllowedSource => SmppEntityType.SMSC;
    public override SmppSessionState AllowedSession => SmppSessionState.Transmitter;
    
    public string MessageId { get; set; }
    public string FinalDate { get; set; }
    public MessageState MessageState { get; set; }
    public byte ErrorCode { get; set; }

    protected override byte[] GetBodyData()
    {
        var buffer = new ByteBuffer(16);
        buffer.Append(EncodeCString(MessageId, vSmppEncodingService));
        buffer.Append(EncodeCString(FinalDate, vSmppEncodingService));
        buffer.Append((byte)MessageState);
        buffer.Append(ErrorCode);
        return buffer.ToBytes();
    }

    protected override void Parse(ByteBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        
        MessageId = DecodeCString(buffer, vSmppEncodingService);
        FinalDate = DecodeCString(buffer, vSmppEncodingService);
        MessageState = (MessageState)GetByte(buffer);
        ErrorCode = GetByte(buffer);
        
        // This PDU has no option parameters.
        // If the buffer still contains something, we received more than required bytes
        if (buffer.Length > 0) throw new TooManyBytesException();
    }
}