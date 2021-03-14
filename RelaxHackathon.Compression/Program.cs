using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text;
using System.Diagnostics;

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
            var lines = await File.ReadAllLinesAsync(args[0]).ConfigureAwait(false);
            var output = new List<string>();
            foreach (var (tag, content) in GetContent(lines))
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
            await File.WriteAllLinesAsync(args[1], lines).ConfigureAwait(false);

            return 0;
        }

        private static IEnumerable<(string? tag, string content)> GetContent(string[] lines)
        {
            var tagger = new Regex("^(?<tag>\\w+)\\s+(?<content>.*)$", RegexOptions.Compiled);
            var multiliner = new Regex("^\\w+\\s+<<(?<multi>.+)$", RegexOptions.Compiled);
            for (int i = 0; i< lines.Length; ++i)
            {
                var line = lines[i].TrimStart();
                if (line.StartsWith('#'))
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
                            line += lines[i].TrimStart();
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
            await File.WriteAllTextAsync(".data.tmp", content).ConfigureAwait(false);
            var start = new ProcessStartInfo
            {
                Arguments = "bash_minifier/minifier.py .data.tmp",
                FileName = "python",
                RedirectStandardOutput = true,
            };
            using var process = Process.Start(start);
            if (process is null)
                return content;
            await process.WaitForExitAsync().ConfigureAwait(false);
            return await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}
