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

using System.Net;
using System.Net.Sockets;

namespace JamaaTech.Smpp.Net.Lib.Networking;

public class TcpIpSessionProperties
{
  #region Variables

  private Socket vSocket;

  #endregion

  #region Constructors

  internal TcpIpSessionProperties(Socket socket)
  {
    vSocket = socket;
  }

  #endregion

  #region Properties

  public LingerOption LingerState
  {
    get => vSocket.LingerState;
    set => vSocket.LingerState = value;
  }

  public bool NoDelay
  {
    get => vSocket.NoDelay;
    set => vSocket.NoDelay = value;
  }

  public int ReceiveTimeout
  {
    get => vSocket.ReceiveTimeout;
    set => vSocket.ReceiveTimeout = value;
  }

  public int ReceiveBufferSize
  {
    get => vSocket.ReceiveBufferSize;
    set => vSocket.ReceiveBufferSize = value;
  }

  public int SendTimeout
  {
    get => vSocket.SendTimeout;
    set => vSocket.SendTimeout = value;
  }

  public int SendBufferSize
  {
    get => vSocket.SendBufferSize;
    set => vSocket.SendBufferSize = value;
  }

  public IPEndPoint LocalEndPoint => (IPEndPoint)vSocket.LocalEndPoint;

  public IPEndPoint RemoteEndPoint => (IPEndPoint)vSocket.RemoteEndPoint;

  #endregion
}