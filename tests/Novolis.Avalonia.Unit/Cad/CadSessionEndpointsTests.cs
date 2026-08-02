using Novolis.Avalonia.Cad.Session;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadSessionEndpointsTests
{
    [Test]
    public async Task ResolveHttpPort_DefaultsWhenUnset()
    {
        var prior = Environment.GetEnvironmentVariable(CadSessionEndpoints.HttpPortEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CadSessionEndpoints.HttpPortEnvVar, null);
            await Assert.That(CadSessionEndpoints.ResolveHttpPort()).IsEqualTo(CadSessionEndpoints.DefaultHttpPort);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CadSessionEndpoints.HttpPortEnvVar, prior);
        }
    }

    [Test]
    public async Task ResolveTcpPort_ParsesValidValue()
    {
        var prior = Environment.GetEnvironmentVariable(CadSessionEndpoints.TcpPortEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CadSessionEndpoints.TcpPortEnvVar, "19001");
            await Assert.That(CadSessionEndpoints.ResolveTcpPort()).IsEqualTo(19001);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CadSessionEndpoints.TcpPortEnvVar, prior);
        }
    }

    [Test]
    public async Task IsHttpEnabledByEnvironment_RespectsExplicitFalse()
    {
        var priorEnable = Environment.GetEnvironmentVariable(CadSessionEndpoints.EnableEnvVar);
        var priorHttp = Environment.GetEnvironmentVariable(CadSessionEndpoints.HttpEnableEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(CadSessionEndpoints.EnableEnvVar, "true");
            Environment.SetEnvironmentVariable(CadSessionEndpoints.HttpEnableEnvVar, "false");
            await Assert.That(CadSessionEndpoints.IsHttpEnabledByEnvironment()).IsFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(CadSessionEndpoints.EnableEnvVar, priorEnable);
            Environment.SetEnvironmentVariable(CadSessionEndpoints.HttpEnableEnvVar, priorHttp);
        }
    }

    [Test]
    public async Task MarkerPaths_AreUnderTemp()
    {
        await Assert.That(CadSessionEndpoints.HttpMarkerPath.Contains("novolis-cad-session.http", StringComparison.Ordinal))
            .IsTrue();
        await Assert.That(CadSessionEndpoints.TcpMarkerPath.Contains("novolis-cad-session.tcp", StringComparison.Ordinal))
            .IsTrue();
    }
}
