using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace BeepCli
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            ParsedOptions options;
            try
            {
                options = ParseArgs(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                PrintUsage();
                return 2;
            }

            if (options.ShowHelp)
            {
                PrintUsage();
                return 0;
            }

            var success = TryPlaySound(options);
            if (!success)
            {
                // Fallback: terminal bell
                for (var i = 0; i < options.Repeat; i++)
                {
                    Console.Write("\a");
                    Console.Out.Flush();
                    if (i + 1 < options.Repeat)
                    {
                        Thread.Sleep(100);
                    }
                }
            }

            return 0;
        }

        private static ParsedOptions ParseArgs(string[] args)
        {
            var options = new ParsedOptions
            {
                VolumePercent = 100,
                Repeat = 1,
                ShowHelp = false,
                Wait = true,
                Sounds = new List<string>(),
                GapMs = 10,
                SequenceGapMs = 25,
                DurationMs = 250
            };

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("-"))
                {
                    switch (arg)
                    {
                        case "-h":
                        case "--help":
                            options.ShowHelp = true;
                            break;
                        case "-s":
                        case "--sound":
                            EnsureHasValue(args, i, arg);
                            options.Sounds.Add(args[++i]);
                            break;
                        case "-v":
                        case "--volume":
                            EnsureHasValue(args, i, arg);
                            if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var vol) || vol < 0 || vol > 100)
                            {
                                throw new ArgumentException("--volume must be an integer between 0 and 100.");
                            }
                            options.VolumePercent = vol;
                            break;
                        case "-r":
                        case "--repeat":
                            EnsureHasValue(args, i, arg);
                            if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rep) || rep < 1 || rep > 50)
                            {
                                throw new ArgumentException("--repeat must be an integer between 1 and 50.");
                            }
                            options.Repeat = rep;
                            break;
                        case "--gap":
                            EnsureHasValue(args, i, arg);
                            if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var gap) || gap < 0 || gap > 10000)
                            {
                                throw new ArgumentException("--gap must be an integer between 0 and 10000 (milliseconds).");
                            }
                            options.GapMs = gap;
                            break;
                        case "--sequence-gap":
                            EnsureHasValue(args, i, arg);
                            if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var seqGap) || seqGap < 0 || seqGap > 10000)
                            {
                                throw new ArgumentException("--sequence-gap must be an integer between 0 and 10000 (milliseconds).");
                            }
                            options.SequenceGapMs = seqGap;
                            break;
                        case "-d":
                        case "--duration-ms":
                            EnsureHasValue(args, i, arg);
                            if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dur) || dur < 0 || dur > 60000)
                            {
                                throw new ArgumentException("--duration-ms must be an integer between 0 and 60000 (milliseconds). 0 means full sound length.");
                            }
                            options.DurationMs = dur;
                            break;
                        case "--wait":
                            options.Wait = true;
                            break;
                        case "--no-wait":
                            options.Wait = false;
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument: {arg}");
                    }
                }
                else
                {
                    // Positional argument treated as a sound alias or file path
                    options.Sounds.Add(arg);
                }
            }

            return options;
        }

        private static void EnsureHasValue(string[] args, int index, string name)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {name}.");
            }
        }

        private static bool TryPlaySound(ParsedOptions options)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return TryPlayOnMac(options);
                }
                #if !DISABLE_WINDOWS_SOUND
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return TryPlayOnWindows(options);
                }
                #endif
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Initial version: rely on terminal bell fallback
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryPlayOnMac(ParsedOptions options)
        {
            var macAliases = GetMacSoundAliases();
            var sounds = new List<string>();
            if (options.Sounds.Count == 0)
            {
                sounds.Add("glass");
            }
            else
            {
                sounds.AddRange(options.Sounds);
            }

            string ResolvePath(string sound)
            {
                if (File.Exists(sound)) return sound;
                if (macAliases.TryGetValue(sound, out var alias)) return alias;
                // Unknown token; fallback to default
                return macAliases["glass"];
            }

            var candidatePaths = new List<string>();
            foreach (var s in sounds)
            {
                candidatePaths.Add(ResolvePath(s));
            }

            var volumeScalar = Math.Clamp(options.VolumePercent, 0, 100) / 100.0;

            // Non-blocking optimization: exactly one sound and repeat==1 and !wait
            if (!options.Wait && candidatePaths.Count == 1 && options.Repeat == 1)
            {
                var argsList = BuildAfplayArgs(candidatePaths[0], volumeScalar, options.DurationMs);
                return StartProcess(fileName: "afplay", arguments: argsList);
            }

            // Block and play the entire sequence 'Repeat' times
            for (var i = 0; i < options.Repeat; i++)
            {
                for (var s = 0; s < candidatePaths.Count; s++)
                {
                    var path = candidatePaths[s];
                    var argsList = BuildAfplayArgs(path, volumeScalar, options.DurationMs);
                    if (!RunProcess("afplay", argsList))
                    {
                        return false;
                    }
                    // Spacing between distinct sounds within a sequence
                    if (s + 1 < candidatePaths.Count)
                    {
                        Thread.Sleep(options.GapMs);
                    }
                }
                // Pause between sequences
                if (i + 1 < options.Repeat)
                {
                    Thread.Sleep(options.SequenceGapMs);
                }
            }

            return true;
        }

        #if !DISABLE_WINDOWS_SOUND
        private static bool TryPlayOnWindows(ParsedOptions options)
        {
            try
            {
                for (var i = 0; i < options.Repeat; i++)
                {
                    System.Media.SystemSounds.Asterisk.Play();
                    if (i + 1 < options.Repeat)
                    {
                        Thread.Sleep(100);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endif

        private static bool StartProcess(string fileName, IReadOnlyList<string> arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = true
                    }
                };

                foreach (var arg in arguments)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                }

                return process.Start();
            }
            catch
            {
                return false;
            }
        }

        private static bool RunProcess(string fileName, IReadOnlyList<string> arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = true
                    }
                };

                foreach (var arg in arguments)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                }

                var started = process.Start();
                if (!started)
                {
                    return false;
                }

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> BuildAfplayArgs(string filePath, double volumeScalar, int durationMs)
        {
            var args = new List<string>
            {
                "-v",
                volumeScalar.ToString(CultureInfo.InvariantCulture)
            };

            if (durationMs > 0)
            {
                var seconds = (durationMs / 1000.0).ToString(CultureInfo.InvariantCulture);
                args.Add("-t");
                args.Add(seconds);
            }

            args.Add(filePath);
            return args;
        }

        private static Dictionary<string, string> GetMacSoundAliases()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "glass", "/System/Library/Sounds/Glass.aiff" },
                { "ping", "/System/Library/Sounds/Ping.aiff" },
                { "hero", "/System/Library/Sounds/Hero.aiff" },
                { "submarine", "/System/Library/Sounds/Submarine.aiff" },
                { "tink", "/System/Library/Sounds/Tink.aiff" },
                { "pop", "/System/Library/Sounds/Pop.aiff" },
                { "purr", "/System/Library/Sounds/Purr.aiff" },
                { "basso", "/System/Library/Sounds/Basso.aiff" },
                { "blow", "/System/Library/Sounds/Blow.aiff" }
            };
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"beep - play a pleasant completion sound

Usage:
  beep [sounds...] [--sound <name|path> (repeatable)] [--volume <0-100>] [--repeat <n>] [--gap <ms>] [--sequence-gap <ms>] [--duration-ms <ms>] [--wait|--no-wait] [-h|--help]

Options:
  -s, --sound     Sound alias (macOS: glass, ping, hero, submarine, tink, pop, purr, basso, blow) or file path (repeatable)
  -v, --volume    Volume percent 0-100 (default: 100)
  -r, --repeat    Number of times to play (default: 1)
  --gap           Milliseconds between sounds in a sequence (default: 10)
  --sequence-gap  Milliseconds between repeated sequences (default: 25)
  -d, --duration-ms  Max milliseconds to play each sound (0 = full length; default: 250)
  --wait          Block until the sound has finished playing (default)
  --no-wait       Return immediately (single-sound case only)
  -h, --help      Show help

Notes:
  - macOS uses 'afplay' with system sounds in /System/Library/Sounds.
  - Other OSes may fall back to terminal bell in this initial version.");
        }

        private sealed class ParsedOptions
        {
            public List<string> Sounds { get; set; } = new List<string>();
            public int VolumePercent { get; set; }
            public int Repeat { get; set; }
            public bool ShowHelp { get; set; }
            public bool Wait { get; set; }
            public int GapMs { get; set; }
            public int SequenceGapMs { get; set; }
            public int DurationMs { get; set; }
        }
    }
}

