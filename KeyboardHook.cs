using SharpHook;

namespace ScriptRunnerMac
{
    public sealed class KeyboardHook : IDisposable
    {
        private SimpleGlobalHook _hook;
        public event EventHandler<EventArgs> HotKeyPressed;
        public event EventHandler<EventArgs> EscapePressed;

       
        public bool SuppressOtherKeysPressed { get; internal set; }

        public KeyboardHook()
        {
        }

        public Task SetHook()
        {            
            _hook = new SimpleGlobalHook();
            _hook.KeyPressed += OnHookEvent;
            _hook.Run();
            //return _hook.RunAsync();                        
            return Task.CompletedTask;
        }

        private void OnHookEvent(object sender, KeyboardHookEventArgs e){
            if (e.Data.KeyCode == SharpHook.Native.KeyCode.VcF10)
            {
                e.SuppressEvent = true;
                //Console.WriteLine("F10 pressed");                
                OnHotKeyPressed();
            }
            else if (e.Data.KeyCode == SharpHook.Native.KeyCode.VcEscape)
            {
                e.SuppressEvent = true;
                //Console.WriteLine("Esc pressed");
                OnEscapePressed();
            }
        }

        private void OnHotKeyPressed()
        {
            HotKeyPressed?.Invoke(null, EventArgs.Empty);
        }

        private void OnEscapePressed()
        {
            EscapePressed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _hook?.Dispose();
        }

        public void Unhook()
        {
            Dispose();
        }      
    }   

    // internal static class Interop
    // {
    //     [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    //     public static extern void CFRunLoopRun();
    // }
}
