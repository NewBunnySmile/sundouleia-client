using OtterGui.Classes;

namespace Sundouleia.CustomCombos;

/// <summary> FilterComboCache, Behaves like OtterGui's but with ILogger and no mouse utility. </summary>
/// <remarks> Intended for uses not requiring callback verification. (FilterComboButtons Excepted) </remarks>
public abstract class CkFilterComboCache<T> : CkFilterComboBase<T>
{
    /// <summary> The selected item in non-index format. </summary>
    /// <remarks> This is for the OPENED Combo. This means if a combo has multiple draws, only the focused list reflects this. <remarks>
    public T? Current { get; protected set; }

    /// <summary> A Cached List of the generated items. </summary>
    /// <remarks> Items are regenerated every time a cleanup is called. </remarks>
    private readonly ICachingList<T> _items;

    /// <summary> The current selection index in the filter cache. </summary>
    /// <remarks> This is for the OPENED Combo. This means if a combo has multiple draws, only the focused list reflects this. <remarks>
    protected int CurrentSelectionIdx = -1;

    /// <summary> The condition that is met whenever the CachingList <typeparamref name="T"/> has finished caching the generated item function. </summary>
    protected bool IsInitialized => _items.IsInitialized;

    protected CkFilterComboCache(IEnumerable<T> items, ILogger log)
        : base(new TemporaryList<T>(items), log)
    {
        Current = default(T);
        _items = (ICachingList<T>)Items;
    }

    protected CkFilterComboCache(Func<IReadOnlyList<T>> generator, ILogger log)
        : base(new LazyList<T>(generator), log)
    {
        Current = default(T);
        _items = (ICachingList<T>)Items;
    }

    /// <summary> Triggers our Caching list to regenerate its passed in item list. </summary>
    /// <remarks> Call this whenever the source of our list updates to keep it synced. </remarks>
    protected override void Cleanup()
        => _items.ClearList();

    /// <summary> Draws the list and updates the selection in the filter cache if needed. </summary>
    protected override void DrawList(float width, float itemHeight, float filterHeight)
    {
        base.DrawList(width, itemHeight, filterHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            UpdateSelection(Items[NewSelection.Value]);
    }

    /// <summary> Invokes SelectionChanged & updates Current. </summary>
    /// <remarks> Called if a change occurred in the DrawList override. </remarks>
    protected virtual void UpdateSelection(T? newSelection)
    {
        if (!ReferenceEquals(Current, newSelection))
            SelectionChanged?.Invoke(Current, newSelection);
        Current = newSelection;
    }

    /// <summary> The main Draw function that should be used for any parenting client side FilterCombo's of all types. </summary>
    /// <remarks> Any selection, or any change, will be stored into the CurrentSelectionIdx. </remarks>
    public bool Draw(string label, string preview, string tooltip, float previewWidth, float itemHeight,
        CFlags flags = CFlags.None, uint? customSearchBg = null)
    {
        return Draw(label, preview, tooltip, ref CurrentSelectionIdx, previewWidth, itemHeight, flags, customSearchBg);
    }

    public bool DrawPopup(string label, Vector2 openPos, float itemHeight, uint? customSearchBg = null)
        => DrawPopup(label, openPos, ref CurrentSelectionIdx, itemHeight, customSearchBg);

    /// <summary> Fires an event that gives us the previous and new selection. </summary>
    public event Action<T?, T?>? SelectionChanged;
}
