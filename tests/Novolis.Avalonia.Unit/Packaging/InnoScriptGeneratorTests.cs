using Novolis.Avalonia.Packaging.Inno;

namespace Novolis.Avalonia.Unit.Packaging;

public class InnoScriptGeneratorTests
{
    [Test]
    public async Task Generate_Uses_User_Space_Install_Path()
    {
        var script = new InnoScriptGenerator
        {
            AppName = "Novolis Audio Live",
            AppVersion = "2026.1.6.123",
            PublishDir = @"C:\publish\app",
            AppExeName = "Novolis.Audio.Live.Studio.exe",
            OutputDir = @"C:\publish\installer",
        }.Generate();

        await Assert.That(script).Contains("PrivilegesRequired=lowest");
        await Assert.That(script).Contains("{localappdata}\\Programs\\Novolis\\Novolis Audio Live");
        await Assert.That(script).Contains("Source: \"C:\\publish\\app\\*\"");
        await Assert.That(script).Contains("OutputDir=C:\\publish\\installer");
        await Assert.That(script).Contains("Filename: \"{app}\\Novolis.Audio.Live.Studio.exe\"");
    }
}
