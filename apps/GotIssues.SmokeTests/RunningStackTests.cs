using System.Net;
using GotIssues.SmokeTests.Infrastructure;

namespace GotIssues.SmokeTests;

/// <summary>
/// One cold start, shared by the criteria that need a healthy stack: AC1, the
/// attribution rule, and AC6's token validation. Standing the stack up is the expensive
/// part, so it is done once — but nothing here depends on execution order, and the
/// attribution test restores what it stops.
/// </summary>
public sealed class RunningStackFixture : IAsyncLifetime
{
    public ComposeProject Stack { get; } = new(ComposeProject.UniqueName("running"));

    public CommandResult UpResult { get; private set; } = new(-1, string.Empty, "not started");

    public async Task InitializeAsync()
    {
        (await Stack.BuildAsync()).EnsureSucceeded("docker compose build");
        UpResult = await Stack.UpAsync();
    }

    public async Task DisposeAsync() => await Stack.DisposeAsync();
}

[Collection(SerialExecution.Name)]
public sealed class RunningStackTests(RunningStackFixture fixture)
    : IClassFixture<RunningStackFixture>
{
    [Fact]
    public async Task AC1_a_cold_start_on_an_empty_volume_brings_every_service_up_healthy()
    {
        // `up --wait` returns non-zero if a service never reaches its declared condition,
        // so this is an assertion and not a sleep.
        fixture.UpResult.EnsureSucceeded("docker compose up --wait (cold start)");

        await StackCheck.AssertStackHealthyAsync(fixture.Stack);

        // Health is connectivity, not schema: the API reports healthy against an empty
        // database. AC4 proved that gap by neutering the migration step and watching
        // this test pass anyway.
        await StackCheck.AssertSchemaMigratedAsync(fixture.Stack);
    }

    [Fact]
    public async Task The_health_endpoint_answering_us_belongs_to_this_stack()
    {
        // TESTING.md's attribution rule. Not a criterion of its own — it is what makes
        // every HTTP-based assertion here mean anything at all.
        await StackCheck.AssertHealthAnswersFromThisStackAsync(fixture.Stack);
    }

    [Fact]
    public async Task The_identity_host_answering_us_belongs_to_this_stack_too()
    {
        // AC6 trusts what the identity host says as much as AC1 trusts the API, so it
        // needs the same proof. Attribution established for one service is not
        // attribution for another sharing the machine.
        await StackCheck.AssertHealthAnswersFromThisStackAsync(fixture.Stack, "identity");
    }

    [Fact]
    public async Task AC6_a_token_the_identity_host_issued_is_accepted()
    {
        var tokens = new TokenFactory(fixture.Stack);
        var token = await tokens
            .IssuedTokenAsync(ComposeProject.MemberClientId, ComposeProject.MemberClientSecret);

        var api = await fixture.Stack.BaseAddressAsync("api");

        Assert.Equal(HttpStatusCode.OK, await TokenFactory.CallAuthenticatedAsync(api, token));
    }

    [Fact]
    public async Task AC6_an_expired_token_is_refused()
    {
        var tokens = new TokenFactory(fixture.Stack);
        var api = await fixture.Stack.BaseAddressAsync("api");

        var status = await TokenFactory
            .CallAuthenticatedAsync(api, await tokens.ExpiredTokenAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task AC6_a_token_for_another_audience_is_refused()
    {
        var tokens = new TokenFactory(fixture.Stack);
        var api = await fixture.Stack.BaseAddressAsync("api");

        var status = await TokenFactory
            .CallAuthenticatedAsync(api, await tokens.WrongAudienceTokenAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task AC6_a_token_signed_by_an_unknown_key_is_refused()
    {
        // T-0010 calls this "the one that matters": it is the difference between
        // validating a signature and merely reading a token. A resource server that
        // accepts this accepts anything anyone cares to write.
        var api = await fixture.Stack.BaseAddressAsync("api");

        var status = await TokenFactory
            .CallAuthenticatedAsync(api, TokenFactory.UnknownKeyToken());

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }
}
