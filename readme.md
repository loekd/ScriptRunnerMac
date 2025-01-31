# Script runner for MacOs

- Starts a Ghostty terminal, or uses an existing one.
- Reads commands from a text file.
- Prints the commands to the terminal and executes them one by one, by clicking F10.

Commands will only print and execute when the terminal has focus. 

## Example
Run this command to test the tool:

```
./ScriptRunnerMac -f /Users/you/projects/ScriptRunnerMac/demo.md
```

Press `F10` to display a command from the file. You can modify it if needed.
Press `F10` again to execute the command.

When all commands are executed, pressing `F10` will quickly flash 'Finished!'.

It is up to you to wait for long running tasks to complete.

## Comments & whitespace
The tool will ignore lines that start with a `#` character, and empty lines.

## Special characters

Adding this line `$CTRL+C` to the file, will send CTRL+C to the terminal. This can be used to abort a running command.

Press the `Escape` key to terminate the Script Runner tool.