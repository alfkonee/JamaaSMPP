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

public class EnquireLink : GenericRequestPDU
{
  #region Constuctors

  internal EnquireLink(PDUHeader header, SmppEncodingService smppEncodingService)
    : base(header, smppEncodingService)
  {
  }

  public EnquireLink(SmppEncodingService smppEncodingService)
    : base(new PDUHeader(CommandType.EnquireLink), smppEncodingService)
  {
  }

  #endregion

  #region Properties

  public override SmppEntityType AllowedSource => SmppEntityType.Any;

  public override SmppSessionState AllowedSession => SmppSessionState.Transceiver;

  #endregion

  #region Methods

  public override ResponsePDU CreateDefaultResponse()
  {
    var header = new PDUHeader(CommandType.EnquireLinkResp, vHeader.SequenceNumber);
    //use default Status and Length
    //header.CommandStatus = 0;
    //header.CommandLength = 16;
    var resp = (EnquireLinkResp)CreatePDU(header, vSmppEncodingService);
    return resp;
  }

  #endregion
}