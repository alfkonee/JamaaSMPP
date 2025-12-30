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

namespace JamaaTech.Smpp.Net.Lib;

public class SmppException : Exception
{
    #region Variables

    #endregion

    #region Properties

    public SmppErrorCode ErrorCode { get; }

    #endregion

    #region Methods

    internal static void WrapAndThrow(Exception exception)
    {
        var smppEx = new SmppException(SmppErrorCode.ESME_RUNKNOWNERR, exception.Message, exception);
        throw smppEx;
    }

    #endregion

    #region Constructors

    public SmppException(SmppErrorCode errorCode)
    {
        ErrorCode = errorCode;
    }

    public SmppException(SmppErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public SmppException(SmppErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    #endregion
}