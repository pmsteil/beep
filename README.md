## beep

A tiny CLI that plays a pleasant “done” sound. Great for finishing long-running commands:

### Features
- Cross-platform design with macOS prioritized
- Simple defaults: just run `beep`
- Options for sound, volume, and repeat

### Example Usage


### Install (bin/ scripts)

From repo root:

```bash
# Build
bin/build.sh

# Publish single-file binary for your OS (auto-detects RID by default)
bin/publish.sh Release

# Install symlink to PATH (prefers /opt/homebrew/bin on macOS, falls back to ~/.local/bin)
bin/install-symlink.sh

# Test
which beep && beep --help
```

Other helpful scripts:
- `bin/uninstall-symlink.sh`: removes the symlink.
- `bin/tool-pack-install.sh`: pack + install as a .NET global tool.

### Usage

```bash
beep [sounds...] [--sound <name|path> (repeatable)] [--volume <0-100>] [--repeat <n>] [--wait] [-h|--help]
```

Defaults: `glass` on macOS, volume 100, repeat 1, non-blocking for a single sound.

Notes:
- You can pass one or more sounds either positionally (`beep glass ping`) or via repeatable `--sound` flags (`-s glass -s ping`). A sound can be an alias or a file path.
- By default a single sound plays non-blocking (process exits quickly). Add `--wait` to block until playback finishes. Any `--repeat` value implies blocking per repeat.
- On macOS, playback uses `afplay` with system sounds in `/System/Library/Sounds`.
- On Linux/Windows, behavior will improve in upcoming versions; terminal bell or Windows sounds are used as available.

### Example Usage (all permutations)

```bash
# 1) Default sound (non-blocking)
beep

# 2) Single alias (positional)
beep glass

# 3) Multiple aliases (positional, blocks until all finish)
beep glass ping hero --wait

# 4) Repeatable --sound flags (equivalent to positional)
beep -s glass -s ping -s hero --wait

# 5) Mix alias and file path
beep glass ~/Downloads/notify.wav --wait

# 6) Custom volume
beep glass --volume 30

# 7) Repeat N times (always blocks)
beep glass --repeat 3

# 8) Non-blocking single sound vs blocking
beep ping            # returns quickly
beep ping --wait     # waits until done

# 9) Combine options
beep -s glass -s ping --volume 50 --repeat 2 --wait
```

### Development

Prereqs: .NET 8 SDK on your machine (macOS Apple Silicon supported).

Local workflow (to be confirmed after code is added):

```bash
dotnet build
dotnet run --project src/Beep
```

Pack as tool:

```bash
dotnet pack -c Release -o ./nupkgs
```

### Architecture (for non-.NET developers)

- **What is .NET?** A modern, cross-platform runtime and SDK, similar to Python + pip, but compiled. You install the SDK (`dotnet`) to build and run apps.

- **Project layout**
  - `Beep.sln`: solution file (workspace container).
  - `src/Beep/Beep.csproj`: project file (like Python `pyproject.toml`). Declares target framework (`net9.0`), metadata, and packaging settings.
  - `src/Beep/Program.cs`: main C# source. Contains `Main(...)`, argument parsing, and OS-specific playback.
  - `bin/*.sh`: convenience scripts that wrap common `dotnet` commands.

- **Entry point and CLI**
  - `Program.Main` reads args and fills a simple options object.
  - Supported args: positional `sounds...`, repeatable `--sound|-s`, `--volume|-v`, `--repeat|-r`, `--wait`, `--help`.
  - Multiple sounds play in order; `-r N` repeats the entire sequence `N` times.

- **Platform detection**
  - Uses `RuntimeInformation.IsOSPlatform(...)` to choose a backend.

- **Audio backends**
  - macOS: spawns `afplay <file>`; system sound aliases map to files in `/System/Library/Sounds` (e.g., `glass`, `ping`, `hero`, `submarine`, `tink`, `pop`, `purr`, `basso`, `blow`).
    - Non-blocking default for a single sound starts `afplay` and exits immediately; `--wait` blocks.
  - Windows: uses `System.Media.SystemSounds` when actually running on Windows.
  - Linux: temporary fallback to terminal bell; future: `paplay`/`canberra-gtk-play`/`aplay`.

- **Build vs Publish (analogy to Python)**
  - Build: like `python -m build` compiling to IL; produces `dll` and intermediate outputs.
  - Publish: like freezing to a standalone binary. `bin/publish.sh` creates a single executable for your OS/arch.
    - RID (Runtime Identifier): target OS/arch, e.g., `osx-arm64`, `linux-x64`, `win-x64`.
    - Self-contained: bundles the .NET runtime into the executable; larger, runs without preinstalled .NET.
    - Framework-dependent: smaller, requires the .NET runtime to be installed.

- **Packaging as a .NET Tool (similar to a global pip tool)**
  - We set `<PackAsTool>true</PackAsTool>` and `ToolCommandName` in `Beep.csproj`.
  - `bin/tool-pack-install.sh` packs a `.nupkg` (NuGet package) and installs it globally (`~/.dotnet/tools/beep`).
  - This is analogous to `pip install --user <package>` putting a console entry-point on your PATH.

- **bin/ scripts mapping**
  - `bin/build.sh` → `dotnet build Beep.sln`.
  - `bin/publish.sh` → `dotnet publish src/Beep -r <RID> -p:PublishSingleFile=... -p:SelfContained=...`.
  - `bin/install-symlink.sh` → symlink the published binary to a PATH dir.
  - `bin/tool-pack-install.sh` → `dotnet pack` + `dotnet tool install -g` from local `./nupkgs`.

- **Performance notes**
  - First run can be slower (runtime cold start). Subsequent runs are faster.
  - Default behavior starts playback and exits quickly for a single sound; use `--wait` to block.

### Roadmap
- v0.1: macOS backend + CLI
- v0.2: Linux and Windows fallbacks
- v1.0: Package as a global tool and single-file binary

### License
MIT


