/************************************************************************
 * Thread-safer response handler implementation.
 * Removes race between initial fetch and waiter registration,
 * cleans up consumed responses & wait contexts.
 ************************************************************************/
using System.Collections.Generic;
using JamaaTech.Smpp.Net.Lib.Protocol;

namespace JamaaTech.Smpp.Net.Lib
{
    public class ConcurrentResponseHandler : IResponseHandler
    {
        private int vDefaultResponseTimeout;
        private readonly IDictionary<uint, ResponsePDU> _responses = new Dictionary<uint, ResponsePDU>(32);
        private readonly IDictionary<uint, PDUWaitContext> _waiters = new Dictionary<uint, PDUWaitContext>(32);
        private readonly object _responsesLock = new object();
        private readonly object _waitersLock = new object();
        // Minimum enforced timeout (was hard-coded 5000). Made adjustable for testing.
        private static int sMinTimeout = 5000;

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

        public ConcurrentResponseHandler()
        {
            vDefaultResponseTimeout = sMinTimeout; //Default min
        }

        public ConcurrentResponseHandler(ResponseHandlerOptions options)
            : this()
        {
            if (options != null)
            {
                DefaultResponseTimeout = options.DefaultResponseTimeout;
            }
        }

        public int DefaultResponseTimeout
        {
            get => vDefaultResponseTimeout;
            set
            {
                var min = sMinTimeout;
                if (value > min) min = value;
                Interlocked.Exchange(ref vDefaultResponseTimeout, min);
            }
        }

        public int Count
        {
            get
            {
                lock (_responsesLock)
                {
                    return _responses.Count;
                }
            }
        }

        public void Handle(ResponsePDU pdu)
        {
            var seq = pdu.Header.SequenceNumber;

            // Store response first so a late waiter can fetch it.
            lock (_responsesLock)
            {
                _responses[seq] = pdu;
            }

            PDUWaitContext ctx = null;
            lock (_waitersLock)
            {
                if (_waiters.TryGetValue(seq, out ctx))
                {
                    _waiters.Remove(seq);
                }
            }

            if (ctx != null)
            {
                if (ctx.TimedOut)
                {
                    // Remove orphaned response (nobody will consume it).
                    lock (_responsesLock)
                    {
                        _responses.Remove(seq);
                    }
                }
                else
                {
                    ctx.AlertResponseReceived();
                }
            }
        }

        public ResponsePDU WaitResponse(RequestPDU pdu)
            => WaitResponse(pdu, vDefaultResponseTimeout);

        public ResponsePDU WaitResponse(RequestPDU pdu, int timeOut)
        {
            var seq = pdu.Header.SequenceNumber;

            // Fast path
            var existing = Fetch(seq);
            if (existing != null) return existing;

            if (timeOut < sMinTimeout) timeOut = vDefaultResponseTimeout;
            var ctx = new PDUWaitContext(seq, timeOut);

            // Register waiter then re-check to close race
            lock (_waitersLock)
            {
                _waiters[seq] = ctx;
                existing = Fetch(seq);
                if (existing != null)
                {
                    _waiters.Remove(seq);
                    return existing;
                }
            }

            // Await signal or timeout
            ctx.WaitForAlert();

            var resp = Fetch(seq);
            if (resp == null)
            {
                // Ensure removal if timeout path taken
                lock (_waitersLock)
                {
                    _waiters.Remove(seq);
                }
                throw new SmppResponseTimedOutException();
            }
            return resp;
        }

        private ResponsePDU Fetch(uint seq)
        {
            lock (_responsesLock)
            {
                if (_responses.TryGetValue(seq, out var pdu))
                {
                    _responses.Remove(seq); // Consume once
                    return pdu;
                }
                return null;
            }
        }
    }
}
