using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace FPBetaVer_1
{
    public class FFmpegRunner
    {
        public string FFmpegPath { get; set; } = "ffmpeg.exe";

        public event Action<string> OutputDataReceived;
        public event Action<int> ProcessCompleted;

        public async Task RunAsync(string inputFile, string outputFile, string arguments)
        {
            if (!File.Exists(FFmpegPath))
                throw new Exception($"ffmpeg не найден по пути: {FFmpegPath}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FFmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<bool>();

            process.Exited += (s, e) =>
            {
                tcs.TrySetResult(true);
                ProcessCompleted?.Invoke(process.ExitCode);
                process.Dispose();
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    OutputDataReceived?.Invoke(e.Data);
            };

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    OutputDataReceived?.Invoke(e.Data);
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                await tcs.Task;
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка запуска ffmpeg: " + ex.Message);
            }
        }
    }
}
