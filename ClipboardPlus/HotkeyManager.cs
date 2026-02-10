using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

public class HotkeyManager : IDisposable
{
  [DllImport("user32.dll")]
  private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

  [DllImport("user32.dll")]
  private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

  private readonly IntPtr _windowHandle;
  private readonly HwndSource _source;
  private int _currentId = 0;

  public event Action<int> HotkeyPressed;

  public HotkeyManager(Window window)
  {
    _windowHandle = new WindowInteropHelper(window).Handle;
    _source = HwndSource.FromHwnd(_windowHandle);
    _source.AddHook(WndProc);
  }

  public int Register(uint modifiers, uint key)
  {
    _currentId++;

    if (!RegisterHotKey(_windowHandle, _currentId, modifiers, key))
      throw new InvalidOperationException("Failed to register hotkey");

    return _currentId;
  }

  public void Unregister(int id)
  {
    UnregisterHotKey(_windowHandle, id);
  }

  public void UnregisterAll()
  {
    for (int id = 1; id <= _currentId; id++)
      UnregisterHotKey(_windowHandle, id);
  }

  private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
  {
    const int WM_HOTKEY = 0x0312;

    if (msg == WM_HOTKEY)
    {
      int id = wParam.ToInt32();
      if (HotkeyPressed != null)
        HotkeyPressed(id);

      handled = true;
    }

    return IntPtr.Zero;
  }

  public void Dispose()
  {
    UnregisterAll();
    _source.RemoveHook(WndProc);
  }
}