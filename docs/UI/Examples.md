# Examples

This page shows a complete multi-screen UI implementation with pagination, a modal dialog, async data loading, push updates, and common `EffectManager` patterns.

---

## FactionUI -- A Complete Multi-Screen Example

This example builds a faction management panel with two screens (Overview and Members), a confirmation dialog for kicking members, pagination, search, and async data loading. Every screen and dialog is registered by type via `UIBuilder`, and an external `RentManager` pushes updates into the UI.

### File Structure

```
FactionPlugin/
  FactionPlugin.cs             -- plugin entry point with static UI singleton
  UI/
    FactionUI.cs                -- IUI<FactionUI> implementation
    FactionOverviewScreen.cs    -- IUIScreen: faction stats
    FactionMembersScreen.cs     -- IUIScreen: member list with pagination
    ConfirmKickDialog.cs        -- IUIDialog: confirm before kicking
  Commands/
    FactionCommand.cs           -- opens the UI
```

---

### FactionPlugin.cs -- Static singleton

```csharp
using BlueBeard.UI;
using Rocket.Core.Plugins;

public class FactionPlugin : RocketPlugin
{
    public static FactionPlugin Instance { get; private set; }
    public UIManager UI { get; private set; }

    protected override void Load()
    {
        Instance = this;
        UI = new UIManager();
        UI.Load();

        UI.RegisterUI<FactionUI>();   // Configure runs here, screens/dialogs are instantiated
    }

    protected override void Unload()
    {
        UI.Unload();
        Instance = null;
    }
}
```

Screens and dialogs access the manager through `FactionPlugin.Instance.UI` because the `new()` constraint forbids constructor injection.

---

### FactionUI.cs -- The Top-Level Container

```csharp
using System.Collections.Generic;
using BlueBeard.UI;
using SDG.Unturned;

public class FactionUI : UIBase, IUI<FactionUI>
{
    public override string Id => "faction";
    public override ushort EffectId => 50600;
    public override short EffectKey => (short)EffectId;

    public void Configure(UIBuilder builder)
    {
        builder
            .AddScreen<FactionOverviewScreen>(isDefault: true)
            .AddScreen<FactionMembersScreen>()
            .AddDialog<ConfirmKickDialog>();
    }

    public override void OnOpened(UIContext ctx)
    {
        var factionName = GetFactionName(ctx.Player.CSteamID.m_SteamID);
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/FactionName", factionName);

        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/Tab_Overview_Active", true);
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/Tab_Members_Active", false);
    }

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        // 1. Global buttons
        switch (buttonName)
        {
            case "Faction_Close":
                FactionPlugin.Instance.UI.CloseUI(ctx.Player);
                return;

            case "Faction_Tab_Overview":
                FactionPlugin.Instance.UI.SetScreen<FactionOverviewScreen>(ctx.Player);
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/Header/Tab_Overview_Active", true);
                EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                    "Canvas/Header/Tab_Members_Active", false);
                return;

            case "Faction_Tab_Members":
                FactionPlugin.Instance.UI.SetScreen<FactionMembersScreen>(ctx.Player);
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

    public override void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        if (ctx.Component.CurrentDialog != null)
        {
            ctx.Component.CurrentDialog.OnTextSubmitted(ctx, inputName, text);
            return;
        }

        ctx.Component.CurrentScreen?.OnTextSubmitted(ctx, inputName, text);
    }

    // Last-resort catch for updates no dialog/screen consumed.
    public override bool OnUpdate(UIContext ctx, string key, object value)
    {
        if (key == "faction.disbanded")
        {
            FactionPlugin.Instance.UI.CloseUI(ctx.Player);
            return true;
        }
        return false;
    }

    private string GetFactionName(ulong steamId) => "The Iron Guard";
}
```

---

### FactionOverviewScreen.cs -- A Simple Screen

```csharp
using BlueBeard.UI;
using SDG.Unturned;

public class FactionOverviewScreen : UIScreenBase
{
    public override string Id => "overview";

    public override void OnShow(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel", true);
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel/MemberCount", "12 Members");
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel/TerritoryCount", "3 Territories");
    }

    public override void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/OverviewPanel", false);
    }

    public override bool OnUpdate(UIContext ctx, string key, object value)
    {
        if (key == "faction.stats_changed")
        {
            // Re-render the stats panel without having to be told exactly what changed.
            OnShow(ctx);
            return true;
        }
        return false;
    }
}
```

---

### FactionMembersScreen.cs -- Pagination + Push Updates

```csharp
using System;
using System.Collections.Generic;
using BlueBeard.UI;
using SDG.Unturned;

public class FactionMembersScreen : UIScreenBase
{
    public override string Id => "members";

    private const int PageSize = 5;
    private const int MaxRows = 5;

    public override void OnShow(UIContext ctx)
    {
        ctx.Component.State["members.page"] = 0;
        ctx.Component.State["members.search"] = "";

        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel", true);

        RefreshList(ctx);
    }

    public override void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel", false);
    }

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        switch (buttonName)
        {
            case "Members_PrevPage":
            {
                var page = (int)ctx.Component.State["members.page"];
                if (page > 0)
                {
                    ctx.Component.State["members.page"] = page - 1;
                    RefreshList(ctx);
                }
                break;
            }

            case "Members_NextPage":
            {
                var page = (int)ctx.Component.State["members.page"];
                ctx.Component.State["members.page"] = page + 1;
                RefreshList(ctx);
                break;
            }

            case string s when s.StartsWith("Members_Kick_"):
            {
                var rowIndex = int.Parse(s.Replace("Members_Kick_", ""));
                var page = (int)ctx.Component.State["members.page"];
                var memberIndex = (page * PageSize) + rowIndex;

                ctx.Component.State["confirm_kick.targetIndex"] = memberIndex;
                FactionPlugin.Instance.UI.OpenDialog<ConfirmKickDialog>(ctx.Player);
                break;
            }
        }
    }

    public override void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        if (inputName == "Members_SearchInput")
        {
            ctx.Component.State["members.search"] = text;
            ctx.Component.State["members.page"] = 0;
            RefreshList(ctx);
        }
    }

    // A push update saying someone joined/left -- refresh the current page.
    public override bool OnUpdate(UIContext ctx, string key, object value)
    {
        if (key == "members.changed")
        {
            RefreshList(ctx);
            return true;
        }
        return false;
    }

    private void RefreshList(UIContext ctx)
    {
        // ... populate rows as before (see previous docs revision for detail)
    }
}
```

---

### ConfirmKickDialog.cs -- A Modal Popup

```csharp
using BlueBeard.UI;
using SDG.Unturned;

public class ConfirmKickDialog : UIDialogBase
{
    public override string Id => "confirm_kick";

    public override void OnShow(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel", true);

        var targetIndex = (int)ctx.Component.State["confirm_kick.targetIndex"];
        var targetName = GetMemberNameByIndex(targetIndex);
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel/Message",
            $"Are you sure you want to kick {targetName}?");
    }

    public override void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel", false);
    }

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        switch (buttonName)
        {
            case "ConfirmKick_Yes":
                var targetIndex = (int)ctx.Component.State["confirm_kick.targetIndex"];
                PerformKick(ctx.Player.CSteamID.m_SteamID, targetIndex);
                FactionPlugin.Instance.UI.CloseDialog(ctx.Player);
                break;

            case "ConfirmKick_No":
                FactionPlugin.Instance.UI.CloseDialog(ctx.Player);
                break;
        }
    }

    private string GetMemberNameByIndex(int index) => "SomeMember";
    private void PerformKick(ulong kickerSteamId, int targetIndex) { /* ... */ }
}
```

---

### Opening from a Command

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

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = (UnturnedPlayer)caller;
        FactionPlugin.Instance.UI.OpenUI<FactionUI>(player);
    }
}
```

---

## Push Update Example: RentManager

An external system that knows nothing about the UI's internal structure can still notify it of state changes via `PushUpdate`:

```csharp
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using Rocket.Unturned.Player;
using Steamworks;

public class RentManager
{
    public void CollectRent(Property property, string renterName)
    {
        // ... perform the transaction ...

        // Notify the owner's UI if they're viewing the housing panel.
        var ownerPlayer = UnturnedPlayer.FromCSteamID(new CSteamID(property.OwnerSteamId));
        if (ownerPlayer != null)
        {
            ThreadHelper.RunSynchronously(() =>
            {
                FactionPlugin.Instance.UI.PushUpdate(ownerPlayer, "rent.collected",
                    new Dictionary<string, object>
                    {
                        ["renter"]   = renterName,
                        ["amount"]   = property.RentPrice,
                        ["next_due"] = property.RentDueAt,
                    });
            });
        }
    }

    // Tell every online player who has the ShopUI open that stock changed.
    public void OnShopPurchase(int itemId, int remaining)
    {
        FactionPlugin.Instance.UI.PushUpdateAll<ShopUI>("stock.changed",
            new Dictionary<string, object>
            {
                ["itemId"]    = itemId,
                ["remaining"] = remaining,
            });
    }
}
```

The update travels through the dialog → screen → IUI chain on the owner's active UI. Any layer can consume it by returning `true` from `OnUpdate`. If no layer handles it, the update is silently dropped (no exception, no warning).

See [Event Routing](Event-Routing.md) for the full push-update dispatch documentation.

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

---

## Async Data Loading Pattern

Use `ThreadHelper` from `BlueBeard.Core` to load data off the main thread and update the UI back on the main thread:

```csharp
using BlueBeard.Core.Helpers;
using BlueBeard.UI;
using SDG.Unturned;

public override void OnShow(UIContext ctx)
{
    ctx.Component.State["members.page"] = 0;

    EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
        "Canvas/MembersPanel", true);
    EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
        "Canvas/MembersPanel/Loading", true);

    var steamId = ctx.Player.CSteamID.m_SteamID;
    ThreadHelper.RunAsynchronously(async () =>
    {
        // Background thread -- safe for DB/HTTP calls
        var members = await database.Table<Member>()
            .Where(m => m.FactionId == GetFactionId(steamId));

        ThreadHelper.RunSynchronously(() =>
        {
            // Guard: the player may have closed the UI while we were loading
            if (!ctx.Component.IsOpen) return;

            ctx.Component.State["members.data"] = members;

            EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                "Canvas/MembersPanel/Loading", false);
            RefreshList(ctx);
        });
    });
}
```

### Key Points

1. Show a loading indicator before starting async work.
2. Use `ThreadHelper.RunAsynchronously` for the DB/HTTP call.
3. Use `ThreadHelper.RunSynchronously` to switch back to the main thread before touching `EffectManager`.
4. Guard against stale state (`ctx.Component.IsOpen`) in case the player closed the UI.
5. Cache fetched data in `State` if you need it later for pagination or selection.
