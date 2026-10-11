# Novolis.Avalonia.AssetStudio

Reusable no-XAML Avalonia surfaces for the Asset Studio authoring workflow.

The package composes the existing `Novolis.Avalonia.Layout`,
`Novolis.Avalonia.Studio`, and `Novolis.Avalonia.GraphicalProfile` APIs. It
provides graph, stack, source, diagnostics, and optional preview panels. The
ordered node/port list is the accessible fallback for graph authoring and
supports keyboard selection and activation.

Preview rendering is an optional adapter supplied by the product host. The
editor can start, inspect recipes, validate source, and show diagnostics
without a GPU or a preview service.

## Install

```powershell
dotnet add package Novolis.Avalonia.AssetStudio
```
