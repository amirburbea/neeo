using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Xunit;

namespace Neeo.Sdk.Tests.Devices.Features;

public sealed class RegistrationFeatureTests
{
    [Fact]
    public async Task QueryIsRegistered_should_simply_wrap_delegate_result()
    {
        using CancellationTokenSource cts = new();
        Mock<QueryIsRegistered> mockQuery = new(MockBehavior.Strict);
        mockQuery
            .Setup(static query => query(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        RegistrationFeature feature = RegistrationFeature.Create<TestCredentials>(
            queryIsRegistered: mockQuery.Object,
            register: (_, _) => Task.FromResult(RegistrationResult.Success)
        );

        IsRegisteredResponse result = await feature.QueryIsRegisteredAsync(cts.Token);

        Assert.True(result.Registered);
        mockQuery.Verify(query => query(cts.Token), Times.Once);
    }

    [Theory]
    [InlineData(Constants.ValidUserName, Constants.ValidPassword, true)]
    [InlineData("wrong-user", "wrong-password", false)]
    public async Task RegisterAsync_deserializes_payload_and_invokes_register(string userName, string password, bool success)
    {
        using CancellationTokenSource cts = new();
        Mock<Func<TestCredentials, CancellationToken, Task<RegistrationResult>>> mockRegister = new(MockBehavior.Strict);
        mockRegister
            .Setup(register => register(It.IsAny<TestCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(static (TestCredentials credentials, CancellationToken _) => credentials switch
            {
                TestCredentials(Constants.ValidUserName, Constants.ValidPassword) => RegistrationResult.Success,
                _ => RegistrationResult.Failed("Invalid credentials"),
            });
        RegistrationFeature feature = RegistrationFeature.Create(
            queryIsRegistered: (_) => Task.FromResult(true),
            register: mockRegister.Object
        );

        TestCredentials credentials = new(userName, password);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(credentials, JsonSerializerOptions.Web);
        RegistrationResult result = await feature.RegisterAsync(bytes, cts.Token);

        Assert.Equal(success, result.IsSuccess);
        mockRegister.Verify(register => register(credentials, cts.Token), Times.Once);
    }

    public readonly record struct TestCredentials(string UserName, string Password);

    private static class Constants
    {
        public const string ValidPassword = "password";
        public const string ValidUserName = "user";
    }
}
