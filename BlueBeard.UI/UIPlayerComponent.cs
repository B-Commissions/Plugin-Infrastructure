using System.Collections.Generic;
using UnityEngine;

namespace BlueBeard.UI;

public class UIPlayerComponent : MonoBehaviour
{
    public IUI CurrentUI { get; set; }
    public IUIScreen CurrentScreen { get; set; }
    public IUIDialog CurrentDialog { get; set; }
    public bool IsOpen { get; set; }

    /// <summary>
    /// Custom state storage -- screens/dialogs can store arbitrary data here.
    /// Keys are namespaced by convention (e.g., "members.page", "ranks.selectedId").
    /// </summary>
    public Dictionary<string, object> State { get; } = new();

    public void Reset()
    {
        CurrentUI = null;
        CurrentScreen = null;
        CurrentDialog = null;
        IsOpen = false;
        State.Clear();
    }
}
