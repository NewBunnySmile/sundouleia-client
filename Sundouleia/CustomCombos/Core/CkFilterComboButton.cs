using CkCommons.Gui;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;

namespace Sundouleia.CustomCombos;

public abstract class CkFilterComboButton<T> : CkFilterComboCache<T>
{
    protected CkFilterComboButton(IEnumerable<T> items, ILogger log)
        : base(items, log)
    { }

    protected CkFilterComboButton(Func<IReadOnlyList<T>> generator, ILogger log)
        : base(generator, log)
    { }

    /// <summary> The condition that when met, prevents the combo from being interacted. </summary>
    protected abstract bool DisableCondition();

    /// <summary> What will occur when the button is pressed. </summary>
    protected virtual void OnButtonPress(int layerIdx) 
    { }

    /// <summary> The virtual function for all filter combo buttons. </summary>
    /// <returns> True if anything was selected, false otherwise. </returns>
    /// <remarks> The action passed in will be invoked if the button interaction was successful. </remarks>
    public virtual bool DrawComboButton(string label, float width, int layerIdx, string bText, string tt, Action? onButtonSuccess = null)
    {
        // we need to first extract the width of the button.
        var comboWidth = width - ImGuiHelpers.GetButtonSize(bText).X - ImGui.GetStyle().ItemInnerSpacing.X;
        InnerWidth = width;

        // if we have a new item selected we need to update some conditionals.

        var previewLabel = Current is not null ? ToString(Current) : "Select an Item...";
        var ret = Draw(label, previewLabel, string.Empty, comboWidth, ImGui.GetTextLineHeightWithSpacing(), CFlags.None);
        // move just beside it to draw the button.
        ImUtf8.SameLineInner();

        // disable the button if we should.
        if (ImGuiUtil.DrawDisabledButton(bText, new Vector2(), string.Empty, DisableCondition()))
            OnButtonPress(layerIdx);
        CkGui.AttachToolTip(tt);

        return ret;
    }
}
