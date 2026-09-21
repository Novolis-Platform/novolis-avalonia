# Novolis.Avalonia.Chat

Code-first Avalonia chrome for Novolis Chat:

- `ChatRail` for spaces, named channels, and unread counts.
- `ChatThreadPanel` for decrypted `MarkdownBody` projections and public
  `ChatFrame` thread metadata.
- `ChatPresenceList` for live presence DTOs.
- `ChatConversationChrome` for the composed three-column surface.

The control receives Markdown only after the application decrypts a protected
message. It never owns crypto, transport, or host processes.
