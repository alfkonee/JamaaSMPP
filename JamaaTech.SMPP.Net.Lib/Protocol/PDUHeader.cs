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

public sealed class PDUHeader
{
  #region Variables

  private CommandType vCommandType;
  private uint vCommandLength;
  private SmppErrorCode vErrorCode;
  private uint vSequenceNumber;

  private static uint vNextSequenceNumber;
  private static object vSyncRoot;

  #endregion

  #region Constructors

  static PDUHeader()
  {
    vSyncRoot = new object();
    vNextSequenceNumber = 1;
  }

  public PDUHeader(CommandType cmdType)
    : this(cmdType, GetNextSequenceNumber())
  {
    vCommandLength = 16;
  }

  public PDUHeader(CommandType cmdType, uint seqNumber)
  {
    vCommandType = cmdType;
    vSequenceNumber = seqNumber;
    vCommandLength = 16;
  }

  public PDUHeader(CommandType cmdType, uint seqNumber, SmppErrorCode errorCode)
    : this(cmdType, seqNumber)
  {
    vErrorCode = errorCode;
    vCommandLength = 16;
  }

  public PDUHeader(CommandType cmdType, uint seqNumber, SmppErrorCode errorCode, uint cmdLength)
    : this(cmdType, seqNumber, errorCode)
  {
    vCommandLength = cmdLength;
  }

  #endregion

  #region Properties

  public CommandType CommandType => vCommandType;

  public uint CommandLength
  {
    get => vCommandLength;
    set => vCommandLength = value;
  }

  public SmppErrorCode ErrorCode
  {
    get => vErrorCode;
    set => vErrorCode = value;
  }

  public uint SequenceNumber => vSequenceNumber;

  #endregion

  #region Methods

  public static PDUHeader Parse(ByteBuffer buffer, SmppEncodingService smppEncodingService)
  {
    if (buffer == null) throw new ArgumentNullException("buffer");
    if (buffer.Length < 16) throw new ArgumentException("Buffer length must not be less than 16 bytes");
    var cmdLength = smppEncodingService.GetIntFromBytes(buffer.Remove(4));
    var cmdType = (CommandType)smppEncodingService.GetIntFromBytes(buffer.Remove(4));
    var errorCode = (SmppErrorCode)smppEncodingService.GetIntFromBytes(buffer.Remove(4));
    var seqNumber = smppEncodingService.GetIntFromBytes(buffer.Remove(4));
    var header = new PDUHeader(cmdType, seqNumber, errorCode, cmdLength);
    return header;
  }

  public byte[] GetBytes(SmppEncodingService smppEncodingService)
  {
    var buffer = new ByteBuffer(32);
    buffer.Append(smppEncodingService.GetBytesFromInt(vCommandLength));
    buffer.Append(smppEncodingService.GetBytesFromInt((uint)vCommandType));
    buffer.Append(smppEncodingService.GetBytesFromInt((uint)vErrorCode));
    buffer.Append(smppEncodingService.GetBytesFromInt(vSequenceNumber));
    return buffer.ToBytes();
  }

  public static uint GetNextSequenceNumber()
  {
    lock (vSyncRoot)
    {
      var seqNumber = vNextSequenceNumber;
      if (vNextSequenceNumber == uint.MaxValue)
        vNextSequenceNumber = 1;
      else
        vNextSequenceNumber++;
      return seqNumber;
    }
  }

  public override string ToString()
  {
    return vCommandType.ToString();
  }

  #endregion
}