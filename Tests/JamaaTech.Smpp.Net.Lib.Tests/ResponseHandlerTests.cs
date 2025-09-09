using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JamaaTech.Smpp.Net.Lib;
using JamaaTech.Smpp.Net.Lib.Testing;
using JamaaTech.Smpp.Net.Lib.Protocol;
using Xunit;

namespace JamaaTech.Smpp.Net.Lib.Tests
{
    public class ResponseHandlerTests : IDisposable
    {
        private readonly int _originalMin;
        public ResponseHandlerTests()
        {
            _originalMin = ResponseHandler.GetMinimumTimeoutForTesting();
            ResponseHandler.SetMinimumTimeoutForTesting(30); // shrink min for fast tests
        }
        public void Dispose()
        {
            ResponseHandler.SetMinimumTimeoutForTesting(_originalMin);
        }

        private static int GetWaitingQueueCount(ResponseHandler h)
        {
            var f = typeof(ResponseHandler).GetField("vWaitingQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            var dict = (System.Collections.IDictionary)f.GetValue(h);
            return dict.Count;
        }
        private static int GetResponseQueueCount(ResponseHandler h)
        {
            var f = typeof(ResponseHandler).GetField("vResponseQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            var dict = (System.Collections.IDictionary)f.GetValue(h);
            return dict.Count;
        }

        [Fact]
        public void Constructor_SetsDefaults()
        {
            var handler = new ResponseHandler();
            Assert.Equal(ResponseHandler.GetMinimumTimeoutForTesting(), handler.DefaultResponseTimeout);
            Assert.Equal(0, handler.Count);
        }

        [Fact]
        public void DefaultResponseTimeout_ClampToMinimum()
        {
            var handler = new ResponseHandler();
            handler.DefaultResponseTimeout = 5; // below min 30
            Assert.Equal(30, handler.DefaultResponseTimeout);
        }

        [Fact]
        public void DefaultResponseTimeout_SetHigherValue()
        {
            var handler = new ResponseHandler();
            handler.DefaultResponseTimeout = 120;
            Assert.Equal(120, handler.DefaultResponseTimeout);
        }

        [Fact]
        public void Handle_AddsResponse_WhenNoWaitingContext()
        {
            var handler = new ResponseHandler();
            uint seq = 0xABCDEF;
            var resp = new TestResponsePDU(seq);

            handler.Handle(resp);

            Assert.Equal(1, handler.Count);

            var req = new TestRequestPDU(seq);
            var sw = Stopwatch.StartNew();
            var returned = handler.WaitResponse(req);
            sw.Stop();

            Assert.Same(resp, returned);
            Assert.True(sw.ElapsedMilliseconds < 50);
        }

        [Fact]
        public void WaitResponse_BlocksUntilResponseArrives()
        {
            var handler = new ResponseHandler();
            uint seq = 0x1234;
            var req = new TestRequestPDU(seq);
            var resp = new TestResponsePDU(seq);

            var sw = Stopwatch.StartNew();
            var waitTask = Task.Run(() => handler.WaitResponse(req));

            Task.Delay(25).Wait();
            handler.Handle(resp);

            var result = waitTask.Result;
            sw.Stop();

            Assert.Same(resp, result);
            Assert.True(sw.ElapsedMilliseconds >= 20);
            Assert.True(sw.ElapsedMilliseconds < 500);
        }

        [Fact]
        public void WaitResponse_ThrowsOnTimeout_UsingDefault()
        {
            var handler = new ResponseHandler();
            uint seq = 0x2222;
            var req = new TestRequestPDU(seq);

            var sw = Stopwatch.StartNew();
            Assert.Throws<SmppResponseTimedOutException>(() => handler.WaitResponse(req));
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds >= 25, $"Elapsed {sw.ElapsedMilliseconds}");
            Assert.True(sw.ElapsedMilliseconds < 500);
            // waiting queue leak after timeout (issue demonstration)
            Assert.Equal(1, GetWaitingQueueCount(handler));
        }

        [Fact]
        public void WaitResponse_TimeoutParameterBelowMinimum_UsesDefaultTimeout()
        {
            var handler = new ResponseHandler();
            handler.DefaultResponseTimeout = 90; // > min (30)
            uint seq = 0x3333;
            var req = new TestRequestPDU(seq);

            var sw = Stopwatch.StartNew();
            Assert.Throws<SmppResponseTimedOutException>(() => handler.WaitResponse(req, 5)); // 5 < min => use default (90)
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds >= 80);
            Assert.True(sw.ElapsedMilliseconds < 800);
        }

        [Fact]
        public void Handle_ResponseArrivesAfterWaiterTimedOut_ExecutesTimedOutBranch()
        {
            var handler = new ResponseHandler();
            uint seq = 0x4444;
            var req = new TestRequestPDU(seq);
            var resp = new TestResponsePDU(seq);

            Assert.Throws<SmppResponseTimedOutException>(() => handler.WaitResponse(req));
            int waitingAfterTimeout = GetWaitingQueueCount(handler); // leak present

            handler.Handle(resp); // triggers TimedOut branch path

            var fetched = handler.WaitResponse(new TestRequestPDU(seq));
            Assert.Same(resp, fetched);
            Assert.Equal(waitingAfterTimeout, GetWaitingQueueCount(handler)); // still leaked
        }

        [Fact]
        public void ResponseNotRemoved_AllowsSecondWaitResponse_ReturningSameObject()
        {
            var handler = new ResponseHandler();
            uint seq = 0x5555;
            var resp = new TestResponsePDU(seq);
            handler.Handle(resp);
            var first = handler.WaitResponse(new TestRequestPDU(seq));
            var second = handler.WaitResponse(new TestRequestPDU(seq)); // same cached instance
            Assert.Same(resp, first);
            Assert.Same(first, second); // demonstrates stale retention (issue)
            Assert.Equal(1, GetResponseQueueCount(handler));
        }

        [Fact]
        public void Concurrency_WaitAndHandle_ManySequences()
        {
            int original = ResponseHandler.GetMinimumTimeoutForTesting();
            ResponseHandler.SetMinimumTimeoutForTesting(500);
            try
            {
                var handler = new ResponseHandler();
                int n = 50;
                var seqs = Enumerable.Range(1, n).Select(i => (uint)i).ToArray();
                var tasks = seqs.Select(s => Task.Run(() => handler.WaitResponse(new TestRequestPDU(s)))).ToArray();
                // deliver in reverse order
                foreach (var s in seqs.Reverse())
                {
                    handler.Handle(new TestResponsePDU(s));
                }
                Task.WaitAll(tasks);
                Assert.Equal(n, handler.Count); // all responses cached (also indicates not removed)
            }
            finally
            {
                ResponseHandler.SetMinimumTimeoutForTesting(original);
            }
        }

        [Fact]
        public void StaticMinTimeoutChange_AffectsNewInstancesButNotExistingValueUnlessPropertySet()
        {
            ResponseHandler.SetMinimumTimeoutForTesting(10);
            var a = new ResponseHandler();
            Assert.Equal(10, a.DefaultResponseTimeout);
            ResponseHandler.SetMinimumTimeoutForTesting(100);
            // existing instance still has old default
            Assert.Equal(10, a.DefaultResponseTimeout);
            // setting below new min clamps to 100
            a.DefaultResponseTimeout = 20; // 20 < new min 100
            Assert.Equal(100, a.DefaultResponseTimeout); // shows global side effect (issue)
        }
    }
}
