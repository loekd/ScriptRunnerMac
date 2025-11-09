using System.Diagnostics;
using SharpHook;

namespace ScriptRunnerMac
{
    internal static class Program
    {
        private static EventSimulator _keyboard;
        private static Process _process;
        private static bool _isRunningStatement;
        private static int _currentLineIndex;
        private static KeyboardHook _hook;

        private static string[] _lines;
        private static string _workingDir;
        private static string _inputFile;

        static async Task Main(string[] args)
        {
            _keyboard = new EventSimulator();
            _hook = new KeyboardHook();
            _hook.EscapePressed += Terminate;
            _hook.HotKeyPressed += HotKeyPressed;
            _hook.RefreshPressed += RefreshPressed;
            _inputFile = args[^1];
            _lines = ReadCommands();

            WriteHorizontalLine();

            //run a process that executes the command and redirect input back here:
            _workingDir = Path.GetDirectoryName(_inputFile) ?? Environment.CurrentDirectory;
            _process = RunSubprocess();
            if (_process is null)
            {
                Console.WriteLine("Failed to start subprocess");
                return;
            }

            //exit when sub-process exits:
            _process.Exited += (sender, e) =>
            {
                Console.WriteLine("Subprocess exited");
                _process.Dispose();
                _hook.Dispose();
                Environment.Exit(0);
            };

            var completed = _hook.SetHook();
            _hook.Dispose();
            await completed;
        }

        private static void RefreshPressed(object sender, EventArgs e)
        {
            if (!ActiveWindow.IsProcessMainWindowFocused(_process.Id))
            {
                Console.WriteLine("Ignoring refresh - terminal not focused.");
                return;
            }
            //Reload file
            _lines = ReadCommands();
            _currentLineIndex = _currentLineIndex > 0 ? _currentLineIndex - 1 : 0;
            _isRunningStatement = false;
            Console.WriteLine("Reloaded file");
        }

        private static Process RunSubprocess()
        {
            var subProcess = Process.GetProcessesByName("Ghostty").OrderByDescending(p => p.StartTime).FirstOrDefault();
            if (subProcess == null)
            {
                //launch shell
                var psi = new ProcessStartInfo(@"open")
                {
                    WorkingDirectory = _workingDir,
                    UseShellExecute = true,
                    Arguments = "-a Ghostty.app -n",

                };
                Process.Start(psi);
                Thread.Sleep(300);
            }

            _process = Process.GetProcessesByName("Ghostty").OrderByDescending(p => p.StartTime).FirstOrDefault();
            if (_process == null || _process.HasExited)
                return null;
            
            _process.EnableRaisingEvents = true;
            return _process;
        }

        private static string[] ReadCommands()
        {
            if (!File.Exists(_inputFile))
            {
                Console.Error.WriteLine("File not found: {0}", _inputFile);
                Environment.Exit(1);
            }
            //ignore empty lines and comments
            var content = File.ReadAllLines(_inputFile);
            var filtered = content
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                .ToArray();
            
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Running {0} commands from {1}", filtered.Length, _inputFile);
            Console.ForegroundColor = ConsoleColor.White;
            
            return filtered;
        }

        private static void HotKeyPressed(object sender, EventArgs e)
        {
            if (!ActiveWindow.IsProcessMainWindowFocused(_process.Id))
            {
                Console.WriteLine("Ignoring hotkey - terminal not focused.");
                return;
            }
            if (_isRunningStatement && _currentLineIndex > 0)
            {
                ExecuteCommandInTerminal();
                _isRunningStatement = false;
            }
            else
            {
                _isRunningStatement = true;

                //check if we are at the end of the file
                if (_currentLineIndex >= _lines.Length)
                {
                    //KillSubprocess();
                    SendFinishedToTerminal();
                    return;
                }

                string line = _lines[_currentLineIndex++];
                string nextLine = _lines.Length > _currentLineIndex ? _lines[_currentLineIndex] : "exit";

                if (_currentLineIndex == 1)
                {
                    ShowNextLine(line);
                }

                SendCommandToTerminal(line);
                ShowNextLine(nextLine);
            }
        }

        private static void ExecuteCommandInTerminal()
        {
            //execute the command
            _keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcEnter);
            Thread.Sleep(10);
            Console.WriteLine($"Executed line {_currentLineIndex}");
        }

        private static void Terminate(object sender, EventArgs e)
        {
            if (!ActiveWindow.IsProcessMainWindowFocused(_process.Id))
            {
                Console.WriteLine("Ignoring hotkey - terminal not focused.");
                return;
            }
            
            _hook.Dispose();
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
            Console.WriteLine($"Next command: {nextLine}".PadRight(Console.WindowWidth, ' '));
            WriteHorizontalLine();

            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.ForegroundColor = oldColor;
        }

        private static void SendCommandToTerminal(string line)
        {
            switch (line)
            {
                case "$CTRL+C":
                    _keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcLeftControl);
                    _keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcC);
                    _keyboard.SimulateKeyRelease(SharpHook.Native.KeyCode.VcC);
                    _keyboard.SimulateKeyRelease(SharpHook.Native.KeyCode.VcLeftControl);
                    break;
                case "$RETURN":
                    _keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcEnter);
                    _keyboard.SimulateKeyRelease(SharpHook.Native.KeyCode.VcEnter);
                    break;
                default:
                    _keyboard.SimulateTextEntry(line);
                    break;
            }
        }

        private static void WriteHorizontalLine()
        {
            Console.WriteLine("".PadRight(Console.WindowWidth, '-'));
        }

        private static void KillSubprocess()
        {
            Console.WriteLine("All commands executed");
            Thread.Sleep(2000);
            _process.Kill();
        }

        private static void SendFinishedToTerminal()
        {
            string message = "Finished!";
            _keyboard.SimulateTextEntry(message);
            Thread.Sleep(10);
            for (int i = 0; i < message.Length; i++)
            {
                _keyboard.SimulateKeyPress(SharpHook.Native.KeyCode.VcBackspace);
                Thread.Sleep(50);
            }
        }
    }
}
