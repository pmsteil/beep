## beep

A tiny CLI that plays a pleasant “done” sound. Great for finishing long-running commands:

```bash
make build && beep
```

### Features
- Cross-platform design with macOS prioritized
- Simple defaults: just run `beep`
- Options for sound, volume, and repeat

### Install

Two options are planned:

1) .NET Global Tool (recommended during development)
- Build and pack locally, then install globally from the local package source.

Steps (local dev):

```bash
# From repo root
dotnet pack -c Release -o ./nupkgs
dotnet tool uninstall -g dev.patiman.beep || true
dotnet tool install -g dev.patiman.beep --add-source ./nupkgs

# Ensure ~/.dotnet/tools is on PATH, then:
beep --help
```

2) Single-file binary
- Publish a self-contained binary and place it on your PATH.

Steps (macOS arm64):

```bash
dotnet publish src/Beep -c Release -r osx-arm64 -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true --no-self-contained
ln -sf "$(pwd)/src/Beep/bin/Release/net9.0/osx-arm64/publish/Beep" /opt/homebrew/bin/beep
```

Detailed install steps are documented below once implementation is complete.

### Usage

```bash
beep [--sound <name|path>] [--volume <0-100>] [--repeat <n>] [-h|--help]
```

Defaults: `--sound glass` on macOS, volume 100, repeat 1.

Examples:

```bash
# Basic
beep

# Choose a sound by alias
beep --sound glass

# Use a custom file
beep --sound ~/Downloads/notify.wav

# Lower volume and repeat 3x
beep --volume 50 --repeat 3
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

### Roadmap
- v0.1: macOS backend + CLI
- v0.2: Linux and Windows fallbacks
- v1.0: Package as a global tool and single-file binary

### License
MIT


