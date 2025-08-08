## Project Plan: beep (CLI completion sound)

### Objective
Build a tiny, reliable CLI command named `beep` that plays a short, pleasant “checkmark”/“done” sound. It is intended for use at the end of long-running terminal commands, e.g. `make build && beep`.

### Primary User
- You (macOS, Apple Silicon), running commands in Terminal/iTerm/zsh/bash.

### Success Criteria
- `beep` runs instantly and exits quickly (≤ 150ms overhead, excluding sound duration).
- On macOS: plays a recognizable, pleasant system sound (default), with options to choose others.
- Optional cross-platform behavior: sensible fallbacks for Linux and Windows.
- Easy installation: either as a .NET global tool or a self-contained single binary placed on PATH.

### Scope
- Minimal CLI with stable defaults.
- Options for sound selection, volume, and repeat.
- Friendly `--help` output.
- Packaging: local install via .NET tool; optional single-file publish for direct PATH usage.

### Non-Goals (initial version)
- No GUI, notifications, or complex scheduling.
- No background daemon or shell integration beyond being callable as a command.
- No network access or telemetry.

### Requirements
- Works on macOS without extra dependencies by leveraging built-in system sounds via `afplay`.
- Does not throw on unsupported platforms; uses graceful fallbacks (e.g., terminal bell `\a`).
- Zero configuration for the common case: `beep` with no arguments plays the default sound.

### Constraints & Considerations
- `Console.Beep` and `System.Media.SoundPlayer` are not generally supported on macOS/Linux in .NET; use OS-native players.
- macOS: `afplay` is available by default; system sounds under `/System/Library/Sounds/*.aiff` are accessible.
- Linux: availability varies. Prefer `paplay`/`canberra-gtk-play`/`aplay` with common system sounds when present.
- Windows: `System.Media.SystemSounds.Asterisk.Play()` is available and reliable.

### High-Level Design
- Language/Runtime: .NET 8 console app (C#).
- Command: `beep`.
- Sound backends (auto-detected):
  - macOS: `afplay` + system sound file (default: `Glass.aiff`), with `-v` for volume.
  - Linux: try `paplay` with a known sound (e.g., `freedesktop/stereo/complete.oga`), fallback to `canberra-gtk-play`, then terminal bell.
  - Windows: `System.Media.SystemSounds.Asterisk` (repeat/volume approximated via repeats; volume control TBD).
- Packaging:
  1) .NET Global Tool (`PackAsTool`), `ToolCommandName=beep`.
  2) Self-contained, single-file binary (`dotnet publish -r osx-arm64`), optional symlink into PATH.

### CLI Specification (v0.1)
- `beep [--sound <name|path>] [--volume <0-100>] [--repeat <n>] [-h|--help]`
- Defaults:
  - sound: `glass` on macOS; reasonable default per-OS otherwise.
  - volume: 100 (best effort; on macOS mapped to `afplay -v 1.0`).
  - repeat: 1
- macOS built-in sound aliases: `glass`, `ping`, `hero`, `submarine`, `tink`, `pop`, `purr`, `basso`, `blow`.
- If `--sound` is a file path, play that file (if supported by OS backend).

### Developer Workflow
1) Install .NET 8 SDK.
2) Clone repo; build and run locally via `dotnet run`.
3) Package as a local .NET tool (`dotnet pack`) and install globally from local nupkg source; or publish a single binary.

### Testing Strategy
- Manual tests on macOS: run `beep` with various options; test chained after long-running commands.
- Smoke tests: ensure process exit code is 0 on success paths and non-zero on invalid flags or missing files.

### Risks & Mitigations
- OS audio command not found: detect and fallback to terminal bell with clear stderr note.
- Volume control parity across OSes: best-effort only; document limitations.
- Name collision with other `beep` commands: user controls PATH order; document how to check `which beep`.

### Milestones
1) Planning and docs (this commit).
2) Scaffold .NET console and implement macOS backend + CLI parsing.
3) Add Linux and Windows fallbacks.
4) Package as .NET tool; add single-file publish instructions.
5) Polish README; tag v0.1.0.


