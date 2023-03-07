using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDir
{
    class Program
    {
        public static bool zip = false;
        public static bool debug = false;
        public static string path = string.Empty;
        public static string outPath = string.Empty;
        public static string filter = "*.*";
        static void Main(string[] args)
        {
            CmdArgs cmdArgs = new CmdArgs(args);
            CmdArgsInterpreter interpreter = new CmdArgsInterpreter(cmdArgs);
            path = interpreter.Path;
            outPath = interpreter.Out;
            zip = interpreter.Zip;
            debug = interpreter.Debug;

            if (interpreter.Path == null || interpreter.Out == null)
            {
                Console.WriteLine(interpreter.Path);
                Console.WriteLine(interpreter.Out);

                Help();
                return;
            }

            if (interpreter.Filter != null) {
                filter = interpreter.Filter;
            }

            try
            {
                var fileInfos = EnumerateFiles(path);
                var sortedFileInfos = fileInfos.OrderByDescending(f => f.LastWriteTime);

                using (var writer = new StreamWriter(outPath))
                {
                    writer.WriteLine("File Name,Last Modified Time,File Path");

                    foreach (var fileInfo in sortedFileInfos)
                    {
                        writer.WriteLine($"{fileInfo.Name},{fileInfo.LastWriteTime},{fileInfo.FullName}");
                    }
                }

                if (zip)
                {
                    using (var compressedFileStream = new FileStream($"{outPath}.gz", FileMode.Create))
                    {
                        using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                        {
                            using (var fileStream = new FileStream(outPath, FileMode.Open))
                            {
                                fileStream.CopyTo(compressionStream);
                            }
                        }
                    }
                    Console.WriteLine($"File saved as {outPath}.gz");
                }
                else
                {
                    Console.WriteLine($"File saved as {outPath}");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }


        }

        static void Help()
        {
            Console.WriteLine("Usage: file-scanner.exe -path [directory] -out [output path] [-zip true/false] [-debug true/false] [-filter *.txt]");
        }
        static IEnumerable<FileInfo> EnumerateFiles(string targetDirectory)
        {
            var fileInfos = new ConcurrentBag<FileInfo>();
            var stack = new ConcurrentStack<string>();
            stack.Push(targetDirectory);

            var tasks = new List<Task>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (stack.TryPop(out var currentDirectory))
                    {
                        try
                        {
                            if (debug)
                            {
                                Console.WriteLine($"Scanning directory: {currentDirectory}");
                            }

                            foreach (var fileName in Directory.EnumerateFiles(currentDirectory, filter))
                            {
                                fileInfos.Add(new FileInfo(fileName));
                            }

                            foreach (var subdirectory in Directory.EnumerateDirectories(currentDirectory))
                            {
                                stack.Push(subdirectory);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            return fileInfos;
        }
    }


    public class CmdArgs
    {
        private readonly Dictionary<string, string> _cmdDictionary;

        public CmdArgs(string[] args)
        {
            _cmdDictionary = ParseCmdArgs(args);
        }

        public string GetValue(string key)
        {
            return _cmdDictionary.ContainsKey(key.ToLower()) ? _cmdDictionary[key.ToLower()] : null;
        }

        private Dictionary<string, string> ParseCmdArgs(string[] args)
        {
            var cmdDictionary = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    string key = args[i].Substring(1).ToLower();
                    string value = string.Empty;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        value = args[i + 1];
                        i++;
                    }
                    cmdDictionary[key] = value;
                }
            }
            return cmdDictionary;
        }
    }

    public class CmdArgsInterpreter
    {
        private readonly CmdArgs _cmdArgs;

        public CmdArgsInterpreter(CmdArgs cmdArgs)
        {
            _cmdArgs = cmdArgs;
        }

        public string Path => _cmdArgs.GetValue("path");

        public string Out => _cmdArgs.GetValue("out");

        public bool Zip => GetBoolValue("zip");

        public bool Debug => GetBoolValue("debug");

        public string Filter => _cmdArgs.GetValue("filter");

        private bool GetBoolValue(string key)
        {
            string value = _cmdArgs.GetValue(key);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            return bool.TryParse(value, out bool result) && result;
        }
    }

}