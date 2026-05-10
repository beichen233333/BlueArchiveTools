using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BlueArchiveTools.CLI.Xtractor
{
    public static class UABEAHelper
    {
        public static void RunFullExtraction(string inputDir, string outputDir)
        {
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            // 提取图片
            ExecuteUABEAExport(inputDir, outputDir, "Texture2D", "png");

            // 提取文本
            ExecuteUABEAExport(inputDir, outputDir, "TextAsset", "txt");

            // 提取音频
            ExecuteUABEAExport(inputDir, outputDir, "AudioClip", "wav");

            // 提取MonoBehaviour
            ExecuteUABEAExport(inputDir, outputDir, "MonoBehaviour", "json");

            // 未包含用dump
            ExecuteFinalCleanup(inputDir, outputDir);
        }

        private static void ExecuteUABEAExport(string targetDir, string outDir, string typeFilter, string format)
        {
            var argsList = new List<string>
            {
                "export",
                "-d", targetDir,
                "-o", outDir,
                "--recursive",
                "--format", format,
                "--keepnames"
            };

            if (!string.IsNullOrEmpty(typeFilter))
            {
                argsList.Add("-t");
                argsList.Add(typeFilter);
            }

            try
            {
                UABEAvalonia.Program.Main(argsList.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export failed for {typeFilter}: {ex.Message}");
            }
        }

        private static void ExecuteFinalCleanup(string targetDir, string outDir)
        {
            string[] uabeaArgs = {
                "export",
                "-d", targetDir,
                "-o", outDir,
                "--recursive",
                "--format", "dump", 
                "--keepnames",
                "-t", "!Texture2D,!TextAsset,!AudioClip,!MonoBehaviour" 
            };

            try
            {
                UABEAvalonia.Program.Main(uabeaArgs);
            }
            catch {}
        }
    }
}
