using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace RelaxHackathon.Compression
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("expected docker file and output file");
                return 1;
            }
            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("dockerfile not found");
                return 2;
            }
            try
            {
                var lines = new List<string>();
                await foreach (var line in ReadLines(args[0]).ConfigureAwait(false))
                    lines.Add(line);
                var output = new List<string>();
                foreach (var (tag, content) in GetContent(lines.ToArray()))
                {
                    switch (tag?.ToLowerInvariant())
                    {
                        case "entrypoint":
                            output.Add($"ENTRYPOINT {CompressJson(content)}");
                            break;
                        case "run":
                            output.Add($"RUN {await CompressShellAsync(content).ConfigureAwait(false)}");
                            break;
                        case null:
                            output.Add(content);
                            break;
                        default:
                            output.Add($"{tag.ToUpperInvariant()} {content.Trim()}");
                            break;
                    }
                }
                await WriteAllTest(args[1], string.Join('\n', output)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            return 0;
        }

        private static async Task WriteAllTest(string file, string content)
        {
            using var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            stream.SetLength(stream.Position);
        }

        private static async IAsyncEnumerable<string> ReadLines(string file)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                yield return line;
        }

        private static IEnumerable<(string? tag, string content)> GetContent(string[] lines)
        {
            var tagger = new Regex("^(?<tag>\\w+)\\s+(?<content>.*)$", RegexOptions.Compiled);
            var multiliner = new Regex("^\\w+\\s+<<(?<multi>.+)$", RegexOptions.Compiled);
            for (int i = 0; i< lines.Length; ++i)
            {
                var line = lines[i].TrimStart();
                if (line.StartsWith('#') || line == "")
                    continue;
                var multi = multiliner.Match(line);
                if (multi.Success)
                {
                    for (i++; i < lines.Length; i++)
                        if (!lines[i].Contains(multi.Groups["multi"].Value))
                            line += "\n" + lines[i];
                    line += lines[i];
                }
                else
                {
                    while (line.EndsWith('\\'))
                    {
                        line = line[..^1].TrimEnd();
                        if (i + 1 < lines.Length)
                            line += lines[++i].TrimStart();
                    }
                }
                line = line.TrimEnd();

                var match = tagger.Match(line);
                if (match.Success)
                    yield return (match.Groups["tag"].Value, match.Groups["content"].Value);
                else yield return (null, line);
            }
        }

        private static string CompressJson(string content)
        {
            try
            {
                var json = JsonDocument.Parse(content);
                using var stream = new MemoryStream();
                var writer = new Utf8JsonWriter(stream);
                json.WriteTo(writer);
                writer.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch
            {
                return content;
            }
        }

        private static async Task<string> CompressShellAsync(string content)
        {
            var dir = Assembly.GetExecutingAssembly().Location;
            dir = Path.GetDirectoryName(dir);
            if (dir is null)
                return content;
            var path = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", ".."));
            if (!Directory.Exists(path))
            {
                return content;
            }
            var data = Path.Combine(path, "tmp");
            await WriteAllTest(data, content).ConfigureAwait(false);
            path = Path.Combine(path, "bash_minifier", "minifier.py");
            var start = new ProcessStartInfo
            {
                Arguments = $"{path} {data}",
                FileName = "python2.7",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(start);
            if (process is null)
                return content;
            await process.WaitForExitAsync().ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                Console.Error.Write(error);
                return content;
            }
            return await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}
