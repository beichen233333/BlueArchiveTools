using System;
using System.IO;

namespace BlueArchiveTools;

public class Program
{
    public static void Main(string[] args)
    {
        // 打印路径信息
        string currentDir = Environment.CurrentDirectory;
        Console.WriteLine($"[DEBUG] 当前工作目录: {currentDir}");

        string filePath = @"测试.txt";
        string fullPath = Path.GetFullPath(filePath);
        Console.WriteLine($"[DEBUG] 目标文件路径: {fullPath}");

        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"[ERROR] 文件不存在: {fullPath}");
            return;
        }

        string targetCrc = "123456";
        string tempPath = fullPath + ".tmp";
        string[] cliArgs = { "crc", "patch", targetCrc, fullPath, tempPath, "-o" };

        Console.WriteLine("[INFO] 正在调用 CLI 执行补丁...");
        
        try 
        {
            // 调用 CLI 项目的入口
            BlueArchiveTools.CLI.Program.Main(cliArgs); 

            if (File.Exists(tempPath))
            {
                File.Move(tempPath, fullPath, true);
                Console.WriteLine("[SUCCESS] 补丁应用成功。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL] 运行异常: {ex.Message}");
        }
    }
}
