<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Ndjson

Avalonia record-slice chrome over an
[`INdjsonDocument`](https://www.nuget.org/packages/Novolis.IO.Ndjson).

The control owns navigation, bounded reads, refresh, malformed-line
presentation, expansion, and copy. Hosts retain file selection and document
lifetime.

## Install

```powershell
dotnet add package Novolis.Avalonia.Ndjson
```

Requires .NET 10, Avalonia, `Novolis.IO.Ndjson`, and
`Novolis.Avalonia.GraphicalProfile`. Install the profile in the application
before placing the control in a window.

## Quick start

```csharp
using Novolis.Avalonia.Ndjson;

var slice = new NdjsonSliceView
{
    RefreshDocumentAsync = cancellationToken => OpenDocumentAsync(cancellationToken),
};
```

`OpenDocumentAsync` returns the host-owned `INdjsonDocument`. The view does not
choose the file or dispose the document.
