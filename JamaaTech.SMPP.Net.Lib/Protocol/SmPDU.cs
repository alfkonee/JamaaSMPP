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

public abstract class SmPDU : RequestPDU
{
  #region Variables

  protected SmppAddress _sourceAddress;

  #endregion

  #region Constructors

  internal SmPDU(PDUHeader header, SmppEncodingService smppEncodingService, SmppAddress srcAddress = null)
    : base(header, smppEncodingService)
  {
    _sourceAddress = srcAddress ?? new SmppAddress();
  }

  #endregion

  #region Properties

  public SmppAddress SourceAddress => _sourceAddress;

  #endregion
}