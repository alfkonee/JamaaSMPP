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
using JamaaTech.Smpp.Net.Lib;
using JamaaTech.Smpp.Net.Lib.Protocol;

namespace JamaaTech.Smpp.Net.Client;

/// <summary>
/// Represents SMPP connection properties
/// </summary>
[Serializable()]
public class SmppConnectionProperties
{
  #region Variables
  private readonly object _vSyncRoot;
  #endregion

  #region Constructors

  /// <summary>
  /// Initializes a new instance of <see cref="SmppConnectionProperties"/>
  /// </summary>
  public SmppConnectionProperties()
  {
    SystemId = "";
    Password = "";
    Host = "";
    AddressTon = TypeOfNumber.International;
    AddressNpi = NumberingPlanIndicator.ISDN;
    InterfaceVersion = InterfaceVersion.v34;
    SystemType = "";
    DefaultServiceType = "";
    SmscId = "";
    _vSyncRoot = new object();
    AllowReceive = true;
    AllowTransmit = true;
  }

  #endregion

  #region Properties

  /// <summary>
  /// Gets or sets the system id that identifies this client to the SMPP server
  /// </summary>
  public string SystemId { get; set; }

  /// <summary>
  /// Gets or sets the password for authenticating the client to the SMPP server
  /// </summary>
  public string Password { get; set; }

  /// <summary>
  /// Gets or sets host name or IP address of the remote host
  /// </summary>
  public string Host { get; set; }

  /// <summary>
  /// Gets or sets the TCP/IP Protocol port number
  /// </summary>
  public int Port { get; set; }

  /// <summary>
  /// Gets or sets the default SMPP interface version to be used
  /// </summary>
  public InterfaceVersion InterfaceVersion { get; set; }

  /// <summary>
  /// Gets or sets the Numbering Plan Indicator (NPI)
  /// </summary>
  public NumberingPlanIndicator AddressNpi { get; set; }

  /// <summary>
  /// Gets or sets the type of number
  /// </summary>
  public TypeOfNumber AddressTon { get; set; }

  /// <summary>
  /// Gets or sets the default encoding to be used when sending messages
  /// </summary>
  public DataCoding DefaultEncoding { get; set; }

  /// <summary>
  /// Gets or sets the defalt SMPP service type
  /// </summary>
  public string DefaultServiceType { get; set; }

  /// <summary>
  /// Gets or sets SMPP service type
  /// </summary>
  public string SystemType { get; set; }

  /// <summary>
  /// Gets the ID or the Short Message Service Center (SMSC)
  /// </summary>
  public string SmscId { internal set; get; }

  /// <summary>
  /// Gets an object that can be used for locking in a multi-threaded environment
  /// </summary>
  public object SyncRoot => _vSyncRoot;

  /// <summary>
  /// Gets or sets the default source address when sending messages
  /// </summary>
  public string SourceAddress { get; set; }

  /// <summary>
  /// Gets or sets UseSeparateConnections
  /// When null: Depends on <see cref="InterfaceVersion"/>, if <see cref="InterfaceVersion.v33"/> true, <see cref="InterfaceVersion.v34"/> false.
  /// When true: Use two sessions for Receiver (<see cref="CommandType.BindReceiver"/>) and Transmitter (<see cref="CommandType.BindTransmitter"/>)
  /// When false: Use one session for Receiver and Transmitter in mode <see cref="CommandType.BindTransceiver"/>
  /// </summary>
  public bool? UseSeparateConnections { get; set; }

  /// <summary>
  /// <see cref="UseSeparateConnections"/>
  /// </summary>
  public bool CanSeparateConnections => UseSeparateConnections == true || InterfaceVersion == InterfaceVersion.v33;

  public bool AllowReceive { get; set; }

  public bool AllowTransmit { get; set; }

  #endregion

  #region Methods

  internal SessionBindInfo GetBindInfo()
  {
    var bindInfo = new SessionBindInfo
    {
      SystemID = SystemId,
      Password = Password,
      ServerName = Host,
      Port = Port,
      InterfaceVersion = InterfaceVersion,
      AddressTon = AddressTon,
      AddressNpi = AddressNpi,
      SystemType = SystemType,
      AllowReceive = AllowReceive,
      AllowTransmit = AllowTransmit
    };
    return bindInfo;
  }

  #endregion
}