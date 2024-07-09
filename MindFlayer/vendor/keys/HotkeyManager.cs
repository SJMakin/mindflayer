using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace NHotkey.WindowsForms;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
    Justification = "This is a singleton; disposing it would break it")]
public class HotkeyManager : HotkeyManagerBase
{
    #region Singleton implementation

    public static HotkeyManager Current { get { return LazyInitializer.Instance; } }

    private static class LazyInitializer
    {
        static LazyInitializer() { }
        public static readonly HotkeyManager Instance = new HotkeyManager();
    }

    #endregion

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly MessageWindow _messageWindow;

    private HotkeyManager()
    {
        _messageWindow = new MessageWindow(this);
        _messageWindow.Show();
        SetHwnd(new WindowInteropHelper(_messageWindow).Handle);
    }

    public void AddOrReplace(string name, Key keys, bool noRepeat, EventHandler<HotkeyEventArgs> handler)
    {
        var flags = GetFlags(keys, noRepeat);
        var vk = unchecked((uint)(keys & ~(Key.LeftAlt | Key.RightAlt | Key.LeftCtrl | Key.RightCtrl | Key.LeftShift | Key.RightShift | Key.LWin | Key.RWin)));
        AddOrReplace(name, vk, flags, handler);
    }

    public void AddOrReplace(string name, Key keys, EventHandler<HotkeyEventArgs> handler)
    {
        AddOrReplace(name, keys, false, handler);
    }


    private static HotkeyFlags GetFlags(Key hotkey, bool noRepeat)
    {
        var noMod = hotkey & ~(Key.LeftAlt | Key.RightAlt | Key.LeftCtrl | Key.RightCtrl | Key.LeftShift | Key.RightShift | Key.LWin | Key.RWin);
        var flags = HotkeyFlags.None;
        if ((hotkey & Key.LeftAlt) == Key.LeftAlt || (hotkey & Key.RightAlt) == Key.RightAlt)
            flags |= HotkeyFlags.Alt;
        if ((hotkey & Key.LeftCtrl) == Key.LeftCtrl || (hotkey & Key.RightCtrl) == Key.RightCtrl)
            flags |= HotkeyFlags.Control;
        if ((hotkey & Key.LeftShift) == Key.LeftShift || (hotkey & Key.RightShift) == Key.RightShift)
            flags |= HotkeyFlags.Shift;
        if (noMod == Key.LWin || noMod == Key.RWin)
            flags |= HotkeyFlags.Windows;
        if (noRepeat)
            flags |= HotkeyFlags.NoRepeat;
        return flags;
    }


    class MessageWindow : Window
    {
        private readonly HotkeyManager _hotkeyManager;

        public MessageWindow(HotkeyManager hotkeyManager)
        {
            _hotkeyManager = hotkeyManager;
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }



        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            Hotkey hotkey;
            return _hotkeyManager.HandleHotkeyMessage(hwnd, msg, wParam, lParam, ref handled, out hotkey);
        }
    }
}
