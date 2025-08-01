/************************************************************************
 * Copyright (C) 2008 Jamaa Technologies
 *
 * This file is part of Jamaa SMPP Client Library.
 *
 * Jamaa SMPP Client Library is free software. You can redistribute it and/or modify
 * it under the terms of the Microsoft Reciprocal License (Ms-RL)
 *
 * You should have received a copy of the Microsoft Reciprocal License
 * along with Jamaa SMPP Client Library; See License.txt for more details.
 *
 * Author: Benedict J. Tesha
 * benedict.tesha@jamaatech.com, www.jamaatech.com
 *
 ************************************************************************/

using System;

namespace JamaaTech.Smpp.Net.Client;

/// <summary>
/// Provides data for <see cref="SmppClient.ConnectionStateChanged"/> even
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
  #region Variables

  private SmppConnectionState _mNewState;
  private SmppConnectionState _mOldState;
  private int _mReconnectInteval;

  #endregion

  #region Constructors

  /// <summary>
  /// Creates a new instance of <see cref="ConnectionStateChangedEventArgs"/>
  /// </summary>
  /// <param name="newState">The current <see cref="SmppClient"/> connection state</param>
  /// <param name="oldState">The previous <see cref="SmppClient"/> connection state</param>
  public ConnectionStateChangedEventArgs(
    SmppConnectionState newState,
    SmppConnectionState oldState)
  {
    _mNewState = newState;
    _mOldState = oldState;
  }

  /// <summary>
  /// Creates a new instance of <see cref="ConnnectionStateChangedEventArgs"/>
  /// </summary>
  /// <param name="newState">The current <see cref="SmppClient"/> state</param>
  /// <param name="oldState">The previous <see cref="SmppClient"/> state</param>
  /// <param name="reconnectInteval">Reconnect inteval</param>
  public ConnectionStateChangedEventArgs(
    SmppConnectionState newState,
    SmppConnectionState oldState,
    int reconnectInteval)
    : this(newState, oldState)
  {
    _mReconnectInteval = reconnectInteval;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Gets the previous <see cref="SmppClient"/> connection state
  /// </summary>
  public SmppConnectionState PreviousState => _mOldState;

  /// <summary>
  /// Gets the current <see cref="SmppClient"/> connection state
  /// </summary>
  public SmppConnectionState CurrentState => _mNewState;

  /// <summary>
  /// Gets or sets a value indicating the amount of time in miliseconds after which <see cref="SmppClient"/> should attemp to reestablish a lost connection
  /// </summary>
  public int ReconnectInteval
  {
    get => _mReconnectInteval;
    set => _mReconnectInteval = value;
  }

  #endregion
}