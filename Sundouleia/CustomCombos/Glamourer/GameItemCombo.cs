using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Extensions;
using OtterGui.Raii;

namespace Sundouleia.CustomCombos.Glamourer;

public sealed class GameItemCombo : CkFilterComboCache<EquipItem>
{
    public readonly string Label;
    public ItemId _currentItem;
    public PrimaryId CustomSetId { get; private set; }
    public Variant CustomVariant { get; private set; }
    public GameItemCombo(EquipSlot slot, ILogger log)
        : base(() => GetItems(slot), log)
    {
        Label = GetLabel(slot);
        _currentItem = ItemSvc.NothingId(slot);
        SearchByParts = true;
    }

    protected override void DrawList(float width, float itemHeight, float filterHeight)
    {
        base.DrawList(width, itemHeight, filterHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            Current = Items[NewSelection.Value];
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current.ItemId == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.ItemId == _currentItem);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    public bool Draw(string previewName, ItemId previewIdx, float width, float innerWidth, string labelDisp = "")
    {
        InnerWidth = innerWidth;
        _currentItem = previewIdx;
        CustomVariant = 0;
        return Draw($"{labelDisp}##Test{Label}", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj = Items[globalIdx];
        var name = ToString(obj);
        var ret = ImGui.Selectable(name, selected);
        ImGui.SameLine();
        using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF808080);
        ImGuiUtil.RightAlign($"({obj.ModelString})");
        return ret;
    }

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => base.IsVisible(globalIndex, filter) || filter.IsContained(Items[globalIndex].PrimaryId.Id.ToString());

    protected override string ToString(EquipItem obj)
        => obj.Name;

    private static string GetLabel(EquipSlot slot)
    {
        var sheet = Svc.Data.GetExcelSheet<Addon>()!;

        return slot switch
        {
            EquipSlot.Head => sheet.GetRow(740).Text.ToString() ?? "Head",
            EquipSlot.Body => sheet.GetRow(741).Text.ToString() ?? "Body",
            EquipSlot.Hands => sheet.GetRow(742).Text.ToString() ?? "Hands",
            EquipSlot.Legs => sheet.GetRow(744).Text.ToString() ?? "Legs",
            EquipSlot.Feet => sheet.GetRow(745).Text.ToString() ?? "Feet",
            EquipSlot.Ears => sheet.GetRow(746).Text.ToString() ?? "Ears",
            EquipSlot.Neck => sheet.GetRow(747).Text.ToString() ?? "Neck",
            EquipSlot.Wrists => sheet.GetRow(748).Text.ToString() ?? "Wrists",
            EquipSlot.RFinger => sheet.GetRow(749).Text.ToString() ?? "Right Ring",
            EquipSlot.LFinger => sheet.GetRow(750).Text.ToString() ?? "Left Ring",
            _ => string.Empty,
        };
    }

    private static IReadOnlyList<EquipItem> GetItems(EquipSlot slot)
    {
        var nothing = ItemSvc.NothingItem(slot);
        if (!ItemSvc.ItemData.ByType.TryGetValue(slot.ToEquipType(), out var list))
            return new[]
            {
                nothing,
            };

        var enumerable = list.AsEnumerable();
        if (slot.IsEquipment())
            enumerable = enumerable.Append(ItemSvc.SmallClothesItem(slot));

        var itemList = enumerable.OrderBy(i => i.Name).Prepend(nothing).ToList();

        return itemList;
    }

    protected override void OnClosePopup()
    {
        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;

        CustomSetId = setId;
        CustomVariant = variant;
    }
}

