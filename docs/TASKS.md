## Task List

### Phase 1: Planning
- [x] Create project plan
- [x] Create README scaffold
- [x] Define CLI spec and success criteria

### Phase 2: Scaffold & CLI
- [x] Initialize .NET console app in `src/Beep`
- [x] Add lightweight CLI parsing
- [x] Implement macOS backend using `afplay` and system sounds
- [x] Support `--sound`, `--volume`, `--repeat`
- [x] Handle file path sound input
- [x] Basic error handling and exit codes

### Phase 3: Cross-Platform
- [ ] Linux: detect `paplay`/`canberra-gtk-play`/`aplay` fallback chain
- [ ] Windows: use `System.Media.SystemSounds`
- [ ] Terminal bell fallback on all OSes

### Phase 4: Packaging
- [x] Pack as .NET global tool
- [ ] Single-file publish for macOS (`osx-arm64`) and others
- [ ] Add install instructions to README

### Phase 5: Polish
- [ ] Add `--help` examples and friendly messages
- [ ] Add CI (optional)
- [ ] Tag v0.1.0


