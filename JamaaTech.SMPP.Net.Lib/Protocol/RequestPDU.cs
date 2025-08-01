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

public abstract class RequestPDU : PDU
{
  #region Constructors

  internal RequestPDU(PDUHeader header, SmppEncodingService smppEncodingService)
    : base(header, smppEncodingService)
  {
  }

  #endregion

  #region Properties

  public virtual bool HasResponse => true;

  #endregion

  #region Methods

  public abstract ResponsePDU CreateDefaultResponse();

  #endregion
}