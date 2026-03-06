using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardPlus
{
  public class StringItem
  {
    public string Value { get; set; }
    private bool _isLocked;
    public bool IsLocked
    {
      get => _isLocked;
      set
      {
        if (_isLocked != value)
        {
          _isLocked = value;
          OnPropertyChanged(nameof(IsLocked));
        }
      }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public StringItem(string value)
    {
      Value = value;
      IsLocked = IsLocked;
    }
  }
}
