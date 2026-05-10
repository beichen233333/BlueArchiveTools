using System;
using System.IO;
using System.Collections.Generic;
using BlueArchiveTools.CLI.MemoryPack;
using BlueArchiveTools.CLI.CRCManip;
using YldaDumpCsExporter;

namespace BlueArchiveTools.CLI;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            DisplayHelp();
            return;
        }

        string command = args[0].ToLower();

        switch (command)
        {
            case "memorypack":
                var cli = new MemoryPackCli();
                cli.Execute(args);
                break;

            case "dump":
                HandleDumpCommand(args);
                break;

            case "crc":
                HandleCrcCommand(args);
                break;

            default:
                Console.WriteLine($"[Error] Unknown command: {command}");
                DisplayHelp();
                break;
        }
    }

    private static void DisplayHelp()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("       BlueArchiveTools CLI 文档          ");
        Console.WriteLine("=================================================");
        Console.WriteLine("\n可用命令:");
        
        Console.WriteLine("\n1. [MemoryPack] 序列化与反序列化");
        Console.WriteLine("   格式: memorypack <mode> <server> <type> <input> <output>");
        Console.WriteLine("   - mode : serialize (Json->Bytes), deserialize (Bytes->Json)");
        Console.WriteLine("   - server : jp (日服), gl (国际服)");
        Console.WriteLine("   - type   : table, media, bundle");

        Console.WriteLine("\n2. [Dump] 导出 dump.cs");
        Console.WriteLine("   格式: dump <server> <il2cppPath> <metadataPath> <outputPath>");
        Console.WriteLine("   - server : cn (国服), gl (国际服), jp (日服)");
        Console.WriteLine("   注意: 国服不需要 il2cpp，可以为none");

        Console.WriteLine("\n3. [CRC] 修改或计算文件校验值");
        Console.WriteLine("   格式: crc <subcommand> [args]");
        Console.WriteLine("   - calc <file> : 计算 CRC32");
        Console.WriteLine("   - patch <target_hex> <input> <output> [-p pos] [-o]");

        Console.WriteLine("\n其他");
        Console.WriteLine("  -h, --help    显示此帮助信息");
        Console.WriteLine("=================================================");
    }

    private static void HandleCrcCommand(string[] args)
    {
        if (args.Length < 2) return;
        string sub = args[1].ToLower();
        var crc32 = new CRC32();
        if (sub == "calc")
        {
            if (args.Length < 3) return;
            using var fs = File.OpenRead(args[2]);
            Algorithm.Consume(crc32, fs);
            Console.WriteLine(crc32.HexDigest());
        }
        else if (sub == "patch")
        {
            if (args.Length < 5) return;
            uint target = uint.Parse(args[2], System.Globalization.NumberStyles.HexNumber);
            long? pos = null;
            bool overwrite = false;
            for (int i = 5; i < args.Length; i++)
            {
                if (args[i] == "-p" || args[i] == "--pos") pos = long.Parse(args[++i]);
                else if (args[i] == "-o" || args[i] == "--overwrite") overwrite = true;
            }
            using var fsIn = File.OpenRead(args[3]);
            using var fsOut = File.Create(args[4]);
            Algorithm.ApplyPatch(crc32, target, fsIn, fsOut, pos ?? fsIn.Length, overwrite);
        }
    }

    private static void HandleDumpCommand(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("[Error] 缺少参数。使用 --help 查看详细用法。");
            return;
        }

        string server = args[1].ToLower();
        string il2cppPath = args[2];
        string metadataPath = args[3];
        string outputPath = args[4];

        if (server == "cn")
        {
            Console.WriteLine("[Process] 正在启动国服导出...");
            string[] cnArgs = { "--metadata", metadataPath, "--output", outputPath, "--profile" };
            ExportCore.Execute(cnArgs);
        }
        else
        {
            if (!File.Exists(il2cppPath) || !File.Exists(metadataPath))
            {
                Console.WriteLine("[Error] 指定的 il2cpp 或 metadata 文件不存在。");
                return;
            }

            string[] il2cppArgs = { "--bin", il2cppPath, "--metadata", metadataPath, "--select-outputs", "--cs-out", outputPath, "--must-compile" };
            Console.WriteLine($"[Process] 正在为 {server} 启动 Il2CppInspector...");
            Il2CppInspector.CLI.App.Main(il2cppArgs);
        }
    }
}
