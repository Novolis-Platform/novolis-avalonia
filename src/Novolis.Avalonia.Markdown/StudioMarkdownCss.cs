namespace Novolis.Avalonia.Markdown;

internal static class StudioMarkdownCss
{
    public const string Dark = """
        html, body {
          margin: 0;
          padding: 0;
          background: #1e1e1e;
        }
        .markdown-body.studio {
          font-family: "Segoe UI", system-ui, sans-serif;
          font-size: 15px;
          line-height: 1.6;
          color: #e8e8e8;
          padding: 16px 20px 24px;
          max-width: none;
        }
        .markdown-body.studio h1,
        .markdown-body.studio h2,
        .markdown-body.studio h3,
        .markdown-body.studio h4,
        .markdown-body.studio h5,
        .markdown-body.studio h6 {
          color: #f5f5f5;
          font-weight: 600;
          margin-top: 1.4em;
          margin-bottom: 0.6em;
          line-height: 1.25;
        }
        .markdown-body.studio h1 { font-size: 1.75em; border-bottom: 1px solid #3a3a3a; padding-bottom: 0.25em; }
        .markdown-body.studio h2 { font-size: 1.4em; border-bottom: 1px solid #333; padding-bottom: 0.2em; }
        .markdown-body.studio h3 { font-size: 1.2em; }
        .markdown-body.studio p { margin: 0 0 0.9em; }
        .markdown-body.studio a { color: #6eb5ff; text-decoration: none; }
        .markdown-body.studio a:hover { text-decoration: underline; }
        .markdown-body.studio code {
          font-family: "Cascadia Code", Consolas, "Courier New", monospace;
          font-size: 0.92em;
          background: #2a2a2a;
          padding: 0.15em 0.35em;
          border-radius: 4px;
        }
        .markdown-body.studio pre {
          font-family: "Cascadia Code", Consolas, "Courier New", monospace;
          font-size: 0.9em;
          background: #252526;
          border: 1px solid #3a3a3a;
          border-radius: 6px;
          padding: 12px 14px;
          overflow-x: auto;
          margin: 0 0 1em;
        }
        .markdown-body.studio pre code { background: transparent; padding: 0; }
        .markdown-body.studio blockquote {
          margin: 0 0 1em;
          padding: 8px 14px;
          border-left: 3px solid #4a7a9a;
          background: #1a2430;
          color: #c8d4e0;
        }
        .markdown-body.studio ul, .markdown-body.studio ol {
          margin: 0 0 1em;
          padding-left: 1.6em;
        }
        .markdown-body.studio li { margin: 0.2em 0; }
        .markdown-body.studio table {
          border-collapse: collapse;
          width: 100%;
          margin: 0 0 1em;
        }
        .markdown-body.studio th, .markdown-body.studio td {
          border: 1px solid #444;
          padding: 6px 10px;
          text-align: left;
        }
        .markdown-body.studio th { background: #2d2d30; font-weight: 600; }
        .markdown-body.studio tr:nth-child(even) td { background: #252526; }
        .markdown-body.studio hr {
          border: none;
          border-top: 1px solid #3a3a3a;
          margin: 1.5em 0;
        }
        .markdown-body.studio img { max-width: 100%; border-radius: 4px; }
        """;

    public const string GitHubLight = """
        html, body { margin: 0; padding: 0; background: #fff; }
        .markdown-body.github-light {
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
          font-size: 16px;
          line-height: 1.5;
          color: #24292e;
          padding: 16px 20px 24px;
        }
        .markdown-body.github-light h1, .markdown-body.github-light h2 {
          border-bottom: 1px solid #eaecef;
          padding-bottom: 0.3em;
        }
        .markdown-body.github-light code {
          font-family: SFMono-Regular, Consolas, monospace;
          background: rgba(27,31,35,.05);
          padding: 0.2em 0.4em;
          border-radius: 3px;
        }
        .markdown-body.github-light pre {
          background: #f6f8fa;
          padding: 16px;
          border-radius: 6px;
          overflow: auto;
        }
        .markdown-body.github-light blockquote {
          border-left: 4px solid #dfe2e5;
          color: #6a737d;
          padding-left: 16px;
          margin-left: 0;
        }
        .markdown-body.github-light table { border-collapse: collapse; }
        .markdown-body.github-light th, .markdown-body.github-light td {
          border: 1px solid #dfe2e5;
          padding: 6px 13px;
        }
        .markdown-body.github-light a { color: #0366d6; }
        """;
}
