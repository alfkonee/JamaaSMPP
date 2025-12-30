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

using JamaaTech.Smpp.Net.Lib.Protocol;

namespace JamaaTech.Smpp.Net.Lib;

public class PDUErrorEventArgs : EventArgs
{
    #region Variable

    #endregion

    #region Constructors

    public PDUErrorEventArgs(PDUException exception, byte[] byteDump, PDUHeader header)
    {
        Exception = exception;
        ByteDump = byteDump;
        Header = header;
    }

    public PDUErrorEventArgs(PDUException exception, byte[] byteDump, PDUHeader header, PDU pdu)
        : this(exception, byteDump, header)
    {
        Pdu = pdu;
    }

    #endregion

    #region Properties

    public PDUException Exception { get; }

    public byte[] ByteDump { get; }

    public PDUHeader Header { get; }

    public PDU Pdu { get; }

    #endregion
}