# Examples

This page shows a complete multi-screen UI implementation with pagination, a modal dialog, async data loading, and common `EffectManager` patterns.

---

## FactionUI -- A Complete Multi-Screen Example

This example builds a faction management panel with two screens (Overview and Members), a confirmation dialog for kicking members, pagination, search, and async data loading.

### File Structure

```
FactionPlugin/
  FactionPlugin.cs          -- plugin entry point
  UI/
    FactionUI.cs             -- IUI implementation
    FactionOverviewScreen.cs -- IUIScreen: faction stats
    FactionMembersScreen.cs  -- IUIScreen: member list with pagination
    ConfirmKickDialog.cs     -- IUIDialog: confirm before kicking
  Commands/
    FactionCommand.cs        -- opens the UI
```

---

### FactionUI.cs -- The Top-Level Container

```csharp
using BlueBeard.UI;
using SDG.Unturned;

public class FactionUI : IUI
{
    public string Id => "faction";
    public ushort EffectId => 50600;
    public short EffectKey => (short)EffectId;
    public IUIScreen[] Screens => new IUIScreen[] { OverviewScreen, MembersScreen };
    public IUIScreen DefaultScreen => OverviewScreen;

    public FactionOverviewScreen OverviewScreen { get; } = new();
    public FactionMembersScreen MembersScreen { get; } = new();

    private readonly UIManager _uiManager;

    public FactionUI(UIManager uiManager)
    {
        _uiManager = uiManager;
    }

    public void OnOpened(UIContext ctx)
    {
        // Set the faction name in the header (visible on all screens)
        string factionName = GetFactionName(ctx.Player.CSteamID.m_SteamID);
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/FactionName", factionName);

        // Highlight the default tab
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/Tab_Overview_Active", true);
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/Tab_Members_Active", false);
    }

    public void OnClosed(UIContext ctx)
    {
        // No cleanup needed
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        // 1. Global buttons
        switch (buttonName)
        {
            case "Faction_Close":
                _uiManager.CloseUI(ctx.Player);
                return;

            case "Faction_Tab_Overview":
                _uiManager.SetScreen(ctx.Player, OverviewScreen);
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/Header/Tab_Overview_Active", true);
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/Header/Tab_Members_Active", false);
                return;

            case "Faction_Tab_Members":
                _uiManager.SetScreen(ctx.Player, MembersScreen);
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/Header/Tab_Overview_Active", false);
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/Header/Tab_Members_Active", true);
                return;
        }

        // 2. Dialog takes priority
        if (ctx.Component.CurrentDialog != null)
        {
            ctx.Component.CurrentDialog.OnButtonPressed(ctx, buttonName);
            return;
        }

        // 3. Route to current screen
        ctx.Component.CurrentScreen?.OnButtonPressed(ctx, buttonName);
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        if (ctx.Component.CurrentDialog != null)
        {
            ctx.Component.CurrentDialog.OnTextSubmitted(ctx, inputName, text);
            return;
        }

        ctx.Component.CurrentScreen?.OnTextSubmitted(ctx, inputName, text);
    }

    private string GetFactionName(ulong steamId)
    {
        // Your data access logic here
        return "The Iron Guard";
    }
}
```

---

### FactionOverviewScreen.cs -- A Simple Screen

```csharp
using BlueBeard.UI;
using SDG.Unturned;

public class FactionOverviewScreen : IUIScreen
{
    public string Id => "overview";
    public IUIDialog[] Dialogs => System.Array.Empty<IUIDialog>();

    public void OnShow(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel", true);

        // Populate some stats
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel/MemberCount", "12 Members");
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel/TerritoryCount", "3 Territories");
    }

    public void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel", false);
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        // No screen-specific buttons in this example
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        // No text inputs in this screen
    }
}
```

---

### FactionMembersScreen.cs -- Pagination Using State

```csharp
using System;
using System.Collections.Generic;
using BlueBeard.UI;
using SDG.Unturned;

public class FactionMembersScreen : IUIScreen
{
    public string Id => "members";
    public IUIDialog[] Dialogs => new IUIDialog[] { ConfirmKickDialog };

    public ConfirmKickDialog ConfirmKickDialog { get; } = new();

    private const int PageSize = 5;
    private const int MaxRows = 5;

    private readonly UIManager _uiManager;

    public FactionMembersScreen(UIManager uiManager)
    {
        _uiManager = uiManager;
    }

    public void OnShow(UIContext ctx)
    {
        // Initialize pagination state
        ctx.Component.State["members.page"] = 0;
        ctx.Component.State["members.search"] = "";

        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel", true);

        RefreshList(ctx);
    }

    public void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel", false);
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        switch (buttonName)
        {
            case "Members_PrevPage":
            {
                int page = (int)ctx.Component.State["members.page"];
                if (page > 0)
                {
                    ctx.Component.State["members.page"] = page - 1;
                    RefreshList(ctx);
                }
                break;
            }

            case "Members_NextPage":
            {
                int page = (int)ctx.Component.State["members.page"];
                ctx.Component.State["members.page"] = page + 1;
                RefreshList(ctx);
                break;
            }

            // Each row has a kick button named Members_Kick_0 through Members_Kick_4
            case string s when s.StartsWith("Members_Kick_"):
            {
                int rowIndex = int.Parse(s.Replace("Members_Kick_", ""));
                int page = (int)ctx.Component.State["members.page"];
                int memberIndex = (page * PageSize) + rowIndex;

                // Store the target in State so the dialog can read it
                ctx.Component.State["confirm_kick.targetIndex"] = memberIndex;

                _uiManager.OpenDialog(ctx.Player, ConfirmKickDialog);
                break;
            }
        }
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        if (inputName == "Members_SearchInput")
        {
            ctx.Component.State["members.search"] = text;
            ctx.Component.State["members.page"] = 0;
            RefreshList(ctx);
        }
    }

    private void RefreshList(UIContext ctx)
    {
        int page = (int)ctx.Component.State["members.page"];
        string search = (string)ctx.Component.State["members.search"];

        // Fetch and filter members (replace with your data access)
        List<FactionMember> allMembers = GetMembers(ctx.Player.CSteamID.m_SteamID);

        if (!string.IsNullOrEmpty(search))
            allMembers = allMembers.FindAll(m =>
                m.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

        int totalPages = Math.Max(1, (int)Math.Ceiling(allMembers.Count / (double)PageSize));

        // Clamp page
        if (page >= totalPages)
        {
            page = totalPages - 1;
            ctx.Component.State["members.page"] = page;
        }

        // Update page indicator
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel/PageLabel", $"Page {page + 1} / {totalPages}");

        // Populate rows
        int start = page * PageSize;
        for (int i = 0; i < MaxRows; i++)
        {
            int idx = start + i;
            bool hasData = idx < allMembers.Count;
            string rowPath = $"Canvas/MembersPanel/Row_{i}";

            EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                rowPath, hasData);

            if (hasData)
            {
                var member = allMembers[idx];
                EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
                    $"{rowPath}/Name", member.Name);
                EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
                    $"{rowPath}/Rank", member.Rank);
                EffectManager.sendUIEffectImageURL(ctx.EffectKey, ctx.Connection, true,
                    $"{rowPath}/Avatar", member.AvatarUrl);
            }
        }

        // Show/hide prev/next buttons
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel/PrevButton", page > 0);
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel/NextButton", page < totalPages - 1);
    }

    private List<FactionMember> GetMembers(ulong steamId)
    {
        // Replace with actual data access
        return new List<FactionMember>();
    }
}
```

---

### ConfirmKickDialog.cs -- A Modal Popup

```csharp
using BlueBeard.UI;
using SDG.Unturned;

public class ConfirmKickDialog : IUIDialog
{
    public string Id => "confirm_kick";

    private UIManager _uiManager;

    public ConfirmKickDialog(UIManager uiManager)
    {
        _uiManager = uiManager;
    }

    public void OnShow(UIContext ctx)
    {
        // Show the overlay
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel", true);

        // Display the target member's name
        int targetIndex = (int)ctx.Component.State["confirm_kick.targetIndex"];
        string targetName = GetMemberNameByIndex(targetIndex);
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel/Message",
            $"Are you sure you want to kick {targetName}?");
    }

    public void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel", false);
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        switch (buttonName)
        {
            case "ConfirmKick_Yes":
                int targetIndex = (int)ctx.Component.State["confirm_kick.targetIndex"];
                PerformKick(ctx.Player.CSteamID.m_SteamID, targetIndex);
                _uiManager.CloseDialog(ctx.Player);
                // Optionally refresh the member list on the screen behind the dialog
                break;

            case "ConfirmKick_No":
                _uiManager.CloseDialog(ctx.Player);
                break;
        }
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        // No text inputs in this dialog
    }

    private string GetMemberNameByIndex(int index) => "SomeMember";
    private void PerformKick(ulong kickerSteamId, int targetIndex) { /* ... */ }
}
```

---

### Registration and Opening from a Command

```csharp
using BlueBeard.UI;
using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

public class FactionCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "faction";
    public string Help => "Opens the faction management panel.";
    public string Syntax => "/faction";
    public List<string> Aliases => new();
    public List<string> Permissions => new() { "faction.open" };

    private readonly UIManager _uiManager;
    private readonly FactionUI _factionUI;

    public FactionCommand(UIManager uiManager, FactionUI factionUI)
    {
        _uiManager = uiManager;
        _factionUI = factionUI;
    }

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = (UnturnedPlayer)caller;
        _uiManager.OpenUI(player, _factionUI);
    }
}
```

**Plugin initialization:**

```csharp
using BlueBeard.UI;
using Rocket.Core.Plugins;

public class FactionPlugin : RocketPlugin
{
    private UIManager _uiManager;
    private FactionUI _factionUI;

    protected override void Load()
    {
        _uiManager = new UIManager();
        _uiManager.Load();

        _factionUI = new FactionUI(_uiManager);
        _uiManager.RegisterUI(_factionUI);

        // Register command (varies by framework)
    }

    protected override void Unload()
    {
        _uiManager.Unload();
    }
}
```

---

## Common EffectManager Calls

These are the Unturned API calls you will use most often inside `OnShow`, `OnHide`, and button handlers. All of them require `ctx.EffectKey` and `ctx.Connection` from the `UIContext`.

### Show or Hide a GameObject

```csharp
EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
    "Canvas/Panel/ElementName", true);   // show

EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
    "Canvas/Panel/ElementName", false);  // hide
```

The string path is the hierarchy path of the Unity `GameObject` within the effect prefab. The `true` after `ctx.Connection` is the `reliable` parameter.

### Set Text

```csharp
EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
    "Canvas/Panel/Label", "Hello World");
```

Works with Unity `Text` and `TextMeshProUGUI` components.

### Set an Image URL

```csharp
EffectManager.sendUIEffectImageURL(ctx.EffectKey, ctx.Connection, true,
    "Canvas/Panel/Avatar", "https://example.com/avatar.png");
```

The URL must be publicly accessible. Commonly used with Steam avatar URLs.

---

## Async Data Loading Pattern

Use `ThreadHelper` from BlueBeard.Core to load data off the main thread and update the UI back on the main thread. This prevents server lag when fetching from a database or HTTP API.

```csharp
using BlueBeard.Core.Helpers;
using BlueBeard.UI;
using SDG.Unturned;

public class FactionMembersScreen : IUIScreen
{
    // ... properties omitted for brevity

    public void OnShow(UIContext ctx)
    {
        ctx.Component.State["members.page"] = 0;

        // Show the screen and a loading spinner
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel", true);
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel/Loading", true);
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel/Content", false);

        ulong steamId = ctx.Player.CSteamID.m_SteamID;

        ThreadHelper.RunAsynchronously(async () =>
        {
            // This runs on a background thread -- safe for DB/HTTP calls
            var members = await database.Table<Member>()
                .Where(m => m.FactionId == GetFactionId(steamId));

            // Switch back to the main thread to update the UI
            ThreadHelper.RunSynchronously(() =>
            {
                // Guard: player may have closed the UI while we were loading
                if (!ctx.Component.IsOpen) return;

                // Cache the data in State
                ctx.Component.State["members.data"] = members;

                // Hide loading, show content
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/MembersPanel/Loading", false);
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/MembersPanel/Content", true);

                // Populate the list
                RefreshList(ctx);
            });
        });
    }

    // ... rest of screen
}
```

### Key Points for Async Loading

1. **Show a loading indicator** before starting the async operation so the player knows something is happening.
2. **Use `ThreadHelper.RunAsynchronously`** with an `async` lambda for database or HTTP calls.
3. **Use `ThreadHelper.RunSynchronously`** to switch back to the main thread before calling any `EffectManager` methods (they are not thread-safe).
4. **Guard against stale state.** The player may close the UI or disconnect while the async operation is in progress. Always check `ctx.Component.IsOpen` before updating the UI.
5. **Cache fetched data in `State`** if you need it later (e.g., for pagination or row selection) to avoid redundant queries.
