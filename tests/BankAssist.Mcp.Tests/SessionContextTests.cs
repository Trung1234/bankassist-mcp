namespace BankAssist.Mcp.Tests;

using BankAssist.Mcp.Masking;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[Trait("Category", "Session")]
public sealed class SessionContextTests
{
    [Fact(DisplayName = "T-11: có header X-Session-Id và X-Rm-Id - dùng đúng giá trị host truyền")]
    public void FromHttpContext_WithHeaders_UsesThem()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[SessionContextFactory.SessionIdHeader] = "s-8f2a";
        http.Request.Headers[SessionContextFactory.RmIdHeader] = "RM007";

        var session = SessionContextFactory.FromHttpContext(http, NullLogger.Instance);

        session.SessionId.Should().Be("s-8f2a");
        session.RmId.Should().Be("RM007");
        session.IsSessionIdFromHeader.Should().BeTrue();
    }

    [Fact(DisplayName = "T-08: thiếu header X-Session-Id - vẫn chạy nhưng ghi cảnh báo")]
    public void FromHttpContext_WithoutSessionHeader_GeneratesIdAndWarns()
    {
        var logger = new RecordingLogger();

        var session = SessionContextFactory.FromHttpContext(new DefaultHttpContext(), logger);

        session.SessionId.Should().NotBeNullOrWhiteSpace();
        session.IsSessionIdFromHeader.Should().BeFalse();
        logger.Warnings.Should().ContainSingle();
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel is LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
