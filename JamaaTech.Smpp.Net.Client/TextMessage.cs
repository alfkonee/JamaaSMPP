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
using System.Collections.Generic;
using JamaaTech.Smpp.Net.Lib.Protocol;
using JamaaTech.Smpp.Net.Lib;

namespace JamaaTech.Smpp.Net.Client
{
    public class TextMessage : ShortMessage
    {
        #region Variables

        private string vText;
        private int vMaxMessageLength;

        #endregion

        #region Constuctors

        /// <summary>
        /// Initializes a new instance of <see cref="TextMessage"/>
        /// </summary>
        public TextMessage()
            : base()
        {
            vText = "";
        }

        internal TextMessage(int segmentId, int messageCount, int sequenceNumber)
            : base(segmentId, messageCount, sequenceNumber)
        {
            vText = "";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a <see cref="System.String"/> value representing the text content of the message
        /// </summary>
        public string Text
        {
            get { return vText; }
            set { vText = value; }
        }

        public int MaxMessageLength
        {
            get { return vMaxMessageLength; }
        }

        #endregion

        #region Methods

        protected override IEnumerable<SendSmPDU> GetPDUs(DataCoding defaultEncoding,
            SmppEncodingService smppEncodingService, SmppAddress destAddress = null, SmppAddress srcAddress = null)
        {
            destAddress ??= new SmppAddress() { Address = vDestinatinoAddress };
            srcAddress ??= new SmppAddress() { Address = vSourceAddress };

            vMaxMessageLength = GetMaxMessageLength(defaultEncoding, false);
            byte[] bytes = smppEncodingService.GetBytesFromString(vText, defaultEncoding);

            // Unicode encoding return 2 items for 1 char 
            // We check vText Length first
            if (vText.Length > vMaxMessageLength && bytes.Length > vMaxMessageLength) // Split into multiple!
            {
                var segId = new Random().Next(1000, 9999); // create random SegmentID
                vMaxMessageLength = GetMaxMessageLength(defaultEncoding, true);
                var messages = Split(vText, vMaxMessageLength);
                var totalSegments = messages.Count; // get the number of (how many) parts

                for (int i = 0; i < totalSegments; i++)
                {
                    SubmitSm sm = CreateSubmitSm(smppEncodingService, destAddress, srcAddress);
                    sm.DataCoding = defaultEncoding;
                    if (SubmitUserMessageReference)
                        sm.SetOptionalParamString(Lib.Protocol.Tlv.Tag.user_message_reference, UserMessageReference);

                    if (vRegisterDeliveryNotification)
                        sm.RegisteredDelivery = RegisteredDelivery.DeliveryReceipt;
                    var udh = new Udh(segId, totalSegments, i+1); // ID, Total, part
                    sm.SetMessageText(messages[i], defaultEncoding, udh); // send parts of the message + all other UDH settings
                    yield return sm;
                }
            }
            else
            {
                SubmitSm sm = CreateSubmitSm(smppEncodingService, destAddress, srcAddress);
                sm.DataCoding = defaultEncoding;
                if (SubmitUserMessageReference)
                    sm.SetOptionalParamString(Lib.Protocol.Tlv.Tag.user_message_reference, UserMessageReference);

                if (vRegisterDeliveryNotification)
                    sm.RegisteredDelivery = RegisteredDelivery.DeliveryReceipt;
                sm.SetMessageBytes(bytes);
                yield return sm;
            }
        }

        protected virtual SubmitSm CreateSubmitSm(SmppEncodingService smppEncodingService,
            SmppAddress destAddress = null, SmppAddress srcAddress = null)
        {
            var sm = new SubmitSm(smppEncodingService, destAddress, srcAddress);
            return sm;
        }

        private static List<String> Split(string message, int maxPartLength)
        {
            var result = new List<String>();

            for (int i = 0; i < message.Length; i += maxPartLength)
            {
                var chunkSize = i + maxPartLength < message.Length ? maxPartLength : message.Length - i;
                var chunk = new char[chunkSize];
                message.CopyTo(i, chunk, 0, chunkSize);
                result.Add(new string(chunk));
            }

            return result;
        }

        private static int GetMaxMessageLength(DataCoding encoding, bool includeUdh)
        {
            switch (encoding)
            {
                case DataCoding.SMSCDefault:
                    return includeUdh ? 153 : 160;
                case DataCoding.Latin1:
                    return includeUdh ? 134 : 140;
                case DataCoding.ASCII:
                    return includeUdh ? 153 : 160;
                case DataCoding.UCS2:
                    return includeUdh ? 67 : 70;
                default:
                    throw new InvalidOperationException("Invalid or unsupported encoding for text message ");
            }
        }

        #endregion

        #region Overriden System.Object Members

        public override string ToString()
        {
            return vText == null ? "" : vText;
        }

        #endregion
    }
}