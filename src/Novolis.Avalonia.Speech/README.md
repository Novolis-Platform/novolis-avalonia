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
