using System.Collections.Generic;
using System.Threading;
using System;
using JamaaTech.Smpp.Net.Lib.Protocol;
using System.Threading.Tasks;

namespace JamaaTech.Smpp.Net.Lib
{

    public class ResponseHandlerV2 : IResponseHandler
    {
        #region Variables
        private int vDefaultResponseTimeout;
        private IDictionary<uint, ResponsePDU> vResponseQueue;
        private IDictionary<uint, object> vWaitingQueue;
        private readonly object vResponseLock = new object();
        private readonly object vWaitingLock = new object();
        // Minimum enforced timeout (was hard-coded 5000). Made adjustable for testing.
        private static int sMinTimeout = 5000;
        // Small scheduling slack to avoid flakiness under heavy test concurrency
        private const int SchedulingSlackMs = 20;
        #endregion

        #region Testing Helpers
        /// <summary>
        /// Adjust minimum timeout (intended for unit testing). Use cautiously in production.
        /// </summary>
        public static void SetMinimumTimeoutForTesting(int milliseconds)
        {
            if (milliseconds < 1) { milliseconds = 1; }
            Interlocked.Exchange(ref sMinTimeout, milliseconds);
        }
        /// <summary>
        /// Returns current minimum enforced timeout.
        /// </summary>
        public static int GetMinimumTimeoutForTesting() { return sMinTimeout; }
        #endregion

        #region Constructors
        public ResponseHandlerV2()
        {
            vDefaultResponseTimeout = sMinTimeout; //Default min
            vWaitingQueue = new Dictionary<uint, object>(32);
            vResponseQueue = new Dictionary<uint, ResponsePDU>(32);
        }
        #endregion

        #region Properties
        public int DefaultResponseTimeout
        {
            get { return vDefaultResponseTimeout; }
            set
            {
                int timeOut = sMinTimeout;
                if (value > timeOut) { timeOut = value; }
                Interlocked.Exchange(ref vDefaultResponseTimeout, timeOut);
            }
        }
        public int Count
        {
            get { lock (vResponseLock) { return vResponseQueue.Count; } }
        }
        #endregion

        #region Methods
        #region Interface Methods
        public void Handle(ResponsePDU pdu)
        {
            uint sequenceNumber = pdu.Header.SequenceNumber;
            
            // First, add the response to the queue
            lock (vResponseLock)
            {
                vResponseQueue[sequenceNumber] = pdu;
            }
            
            // Then check for waiting contexts and notify them
            lock (vWaitingLock)
            {
                object waitContext;
                if (vWaitingQueue.TryGetValue(sequenceNumber, out waitContext))
                {
                    if (waitContext is PDUWaitContext syncContext)
                    {
                        syncContext.AlertResponseReceived();
                        // Always remove after signaling (success or timeout) to avoid leaks
                        vWaitingQueue.Remove(sequenceNumber);
                    }
                    else if (waitContext is PDUWaitContextAsync asyncContext)
                    {
                        asyncContext.AlertResponseReceived(pdu);
                        // Remove from waiting queue since it's been processed
                        vWaitingQueue.Remove(sequenceNumber);
                    }
                }
            }
        }

        public ResponsePDU WaitResponse(RequestPDU pdu)
        {
            return WaitResponse(pdu, vDefaultResponseTimeout);
        }

        public ResponsePDU WaitResponse(RequestPDU pdu, int timeOut)
        {
            uint sequenceNumber = pdu.Header.SequenceNumber;
            
            // Check if response already exists
            ResponsePDU resp = FetchResponse(sequenceNumber);
            if (resp != null) { return resp; }
            
            if (timeOut < sMinTimeout) { timeOut = vDefaultResponseTimeout; }
            int effectiveTimeout = checked(timeOut + SchedulingSlackMs);
            PDUWaitContext waitContext = new PDUWaitContext(sequenceNumber, effectiveTimeout);
            
            // Register waiter (no nested locks with response lock)
            lock (vWaitingLock)
            {
                vWaitingQueue[sequenceNumber] = waitContext;
            }

            // Double-check after registration without holding waiting lock
            resp = FetchResponse(sequenceNumber);
            if (resp != null)
            {
                // Remove waiter and return immediately
                lock (vWaitingLock)
                {
                    vWaitingQueue.Remove(sequenceNumber);
                }
                return resp;
            }
            
            // Wait for the response
            bool responseReceived = waitContext.WaitForAlert();
            
            // Fetch the response after waiting
            resp = FetchResponse(sequenceNumber);
            
            // Clean up the waiting queue entry if still present
            lock (vWaitingLock)
            {
                vWaitingQueue.Remove(sequenceNumber);
            }

            if (resp == null)
            {
                throw new SmppResponseTimedOutException();
            }
            
            return resp;
        }

        public async Task<ResponsePDU> WaitResponseAsync(RequestPDU pdu, int timeOut, CancellationToken cancellationToken = default)
        {
            uint sequenceNumber = pdu.Header.SequenceNumber;
            
            // Check if response already exists
            ResponsePDU resp = FetchResponse(sequenceNumber);
            if (resp != null) { return resp; }
            
            if (timeOut < sMinTimeout) { timeOut = vDefaultResponseTimeout; }
            int effectiveTimeout = checked(timeOut + SchedulingSlackMs);
            
            var tcs = new TaskCompletionSource<ResponsePDU>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitContext = new PDUWaitContextAsync(sequenceNumber, effectiveTimeout, tcs, cancellationToken);
            
            // Register waiter first
            lock (vWaitingLock)
            {
                vWaitingQueue[sequenceNumber] = waitContext;
            }

            // After registration, re-check without holding waiting lock
            resp = FetchResponse(sequenceNumber);
            if (resp != null)
            {
                // Response already available; clean up and return
                waitContext.Dispose();
                lock (vWaitingLock)
                {
                    vWaitingQueue.Remove(sequenceNumber);
                }
                return resp;
            }
            
            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                // Ensure removal of the waiting entry on completion (success/timeout/cancel)
                lock (vWaitingLock)
                {
                    vWaitingQueue.Remove(sequenceNumber);
                }
            }
        }

        public async Task<ResponsePDU> WaitResponseAsync(RequestPDU pdu, CancellationToken cancellationToken = default)
        {
            return await WaitResponseAsync(pdu, vDefaultResponseTimeout, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region Helper Methods
        private void AddResponse(ResponsePDU pdu)
        {
            lock (vResponseLock)
            {
                vResponseQueue[pdu.Header.SequenceNumber] = pdu;
            }
        }

        private ResponsePDU FetchResponse(uint sequenceNumber)
        {
            lock (vResponseLock)
            {
                ResponsePDU pdu;
                vResponseQueue.TryGetValue(sequenceNumber, out pdu);
                return pdu;
            }
        }

        private void RemoveWaitingQueue(uint sequenceNumber)
        {
            // Callers must hold vWaitingLock
            vWaitingQueue.Remove(sequenceNumber);
        }

        #endregion
        #endregion
    }
}
