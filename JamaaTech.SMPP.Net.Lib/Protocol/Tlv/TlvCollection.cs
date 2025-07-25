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

namespace JamaaTech.Smpp.Net.Lib.Protocol.Tlv;

public class TlvCollection : List<Tlv>
{
  #region Methods

  public byte[] GetBytes(SmppEncodingService smppEncodingService)
  {
    var buffer = new ByteBuffer(64); //Creates buffer with enough capacity
    foreach (var tlv in this) buffer.Append(tlv.GetBytes(smppEncodingService));
    return buffer.ToBytes();
  }

  public static TlvCollection Parse(ByteBuffer buffer, SmppEncodingService smppEncodingService)
  {
    if (buffer == null) throw new ArgumentNullException("buffer");
    var tlvs = new TlvCollection();
    while (buffer.Length > 0)
    {
      var tlv = Tlv.Parse(buffer, smppEncodingService);
      tlvs.Add(tlv);
    }

    return tlvs;
  }

  public Tlv GetTlvByTag(Tag tag)
  {
    foreach (var tlv in this)
      if (tlv.Tag == tag)
        return tlv;
    return null;
  }

  #endregion
}