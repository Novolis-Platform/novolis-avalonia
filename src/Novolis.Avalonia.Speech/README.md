<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Speech

Application-level speech front for Avalonia hosts.

## Install

Add a package reference to `Novolis.Avalonia.Speech` and register
`AddNovolisSpeech()` in the host's dependency-injection container.

## Usage

It selects between the local `IVoiceService` and a configured
`Novolis.Audio.Voice.AzureSpeech` client. Azure configuration is stored through
the host's `ISecureTokenStore`; the package never provides a Novolis relay or
subscription.

Device voice is playback-only. Azure Speech is the path used when the caller
needs MP3 bytes.
