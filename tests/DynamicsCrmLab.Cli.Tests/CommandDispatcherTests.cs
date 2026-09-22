using DynamicsCrmLab.Cli;
using DynamicsCrmLab.Cli.Commands;
using FluentAssertions;
using Xunit;

namespace DynamicsCrmLab.Cli.Tests;

public sealed class CommandDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_CarriesTheCommandsOwnAnswerBack()
    {
        var refused = new StubCommand("close", exitCode: 1);
        var dispatcher = new CommandDispatcher([refused]);

        var code = await dispatcher.DispatchAsync(["close", "abc"], CancellationToken.None);

        code.Should().Be(1);
        refused.Ran.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_ReportsSuccessWhenTheCommandSucceeded()
    {
        var dispatcher = new CommandDispatcher([new StubCommand("raise", exitCode: 0)]);

        var code = await dispatcher.DispatchAsync(["raise"], CancellationToken.None);

        code.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_ReportsFailureForACommandItDoesNotKnow()
    {
        var dispatcher = new CommandDispatcher([new StubCommand("raise", exitCode: 0)]);

        var code = await dispatcher.DispatchAsync(["fly"], CancellationToken.None);

        code.Should().Be(1);
    }

    [Fact]
    public async Task DispatchAsync_TreatsNoCommandAtAllAsAskingWhatThereIs()
    {
        var dispatcher = new CommandDispatcher([new StubCommand("raise", exitCode: 0)]);

        var code = await dispatcher.DispatchAsync([], CancellationToken.None);

        code.Should().Be(0);
    }

    private sealed class StubCommand(string name, int exitCode) : ICliCommand
    {
        public bool Ran { get; private set; }

        public string Name => name;

        public string Usage => name;

        public Task<int> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            Ran = true;

            return Task.FromResult(exitCode);
        }
    }
}
