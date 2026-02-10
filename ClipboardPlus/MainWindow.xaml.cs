using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;

namespace ClipboardPlus
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private HotkeyManager _hotkeys;
    private int _typeOutHotkeyId;
    private IKeyboardMouseEvents m_Events;
    public ObservableCollection<StringItem> Items { get; set; }
    public List<Keys> HotKeys { get; set; }
    public string StoreSelectedItem { get; set; }
    public int StoreTypingSpeed { get; set; }

    // Default Hotkeys
    private Keys _key1 = Keys.LControlKey;
    private Keys _key2 = Keys.LMenu;
    private Keys _key3 = Keys.V;
    private Keys _killSwitch = Keys.Escape;
    private volatile bool _killSwitchPressed;

    private StringItem _selectedItem;
    private int _typingSpeed = 50;

    private NotifyIcon _notifyIcon;
    private bool _isClosing = false;
    public MainWindow()
    {
      InitializeComponent();
      InitializeSystemTrayIcon();
      SubscribeGlobal();
      DataContext = this; // Sets data context to the window itself

      Items = new ObservableCollection<StringItem>();
      HotKeys = new List<Keys>(3);
    }
    private void Subscribe(IKeyboardMouseEvents events)
    {
      m_Events = events;
      m_Events.KeyDown -= OnKeyDown;
      m_Events.KeyDown += OnKeyDown;
      //m_Events.KeyUp += OnKeyUp;
      m_Events.KeyPress += HookManager_KeyPress;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      // Get the screen working area dimensions in DIPs
      var screenHeight = SystemParameters.WorkArea.Height;
      var screenWidth = SystemParameters.WorkArea.Width;

      // Set the window's position to the bottom-right corner
      Left = screenWidth - ActualWidth;
      Top = screenHeight - ActualHeight;

      // Ensure startup location is manual for these coordinates to be used
      WindowStartupLocation = WindowStartupLocation.Manual;

      // Set up hotkey registration with windows
      _hotkeys = new HotkeyManager(this);
      _hotkeys.HotkeyPressed += OnHotkeyPressed;

      RegisterDefaultHotkey();
    }
    private void InitializeSystemTrayIcon()
    {
      _notifyIcon = new NotifyIcon();
      // You need to replace "PathToYourIcon.ico" with an actual .ico file resource in your project
      // Example if the icon is added as a resource file:
      // _notifyIcon.Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/YourAppName;component/Assets/appicon.ico")).Stream);
      _notifyIcon.Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/ClipboardPlus;component/Resources/clipboardplus.ico")).Stream);

      // Or use a default system icon for testing:
      //_notifyIcon.Icon = SystemIcons.Application;

      _notifyIcon.Text = "Clipboard+"; // Tooltip text
      _notifyIcon.Visible = true;

      // Handle double-click on the tray icon to restore the window
      _notifyIcon.DoubleClick += (s, args) => ShowWindow();

      // Optional: Add a Context Menu for the tray icon
      var contextMenu = new ContextMenu();
      contextMenu.MenuItems.Add(new MenuItem("Open", (s, a) => ShowWindow()));
      contextMenu.MenuItems.Add(new MenuItem("-")); // Separator
      contextMenu.MenuItems.Add(new MenuItem("Exit", (s, a) => ExitApplication()));
      _notifyIcon.ContextMenu = contextMenu;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
      if (WindowState == WindowState.Minimized)
      {
        // When minimized, hide the window from the taskbar and screen
        ShowInTaskbar = false;
        Visibility = Visibility.Hidden;
      }
    }
    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!_isClosing)
      {
        // If the user clicks 'X' (Close button), minimize instead of closing immediately
        e.Cancel = true; // Cancel the actual close operation
        WindowState = WindowState.Minimized;
      }
      else
      {
        // If the flag is set (e.g. from the Exit menu item), dispose the icon properly
        _notifyIcon.Dispose();
      }
    }
    private void RegisterDefaultHotkey()
    {
      const uint MOD_CONTROL = 0x0002;
      const uint MOD_ALT = 0x0001;

      uint key = (uint)KeyInterop.VirtualKeyFromKey(Key.V);

      _typeOutHotkeyId = _hotkeys.Register(MOD_CONTROL | MOD_ALT, key);
    }
    private async void OnHotkeyPressed(int id)
    {
      if (id != _typeOutHotkeyId)
        return;

      if (StoreSelectedItem == null && Items.Count > 0)
        StoreSelectedItem = Items[0].Value.TrimEnd('\n', '\t', '\r'); // Remove trailing new lines or tabs to prevent sending characters in a login screen or form

      if (StoreSelectedItem == null)
        return;

      _killSwitchPressed = false; // Reset before starting

      await Task.Delay(100);
      for (int i = 0; i < StoreSelectedItem.Length; i++)
      {
        if (_killSwitchPressed)
          break; // Interrupt typing if kill switch is pressed
        
        await Task.Delay(TypingSpeed);
        SendKeys.SendWait(EscapeForSendKeys(StoreSelectedItem[i]));
      }
    }
    private void Clear()
    {
      Items.Clear();
    }
    private void ShowWindow()
    {
      Visibility = Visibility.Visible;
      ShowInTaskbar = true;
      WindowState = WindowState.Normal;
      Activate(); // Bring the window to the front
    }
    protected override void OnClosed(EventArgs e)
    {
      if (_hotkeys != null)
        _hotkeys.Dispose();

      base.OnClosed(e);
    }
    private void ExitApplication()
    {
      _isClosing = true; // Set the flag so MainWindow_Closing performs a real exit
      System.Windows.Application.Current.Shutdown();
    }
    public StringItem SelectedItem
    {
      get => _selectedItem;
      set
      {
        if (_selectedItem != value)
        {
          _selectedItem = value;
          OnPropertyChanged(nameof(SelectedItem));
          StoreSelectedItem = _selectedItem?.Value;
        }
      }
    }
    public int TypingSpeed
    {
      get => _typingSpeed;
      set
      {
        if (_typingSpeed != value)
        {
          _typingSpeed = value;
          OnPropertyChanged(nameof(TypingSpeed));
          StoreTypingSpeed = _typingSpeed;
        }
      }
    }
    public Keys Key1
    {
      get => _key1;
      set
      {
        if (_key1 != value)
        {
          _key1 = value;
          OnPropertyChanged(nameof(Key1));
          UpdateHotkeyRegistration();
        }
      }
    }

    public Keys Key2
    {
      get => _key2;
      set
      {
        if (_key2 != value)
        {
          _key2 = value;
          OnPropertyChanged(nameof(Key2));
          UpdateHotkeyRegistration();
        }
      }
    }

    public Keys Key3
    {
      get => _key3;
      set
      {
        if (_key3 != value)
        {
          _key3 = value;
          OnPropertyChanged(nameof(Key3));
          UpdateHotkeyRegistration();
        }
      }
    }
    private uint ConvertToModifier(Keys key)
    {
      switch (key)
      {
        case Keys.LControlKey:
        case Keys.RControlKey:
        case Keys.Control:
          return 0x0002; // MOD_CONTROL

        case Keys.LMenu:
        case Keys.RMenu:
        case Keys.Menu:
          return 0x0001; // MOD_ALT

        case Keys.LShiftKey:
        case Keys.RShiftKey:
        case Keys.Shift:
          return 0x0004; // MOD_SHIFT

        case Keys.LWin:
        case Keys.RWin:
          return 0x0008; // MOD_WIN -- Can't register hotkeys to windows key

        default:
          return 0;
      }
    }
    private void UpdateHotkeyRegistration()
    {
      if (_hotkeys == null)
        return;

      // Remove old hotkey
      if (_typeOutHotkeyId != 0)
        _hotkeys.Unregister(_typeOutHotkeyId);

      uint mod1 = ConvertToModifier(Key1);
      uint mod2 = ConvertToModifier(Key2);
      uint modifiers = mod1 | mod2;
      uint key = (uint)Key3;

      try
      {
        _typeOutHotkeyId = _hotkeys.Register(modifiers, key);
      }
      catch (InvalidOperationException ex)
      {
        // Optionally notify the user or revert to previous hotkey
        System.Windows.MessageBox.Show("Failed to register hotkey: " + ex.Message + ". Please restart application.");
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    
    private void HookManager_KeyPress(object sender, KeyPressEventArgs e)
    {
      Log(string.Format("KeyPress \t {0}\n", e.KeyChar));
    }

    //private void OnKeyUp(object sender, KeyEventArgs e)
    //{
    //  Use when something needs to happen after a key is released
    //}
    private async void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
      if (e.Control && e.KeyCode == Keys.C)
      {
        // Ctrl+C detected
        await Task.Delay(50); // Let windows clipboard catch up
        // Clipboard capture logic
        string ClipboardText = Clipboard.GetText();
        bool exists = false;
        for (int i = 0; i < Items.Count; i++)
        {
          if (Items[i].Value.Equals(ClipboardText))
          {
            exists = true;
            break;
          }
        }
        if (!exists)
        {
          if (Items.Count >= 5)
            Items.RemoveAt(4);
          Items.Insert(0, new StringItem(ClipboardText));
        }
      }
      else if (e.KeyCode == Keys.Oemtilde)
      {
        Clear();
        return;
      }
      else if (e.KeyCode == _killSwitch)
      {
        _killSwitchPressed = true;
        return;
      }
    }
    public void SubscribeGlobal()
    {
      Unsubscribe();
      Subscribe(Hook.GlobalEvents());
    }

    private void GlobalHookKeyPress(object sender, KeyPressEventArgs e)
    {
      Console.WriteLine("KeyPress: \t{0}", e.KeyChar);
    }

    public void Unsubscribe()
    {
      if (m_Events == null) return;
      m_Events.KeyDown -= OnKeyDown;
      //m_Events.KeyUp -= OnKeyUp;
      m_Events.KeyPress -= HookManager_KeyPress;

      m_Events.Dispose();
      m_Events = null;
    }

    private void Log(string message)
    {
      if (!IsLoaded) return;
      //LogBlock.Text = message;
    }

    private static string EscapeForSendKeys(char c)
    {
      // SendKeys special characters: + ^ % ~ ( ) { } [ ]
      switch (c)
      {
        case '+':
        case '^':
        case '%':
        case '~':
        case '(':
        case ')':
        case '{':
        case '}':
        case '[':
        case ']':
          return "{" + c + "}";
        // TODO: Figure out a good way to handle multiple line breaks in copied text
        //case '\r':
        //  return string.Empty;
        default:
          return c.ToString();
      }
    }
  }
}