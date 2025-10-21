using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SharpHook;


namespace ScriptRunnerMac
{
    internal class Program
    {
        private static EventSimulator keyboard;
        private static Process process;
        private static bool isRunningStatement;
        private static int currentLineIndex;
        private static KeyboardHook hook;

        private static string[] lines;
        private static string workingDir;

        static async Task Main(string[] args)
        {
            keyboard = new EventSimulator();
            hook = new KeyboardHook();
            Console.WriteLine("Running!");
            hook.EscapePressed += Terminate;
            hook.HotKeyPressed += HotKeyPressed;            
            string file = args[^1];
            lines = ReadCommands(file);


            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Running {0} commands from {1}", lines.Length, file);
            Console.ForegroundColor = ConsoleColor.White;
            WriteHorizontalLine();

            //run a process that executes the command and redirect input back here:
            workingDir = Path.GetDirectoryName(file) ?? Environment.CurrentDirectory;
            process = RunSubprocess(workingDir);
            if (process == null)
            {
                Console.WriteLine("Failed to start subprocess");
                return;
            }

            //exit when subprocess exits:
            process.Exited += (sender, e) =>
            {
                Console.WriteLine("Subprocess exited");
                process.Dispose();
                hook.Dispose();
                Environment.Exit(0);
            };

            var completed = hook.SetHook();
            hook.Dispose();
            await completed;
        }

        private static Process RunSubprocess(string workingDir)
        {
            var process = Process.GetProcessesByName("Ghostty").OrderByDescending(p => p.StartTime).FirstOrDefault();
            if (process == null)
            {
                //launch shell
                var psi = new ProcessStartInfo(@"open")
                {
                    WorkingDirectory = workingDir,
                    UseShellExecute = true,
                    Arguments = "-a Ghostty.app -n",

                };
                var shell = Process.Start(psi);
                Thread.Sleep(300);
            }

            process = Process.GetProcessesByName("Ghostty").OrderByDescending(p => p.StartTime).FirstOrDefault();
            if (process != null)
            {
                process.EnableRaisingEvents = true;
            }

            if (process.HasExited)
            {
                Console.Error.WriteLine("Subprocess exited");
                return null;
            }
            return process;
        }

        private static string[] ReadCommands(string file)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine("File not found: {0}", file);
                Environment.Exit(1);
            }
            //ignore empty lines and comments
            var lines = File.ReadAllLines(file);
            return lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#')).ToArray();
        }

        private static void HotKeyPressed(object sender, EventArgs e)
        {
            if (!ActiveWindow.IsProcessMainWindowFocused(process.Id))
            {
                Console.WriteLine("Ignoring hotkey - terminal not focused.");
                return;
            }
            if (isRunningStatement && currentLineIndex > 0)
            {
                ExecuteCommandInTerminal();
                isRunningStatement = false;
            }
            else
            {
                isRunningStatement = true;

                //check if we are at the end of the file
                if (currentLineIndex >= lines.Length)
                {
                    //KillSubprocess();
                    SendFinishedToTerminal();
                    return;
                }

                string line = lines[currentLineIndex++];
                string nextLine = lines.Length > currentLineIndex ? lines[currentLineIndex] : "exit";

                if (currentLineIndex == 1)
                {
                    ShowNextLine(line);
                }

                SendCommandToTerminal(line);
                ShowNextLine(nextLine);
            }
        }

        private static void ExecuteCommandInTerminal()
        {
            //remove the F10 char
            // keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcBackspace);
            // Thread.Sleep(10);
            //execute the command
            keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcEnter);
            Thread.Sleep(10);
        }

        private static void Terminate(object sender, EventArgs e)
        {
            if (!ActiveWindow.IsProcessMainWindowFocused(process.Id))
            {
                Console.WriteLine("Ignoring hotkey - terminal not focused.");
                return;
            }
            
            hook.Dispose();
            Console.WriteLine("Terminating");
            Environment.Exit(0);
        }

        private static void ShowNextLine(string nextLine)
        {
            int cursorLeft = Console.CursorLeft;
            int cursorTop = Console.CursorTop;
            var oldColor = Console.ForegroundColor;
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(string.Format("Next command: {0}", nextLine).PadRight(Console.WindowWidth, ' '));
            WriteHorizontalLine();

            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.ForegroundColor = oldColor;
        }

        private static void SendCommandToTerminal(string line)
        {
            switch (line)
            {
                case "$CTRL+C":
                    keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcLeftControl);
                    keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcC);                    
                    keyboard.SimulateKeyRelease(SharpHook.Native.KeyCode.VcC);
                    keyboard.SimulateKeyRelease(SharpHook.Native.KeyCode.VcLeftControl);
                    break;
                case "$RETURN":
                    keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcEnter);
                    keyboard.SimulateKeyRelease(SharpHook.Native.KeyCode.VcEnter);
                    break;
                default:
                    keyboard.SimulateTextEntry(line);
                    break;
            }
            //type command: (updateable)
            //remove the F10 chars before typing a line
            // keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcBackspace);
            // Thread.Sleep(10);
            // keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcBackspace);
            // Thread.Sleep(10);
            //use virtual keyboard to type the command (no enter)
            // keyboard.SimulateTextEntry(line);
            //Thread.Sleep(10);
        }

        private static void WriteHorizontalLine()
        {
            Console.WriteLine("".PadRight(Console.WindowWidth, '-'));
        }

        private static void KillSubprocess()
        {
            Console.WriteLine("All commands executed");
            Thread.Sleep(2000);
            process.Kill();
        }

        private static void SendFinishedToTerminal()
        {
            string message = "Finished!";
            keyboard.SimulateTextEntry(message);
            Thread.Sleep(10);
            for (int i = 0; i < message.Length; i++)
            {
                keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcBackspace);
                Thread.Sleep(50);
            }
        }
    }
}
