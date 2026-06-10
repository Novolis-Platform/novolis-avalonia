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

    [Test]
    public async Task Generate_Includes_Upgrade_And_Publisher_Metadata()
    {
        var script = new InnoScriptGenerator
        {
            AppName = "Manuscript Studio",
            AppVersion = "2026.1.0.42",
            PublishDir = @"C:\publish\app",
            AppExeName = "ManuscriptStudio.exe",
            OutputDir = @"C:\publish\installer",
            AppId = "Novolis.ManuscriptStudio",
            AppSupportUrl = "https://github.com/Novolis-Platform/novolis-apps/issues",
            AppUpdatesUrl = "https://github.com/Novolis-Platform/novolis-apps/releases",
        }.Generate();

        await Assert.That(script).Contains("UsePreviousAppDir=yes");
        await Assert.That(script).Contains("DisableDirPage=auto");
        await Assert.That(script).Contains("CloseApplications=yes");
        await Assert.That(script).Contains("CloseApplicationsFilter=ManuscriptStudio.exe");
        await Assert.That(script).Contains("RestartApplications=yes");
        await Assert.That(script).Contains("AppPublisher=Novolis Platform");
        await Assert.That(script).Contains("AppSupportURL=https://github.com/Novolis-Platform/novolis-apps/issues");
        await Assert.That(script).Contains("AppUpdatesURL=https://github.com/Novolis-Platform/novolis-apps/releases");
        await Assert.That(script).Contains("VersionInfoVersion=2026.1.0.42");
    }
}
