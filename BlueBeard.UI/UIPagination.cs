using System;
using System.Collections.Generic;

namespace BlueBeard.UI;

/// <summary>
/// Reusable paged-list helper for effect UIs with a fixed number of row slots. Owns the
/// page number in <see cref="UIPlayerComponent.State"/> (so it survives screen redraws
/// and resets on close) and renders one page into your slots:
///
/// <code>
/// private static readonly UIPagination&lt;MemberInfo&gt; Pager = new("members", pageSize: 8);
///
/// private void Render(UIContext ctx, List&lt;MemberInfo&gt; members)
/// {
///     Pager.Render(ctx, members, (slot, member) =>
///     {
///         UIElements.SetVisible(ctx, $"Row_{slot}", member != null);
///         if (member != null) UIElements.SetText(ctx, $"Row_{slot}_Name", member.Name);
///     });
/// }
///
/// public override void OnButtonPressed(UIContext ctx, string button)
/// {
///     if (button == "Members_Next" &amp;&amp; Pager.NextPage(ctx, _members.Count)) Render(ctx, _members);
///     if (button == "Members_Prev" &amp;&amp; Pager.PreviousPage(ctx)) Render(ctx, _members);
/// }
/// </code>
/// </summary>
public sealed class UIPagination<T>(string stateKey, int pageSize)
{
    private readonly string _pageKey = stateKey + ".page";

    public int PageSize { get; } = pageSize > 0
        ? pageSize
        : throw new ArgumentOutOfRangeException(nameof(pageSize));

    /// <summary>Current zero-based page for this player (0 when never set).</summary>
    public int GetPage(UIContext context) =>
        context.Component.State.TryGetValue(_pageKey, out var value) && value is int page ? page : 0;

    public void SetPage(UIContext context, int page) =>
        context.Component.State[_pageKey] = Math.Max(0, page);

    public int GetPageCount(int itemCount) =>
        itemCount <= 0 ? 1 : (itemCount + PageSize - 1) / PageSize;

    /// <summary>Advance one page if one exists. Returns whether the page changed.</summary>
    public bool NextPage(UIContext context, int itemCount)
    {
        var page = GetPage(context);
        if (page + 1 >= GetPageCount(itemCount)) return false;
        SetPage(context, page + 1);
        return true;
    }

    /// <summary>Go back one page if possible. Returns whether the page changed.</summary>
    public bool PreviousPage(UIContext context)
    {
        var page = GetPage(context);
        if (page <= 0) return false;
        SetPage(context, page - 1);
        return true;
    }

    /// <summary>
    /// Invoke <paramref name="renderSlot"/> for every slot on the current page:
    /// (slotIndex 0..PageSize-1, item or default when the slot is past the end).
    /// Clamps the stored page if the item list shrank. Returns the rendered page index.
    /// </summary>
    public int Render(UIContext context, IReadOnlyList<T> items, Action<int, T> renderSlot)
    {
        if (renderSlot == null) throw new ArgumentNullException(nameof(renderSlot));

        var page = Math.Min(GetPage(context), GetPageCount(items?.Count ?? 0) - 1);
        SetPage(context, page);

        var start = page * PageSize;
        for (var slot = 0; slot < PageSize; slot++)
        {
            var index = start + slot;
            renderSlot(slot, items != null && index < items.Count ? items[index] : default);
        }
        return page;
    }
}
