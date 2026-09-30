using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace Novolis.Avalonia.Live;

/// <summary>Live DSL completion entries for AvaloniaEdit.</summary>
public sealed class LiveDslCompletionData : ICompletionData
{
    public LiveDslCompletionData(string text, string description, string? insertText = null)
    {
        Text = text;
        Description = description;
        Content = text;
        InsertionText = insertText ?? text;
    }

    public string Text { get; }
    public object Content { get; }
    public object Description { get; }
    public double Priority => 0;
    public string InsertionText { get; }

    public global::Avalonia.Media.IImage? Image => null;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, InsertionText);
    }
}
