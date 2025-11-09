using SharpHook;

namespace ScriptRunnerMac
{
    public sealed class KeyboardHook : IDisposable
    {
        private SimpleGlobalHook _hook;
        public event EventHandler<EventArgs> HotKeyPressed;
        
        public event EventHandler<EventArgs> RefreshPressed;
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

        private void OnHookEvent(object sender, KeyboardHookEventArgs e)
        {
            switch (e.Data.KeyCode)
            {
                case SharpHook.Native.KeyCode.VcF10:
                    e.SuppressEvent = true;              
                    OnHotKeyPressed();
                    break;
                case SharpHook.Native.KeyCode.VcF5:
                    e.SuppressEvent = true;
                    OnRefreshPressed();
                    break;
                case SharpHook.Native.KeyCode.VcEscape:
                    e.SuppressEvent = true;
                    OnEscapePressed();
                    break;
            }
        }
        
        private void OnRefreshPressed()
        {
            RefreshPressed?.Invoke(null, EventArgs.Empty);
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
}
