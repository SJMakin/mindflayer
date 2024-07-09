using System.Windows.Input;

namespace NHotkey.WindowsForms;

static class Extensions
{
    public static bool HasFlag(this Key keys, Key flag)
    {
        return (keys & flag) == flag;
    }
}
