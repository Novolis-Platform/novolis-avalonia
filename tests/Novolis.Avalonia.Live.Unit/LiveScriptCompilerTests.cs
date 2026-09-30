using Novolis.Avalonia.Live;

namespace Novolis.Avalonia.Unit.Live;

public sealed class LiveScriptCompilerTests
{
    [Test]
    public async Task CompileAsync_Repl_NotePlay_Succeeds()
    {
        var compiler = new LiveScriptCompiler();
        var program = await compiler.CompileAsync("Note.Play(C4)");

        await Assert.That(program).IsNotNull();
    }
}
