using System;
using System.IO;
using BlueArchiveTools.Xtractor;

namespace BlueArchiveTools
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = AppContext.BaseDirectory;
            DirectoryInfo root = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent;

            string tempPath = Path.Combine(root.FullName, "Temp");
            string testPath = Path.Combine(root.FullName, "测试");

            UABEAHelper.RunFullExtraction(tempPath, testPath);
            
            Console.WriteLine("Done.");
        }
    }
}
