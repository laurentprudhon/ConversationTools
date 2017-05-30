using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fasttext
{
    class ExecutionResult
    {
        public int ReturnCode { get; set; }
        public string OutputMessage { get; set; }
        public string ErrorMessage { get; set; }

        public TimeSpan TotalProcessorTime { get; set; }
    }

    class ExecutableLauncher
    {
        public static ExecutionResult ExecuteCommand(string executablePath, string args, string workingDir)
        {
            var process = LaunchCommand(executablePath, args, workingDir);
            return WaitForCommandExit(process);
        }

        public static Process LaunchCommand(string executablePath, string args, string workingDir)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = workingDir;

            startInfo.FileName = executablePath;
            startInfo.Arguments = args;

            startInfo.CreateNoWindow = true;
            startInfo.ErrorDialog = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = Process.Start(startInfo);
            return process;
        }

        public static void SendInputLine(Process process, string inputLine)
        {
            process.StandardInput.WriteLine(inputLine);
        }

        public static string ReadOutputLine(Process process)
        {
            var outputLine = process.StandardOutput.ReadLine();
            return outputLine;
        }

        public static ExecutionResult WaitForCommandExit(Process process)
        { 
            process.WaitForExit();

            var result = new ExecutionResult();
            result.ReturnCode = process.ExitCode;
            result.TotalProcessorTime = process.TotalProcessorTime;
            result.OutputMessage = process.StandardOutput.ReadToEnd();
            result.ErrorMessage = process.StandardError.ReadToEnd();

            return result;
        }

        public static void Kill(Process process)
        {
            process.Kill();
        }
    }
}
