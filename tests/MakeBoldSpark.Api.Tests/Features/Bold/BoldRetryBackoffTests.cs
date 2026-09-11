using System.Threading.Channels;
using MakeBoldSpark.Api.Features.Bold;
using MakeBoldSpark.Api.Features.Bold.Completions;
using MakeBoldSpark.Api.Features.Bold.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldRetryBackoffTests
{
    private static CompletionService Create(StubProvider provider, CompletionRetryDelay delay, int retries = 2)
    {
        var options = Options.Create(new BoldOptions { ProviderCall = new() { MaxRetries = retries } });
        return new CompletionService([provider], options, new CompletionRequestValidator(options),
            NullLogger<CompletionService>.Instance, delay);
    }

    private static CompletionRequestDto Request => new("router", null,
        [new("user", "Fictional backoff example")], null, null, null, null, null);

    [TestMethod]
    public async Task TransientRetries_WaitBeforeSendingTheNextAttempt()
    {
        var provider = new StubProvider { Failures = 2 };
        var delay = new ControlledDelay();
        var execution = Create(provider, delay).ExecuteAsync(Request, CancellationToken.None);
        var first = await delay.Pending.Reader.ReadAsync();
        Assert.AreEqual(TimeSpan.FromMilliseconds(250), first.Duration);
        Assert.AreEqual(1, provider.Calls);
        Assert.IsFalse(execution.IsCompleted);
        first.Release.SetResult();
        var second = await delay.Pending.Reader.ReadAsync();
        Assert.AreEqual(TimeSpan.FromMilliseconds(500), second.Duration);
        Assert.AreEqual(2, provider.Calls);
        second.Release.SetResult();
        Assert.IsTrue((await execution).Success);
        Assert.AreEqual(3, provider.Calls);
    }

    [TestMethod]
    public async Task CancellationDuringBackoff_PreventsAnotherProviderCall()
    {
        var provider = new StubProvider { Failures = 2 };
        var delay = new ControlledDelay();
        using var cancellation = new CancellationTokenSource();
        var execution = Create(provider, delay).ExecuteAsync(Request, cancellation.Token);
        await delay.Pending.Reader.ReadAsync();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await execution);
        Assert.AreEqual(1, provider.Calls);
    }

    [TestMethod]
    public async Task Backoff_IsBoundedAndHardErrorsAreNotRetried()
    {
        var delay = new RecordingDelay();
        var provider = new StubProvider { Failures = 20 };
        var outcome = await Create(provider, delay, 8).ExecuteAsync(Request, CancellationToken.None);
        Assert.IsFalse(outcome.Success);
        CollectionAssert.AreEqual(new double[] { 250, 500, 1000, 2000, 4000, 5000, 5000, 5000 }, delay.Milliseconds);
        Assert.AreEqual(9, provider.Calls);
        delay.Milliseconds.Clear();
        provider = new StubProvider { Failures = 20, Retryable = false };
        outcome = await Create(provider, delay).ExecuteAsync(Request, CancellationToken.None);
        Assert.IsFalse(outcome.Success);
        Assert.AreEqual(1, provider.Calls);
        Assert.HasCount(0, delay.Milliseconds);
    }

    private sealed class ControlledDelay : CompletionRetryDelay
    {
        public Channel<(TimeSpan Duration, TaskCompletionSource Release)> Pending { get; } =
            Channel.CreateUnbounded<(TimeSpan, TaskCompletionSource)>();
        public override Task WaitAsync(int retry, CancellationToken cancellationToken)
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Pending.Writer.TryWrite((GetDuration(retry), release));
            return release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class RecordingDelay : CompletionRetryDelay
    {
        public List<double> Milliseconds { get; } = [];
        public override Task WaitAsync(int retry, CancellationToken cancellationToken)
        {
            Milliseconds.Add(GetDuration(retry).TotalMilliseconds);
            return Task.CompletedTask;
        }
    }

    private sealed class StubProvider : IProviderClient
    {
        public string Provider => "openai";
        public int Calls { get; private set; }
        public int Failures { get; init; }
        public bool Retryable { get; init; } = true;
        public Task<ProviderCompletionResult> CompleteAsync(ProviderCompletionRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls <= Failures) throw new BoldProviderException("provider_unavailable", "Fictional outage", Retryable);
            return Task.FromResult(new ProviderCompletionResult("fictional", 1, 1, "fictional"));
        }
        public Task<ProviderHealth> CheckHealthAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ProviderHealth(true, 0, null));
    }
}
