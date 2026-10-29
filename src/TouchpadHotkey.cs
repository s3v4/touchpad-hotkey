using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

// Windows Precision Touchpad toggle: CTRL + WIN + F24 (Microsoft documented).
internal sealed class TouchpadHotkey : ApplicationContext
{
    const int Id = 0x5421;
    readonly HotkeyWindow window;
    readonly NotifyIcon tray;
    readonly System.Windows.Forms.Timer timer;
    bool waiting;
    int ticks;
    readonly string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TouchpadHotkey.log");

    [DllImport("user32.dll", SetLastError=true)] static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError=true)] static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public UNION data; }
    [StructLayout(LayoutKind.Explicit)] struct UNION {
        [FieldOffset(0)] public KEYBDINPUT keyboard;
        [FieldOffset(0)] public MOUSEINPUT mouse;
    }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int x,y; public uint data, flags, time; public UIntPtr extra; }
    static INPUT Key(ushort vk, bool up) { return new INPUT { type=1, data=new UNION { keyboard=new KEYBDINPUT { vk=vk, flags=up ? 2u : 0u } } }; }
    void Log(string text) { try { File.AppendAllText(log, DateTime.Now.ToString("s") + " " + text + Environment.NewLine); } catch {} }

    sealed class HotkeyWindow : NativeWindow, IDisposable {
        public Action Pressed;
        public HotkeyWindow() { CreateHandle(new CreateParams { Parent=new IntPtr(-3) }); }
        protected override void WndProc(ref Message m) { if(m.Msg==0x312 && m.WParam.ToInt32()==Id && Pressed!=null) Pressed(); base.WndProc(ref m); }
        public void Dispose() { DestroyHandle(); }
    }
    TouchpadHotkey() {
        window=new HotkeyWindow();
        if(!RegisterHotKey(window.Handle, Id, 0x4003, 0x20)) {
            int error=Marshal.GetLastWin32Error();
            window.Dispose();
            throw new InvalidOperationException("Cannot register Ctrl+Alt+Space. Win32 error " + error);
        }
        var menu=new ContextMenuStrip();
        menu.Items.Add("Открыть настройки тачпада", null, delegate { System.Diagnostics.Process.Start("ms-settings:devices-touchpad"); });
        menu.Items.Add("Выход", null, delegate { ExitThread(); });
        tray=new NotifyIcon { Icon=SystemIcons.Application, Text="Тачпад: Ctrl + Alt + Пробел", ContextMenuStrip=menu, Visible=true };
        timer=new System.Windows.Forms.Timer { Interval=40 };
        window.Pressed=delegate { if(!waiting) { waiting=true; ticks=0; timer.Start(); } };
        timer.Tick+=delegate {
            // Wait until the physical hotkey is released to avoid sending Alt+Win chords.
            if((GetAsyncKeyState(0x11)&0x8000)!=0 || (GetAsyncKeyState(0x12)&0x8000)!=0 || (GetAsyncKeyState(0x20)&0x8000)!=0) {
                if(++ticks>250) { timer.Stop(); waiting=false; Log("Hotkey release timed out"); }
                return;
            }
            timer.Stop(); waiting=false;
            INPUT[] input={Key(0x11,false),Key(0x5B,false),Key(0x87,false),Key(0x87,true),Key(0x5B,true),Key(0x11,true)};
            uint sent=SendInput((uint)input.Length,input,Marshal.SizeOf(typeof(INPUT)));
            Log("Toggle sent: " + sent + "/6");
            if(sent!=input.Length) tray.ShowBalloonTip(3000,"Тачпад","Не удалось переключить тачпад. Откройте настройки через меню значка.",ToolTipIcon.Warning);
        };
        Log("Started; Ctrl+Alt+Space registered");
    }
    protected override void ExitThreadCore() {
        timer.Stop(); timer.Dispose(); UnregisterHotKey(window.Handle,Id); window.Dispose(); tray.Visible=false; tray.Dispose(); base.ExitThreadCore();
    }
    [STAThread] static void Main() {
        bool created;
        using(var mutex=new Mutex(true,"Local\\CodexTouchpadHotkey",out created)) {
            if(!created) return;
            Application.EnableVisualStyles();
            try { Application.Run(new TouchpadHotkey()); }
            catch(Exception ex) { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"TouchpadHotkey.log"),ex.ToString()+Environment.NewLine); MessageBox.Show(ex.Message,"Touchpad hotkey"); }
        }
    }
}
