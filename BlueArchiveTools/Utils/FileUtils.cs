using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BlueArchiveTools.Utils;

public static class FileUtils
{
    public static List<string> FindFiles(
        string directory,
        List<string> keywords,
        bool absoluteMatch = false,
        bool sequentialMatch = false)
    {
        List<string> paths = new List<string>();
        List<Regex> compiledPatterns = new List<Regex>();

        foreach (var k in keywords)
        {
            try
            {
                compiledPatterns.Add(new Regex(absoluteMatch ? $"^{Regex.Escape(k)}$" : k));
            }
            catch
            {
                compiledPatterns.Add(new Regex(Regex.Escape(k)));
            }
        }

        if (!Directory.Exists(directory)) return paths;

        var allFiles = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);

        foreach (var file in allFiles)
        {
            string fileName = Path.GetFileName(file);
            if (compiledPatterns.Any(p => p.IsMatch(fileName)))
            {
                paths.Add(file);
            }
        }

        if (!sequentialMatch) return paths;

        List<string> sortedPaths = new List<string>();
        foreach (var pattern in compiledPatterns)
        {
            foreach (var p in paths)
            {
                if (pattern.IsMatch(Path.GetFileName(p)))
                {
                    sortedPaths.Add(p);
                    break;
                }
            }
        }
        return sortedPaths;
    }
}
