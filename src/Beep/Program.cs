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
                Sound = null,
                VolumePercent = 100,
                Repeat = 1,
                ShowHelp = false
            };

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                switch (arg)
                {
                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;
                    case "-s":
                    case "--sound":
                        EnsureHasValue(args, i, arg);
                        options.Sound = args[++i];
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
                    default:
                        throw new ArgumentException($"Unknown argument: {arg}");
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return TryPlayOnWindows(options);
                }
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
            string? candidatePath = null;

            if (!string.IsNullOrWhiteSpace(options.Sound))
            {
                var s = options.Sound!;
                if (File.Exists(s))
                {
                    candidatePath = s;
                }
                else if (macAliases.TryGetValue(s, out var aliasPath))
                {
                    candidatePath = aliasPath;
                }
                else
                {
                    // Unrecognized sound; fallback to default
                    candidatePath = macAliases["glass"];
                }
            }
            else
            {
                candidatePath = macAliases["glass"];
            }

            if (!File.Exists(candidatePath))
            {
                return false;
            }

            var volumeScalar = Math.Clamp(options.VolumePercent, 0, 100) / 100.0;

            for (var i = 0; i < options.Repeat; i++)
            {
                if (!RunProcess(
                        fileName: "afplay",
                        arguments: new[] { "-v", volumeScalar.ToString(CultureInfo.InvariantCulture), candidatePath! }))
                {
                    return false;
                }

                if (i + 1 < options.Repeat)
                {
                    Thread.Sleep(100);
                }
            }

            return true;
        }

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
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
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
  beep [--sound <name|path>] [--volume <0-100>] [--repeat <n>] [-h|--help]

Options:
  -s, --sound     Sound alias (macOS: glass, ping, hero, submarine, tink, pop, purr, basso, blow) or file path
  -v, --volume    Volume percent 0-100 (default: 100)
  -r, --repeat    Number of times to play (default: 1)
  -h, --help      Show help

Notes:
  - macOS uses 'afplay' with system sounds in /System/Library/Sounds.
  - Other OSes may fall back to terminal bell in this initial version.");
        }

        private sealed class ParsedOptions
        {
            public string? Sound { get; set; }
            public int VolumePercent { get; set; }
            public int Repeat { get; set; }
            public bool ShowHelp { get; set; }
        }
    }
}

