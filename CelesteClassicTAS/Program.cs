using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CelesteClassicTAS;

public static class Program {
    private static readonly Regex randomSeedRegex = new(@"\[[\d\.,]*\]", RegexOptions.Compiled);

    public static void Main(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("Please drag and drop pico 8 tas file or folder");
            return;
        }

        string path = args[0];
        FileAttributes attr = File.GetAttributes(path);

        if (attr.HasFlag(FileAttributes.Directory)) {
            ConvertFolder(path);
        } else {
            ConvertFile(path);
        }
    }

    private static void ConvertFolder(string folderPath) {
        DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

        foreach (FileInfo fileInfo in directoryInfo.GetFiles()) {
            if (fileInfo.Extension == ".tas") {
                ConvertFile(fileInfo.FullName);
            }
        }

        foreach (DirectoryInfo subDir in directoryInfo.GetDirectories()) {
            ConvertFolder(subDir.FullName);
        }
    }

    private static void ConvertFile(string filePath) {
        Console.WriteLine($"Converting {filePath} -> {filePath}");

        List<string> result = new();

        if (GetLoadCommand(filePath) is { } loadCommand) {
            result.Add(loadCommand);
        }

        // Celeste Classic is 30fps, but Celeste is 60fps, so add two frames at a time
        int frames = 2;
        string allText = File.ReadAllText(filePath);

        List<string> allLines = randomSeedRegex.Replace(allText, "").Split(',').ToList();

        // Remove the last 2 frames because the ceiling is lower than classic
        int linesCount = allLines.Count - 2;
        for (int i = 0; i < linesCount; i++) {
            ParseLine(allLines[i], out string? action);
            ParseLine(i == linesCount - 1 ? null : allLines[i + 1], out string? nextAction);

            if (action != nextAction) {
                if (action.Length == 0) {
                    result.Add(frames.ToString().PadLeft(4));
                } else {
                    result.Add(frames.ToString().PadLeft(4) + "," + action);
                }

                frames = 2;
            } else {
                frames += 2;
            }
        }

        File.WriteAllText(filePath, string.Join("\n", result));
    }

    private static string? GetLoadCommand(string filePath) {
        string fileName = Path.GetFileNameWithoutExtension(filePath).ToUpper();
        if (int.TryParse(fileName.Replace("TAS", ""), out int index)) {
            string? room = $"{(index - 1) % 8} {(index - 1) / 8}";

            int frames = index switch {
                1 => 212,
                2 => 48,
                4 => 56,
                5 => 60,
                6 => 60,
                7 => 46,
                9 => 56,
                11 => 48,
                16 => 56,
                18 => 56,
                19 => 48,
                20 => 64,
                21 => 76,
                24 => 48,
                27 => 48,
                28 => 100,
                29 => 60,
                30 => 64,
                31 => 48,
                _ => 52
            };

            string waitFrames = index == 7 ? "   2\n" : "";
            string titleAction = index == 1 ? "   2\n   2,J,X\n" : "";
            return $"console pico {room}\n{waitFrames}\n#Start\n{titleAction}{frames.ToString(),4}";
        }

        return null;
    }

    private static void ParseLine(string? line, out string? action) {
        if (line == null) {
            action = null;
            return;
        }

        action = "";

        if (int.TryParse(line, out int value)) {
            action = ActionToString((Pico8Actions) value);
        }
    }

    private static string ActionToString(Pico8Actions actions) {
        StringBuilder sb = new();
        if (actions.HasFlag(Pico8Actions.Left)) {
            sb.Append(",L");
        }

        if (actions.HasFlag(Pico8Actions.Right)) {
            sb.Append(",R");
        }

        if (actions.HasFlag(Pico8Actions.Up)) {
            sb.Append(",U");
        }

        if (actions.HasFlag(Pico8Actions.Down)) {
            sb.Append(",D");
        }

        if (actions.HasFlag(Pico8Actions.Jump)) {
            sb.Append(",J");
        }

        if (actions.HasFlag(Pico8Actions.Dash)) {
            sb.Append(",X");
        }

        return sb.ToString();
    }
}

[Flags]
public enum Pico8Actions {
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Up = 1 << 2,
    Down = 1 << 3,
    Jump = 1 << 4,
    Dash = 1 << 5,
}