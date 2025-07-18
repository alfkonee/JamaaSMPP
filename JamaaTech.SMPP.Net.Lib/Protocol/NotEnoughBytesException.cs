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

namespace JamaaTech.Smpp.Net.Lib.Protocol;

public class NotEnoughBytesException : PDUException
{
  #region Constructors

  public NotEnoughBytesException(string message)
    : base(SmppErrorCode.ESME_RINVCMDLEN, message)
  {
  }

  public NotEnoughBytesException(string messager, Exception innerException)
    : base(SmppErrorCode.ESME_RINVCMDLEN, messager, innerException)
  {
  }

  #endregion
}