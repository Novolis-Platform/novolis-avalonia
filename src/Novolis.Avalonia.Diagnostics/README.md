<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Diagnostics

Installs early process and Avalonia UI-thread exception recording into an
app-private `Novolis.Logging.Core` diagnostic journal. Platform packages provide
the user-facing export action.

## Install

Add a package reference to `Novolis.Avalonia.Diagnostics` and register its
diagnostic services in the Avalonia host.

## Usage

Initialize the diagnostic service during application startup before creating
the main window.
