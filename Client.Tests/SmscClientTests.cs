using JamaaTech.Smpp.Net.Lib;
using JamaaTech.Smpp.Net.Lib.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Elyfe.Smpp.Client.Tests;

public class SmscClientTests
{
    private readonly Mock<ILogger<SmscClient>> _mockLogger;
    private readonly IOptions<SmscOptions> _options;
    private readonly Mock<SmppClientSession> _mockSession;
    private readonly SmppEncodingService _encodingService;

    private class TestableSmscClient : SmscClient
    {
        private readonly Mock<SmppClientSession> _mockSession;

        public TestableSmscClient(
            ILogger<SmscClient> logger,
            IOptions<SmscOptions> options,
            Mock<SmppClientSession> mockSession) : base(logger, options)
        {
            _mockSession = mockSession;
        }

        protected override SmppClientSession CreateAndBindSession(SessionBindInfo bindInfo, SmppEncodingService encodingService)
        {
            return _mockSession.Object;
        }
    }

    public SmscClientTests()
    {
        _mockLogger = new Mock<ILogger<SmscClient>>();
        _encodingService = new SmppEncodingService();
        _options = Options.Create(new SmscOptions
        {
            Host = "localhost",
            Port = 2775,
            SystemId = "test",
            Password = "test",
            Reconnect = false
        });

        _mockSession = new Mock<SmppClientSession>(_encodingService);
    }

    [Fact]
    public void InitialState_ShouldBeClosed()
    {
        var client = new TestableSmscClient(_mockLogger.Object, _options, _mockSession);
        Assert.Equal(SmppConnectionState.Closed, client.ConnectionState);
    }

    [Fact]
    public async Task ConnectAsync_WhenClosed_ShouldTransitionToBound()
    {
        var client = new TestableSmscClient(_mockLogger.Object, _options, _mockSession);
        var states = new List<SmppConnectionState>();
        client.ConnectionStateChanged += (_, e) => states.Add(e.NewState);

        _mockSession.Setup(s => s.State).Returns(SmppSessionState.Open);

        await client.ConnectAsync();

        Assert.Equal(SmppConnectionState.Bound, client.ConnectionState);
        Assert.Equal(new[] { SmppConnectionState.Connecting, SmppConnectionState.Bound }, states);
    }

    [Fact]
    public async Task DisconnectAsync_WhenBound_ShouldTransitionToClosed()
    {
        var client = new TestableSmscClient(_mockLogger.Object, _options, _mockSession);
        
        _mockSession.Setup(s => s.State).Returns(SmppSessionState.Open);
        await client.ConnectAsync();

        var states = new List<SmppConnectionState>();
        client.ConnectionStateChanged += (_, e) => states.Add(e.NewState);

        await client.DisconnectAsync();
        _mockSession.Raise(s => s.SessionClosed += null, new SmppSessionClosedEventArgs(SmppSessionCloseReason.EndSessionCalled, null));

        Assert.Equal(SmppConnectionState.Closed, client.ConnectionState);
        Assert.Contains(SmppConnectionState.Unbinding, states);
        Assert.Contains(SmppConnectionState.Closed, states);
    }

    [Fact]
    public async Task SendSmsAsync_WhenNotBound_ShouldThrowInvalidOperationException()
    {
        var client = new TestableSmscClient(_mockLogger.Object, _options, _mockSession);
        var pdu = new SubmitSm(_encodingService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendSingleSmsAsync(pdu));
    }

    [Fact]
    public async Task SendSmsAsync_WhenBound_ShouldCallSessionSendPdu()
    {
        var client = new TestableSmscClient(_mockLogger.Object, _options, _mockSession);
        _mockSession.Setup(s => s.State).Returns(SmppSessionState.Open);
        await client.ConnectAsync();

        var pdu = new SubmitSm(_encodingService);
        var expectedResponse = pdu.CreateDefaultResponse();
        _mockSession.Setup(s => s.SendPdu(pdu)).Returns(expectedResponse);

        var response = await client.SendSingleSmsAsync(pdu);

        _mockSession.Verify(s => s.SendPdu(pdu), Times.Once);
        Assert.Same(expectedResponse, response);
    }

    [Fact]
    public async Task OnPduReceived_WithDeliverSm_ShouldRaiseDeliverSmReceivedEvent()
    {
        var client = new TestableSmscClient(_mockLogger.Object, _options, _mockSession);
        _mockSession.Setup(s => s.State).Returns(SmppSessionState.Open);
        await client.ConnectAsync();

        DeliverSmEventArgs? receivedArgs = null;
        client.DeliverSmReceived += (_, e) => { receivedArgs = e; };

        var deliverSmPdu = new DeliverSm(_encodingService);
        var pduReceivedEventArgs = new PduReceivedEventArgs(deliverSmPdu);

        _mockSession.Raise(s => s.PduReceived += null, pduReceivedEventArgs);

        Assert.NotNull(receivedArgs);
        Assert.Same(deliverSmPdu, receivedArgs.Pdu);
    }
}
