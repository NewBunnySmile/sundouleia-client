using CkCommons.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Sundouleia.Pairs;
using Sundouleia.PlayerClient;
using Sundouleia.Services.Configs;
using Sundouleia.Services.Mediator;
using Dalamud.Bindings.ImGui;

namespace Sundouleia.Gui.Handlers;

public class IdDisplayHandler
{
    private readonly SundouleiaMediator _mediator;
    private readonly MainConfig _mainConfig;
    private readonly ServerConfigManager _serverConfig;
    private readonly Dictionary<string, bool> _showIdForEntry = new(StringComparer.Ordinal);
    private string _editComment = string.Empty;
    private string _editEntry = string.Empty;
    private string _lastMouseOverUid = string.Empty;
    private bool _popupShown = false;
    private DateTime? _popupTime;
    public IdDisplayHandler(SundouleiaMediator mediator, ServerConfigManager serverManager, MainConfig sundouleiaConfigService)
    {
        _mediator = mediator;
        _serverConfig = serverManager;
        _mainConfig = sundouleiaConfigService;
    }

    public bool DrawPairText(string id, Sundesmo pair, float textPosX, Func<float> editBoxWidth)
    {
        var returnVal = false;

        ImGui.SameLine(textPosX);
        (var textIsUid, var playerText) = GetPlayerText(pair);

        var textWidth = editBoxWidth.Invoke() - 20f;
        var hovered = false;

        if (!string.Equals(_editEntry, pair.UserData.UID, StringComparison.Ordinal))
        {
            ImGui.AlignTextToFramePadding();

            using (ImRaii.Group())
            {
                using (ImRaii.PushFont(UiBuilder.MonoFont, textIsUid)) ImGui.TextUnformatted(playerText);
                if (ImGui.IsItemHovered())
                {
                    hovered = true;

                    if (!string.Equals(_lastMouseOverUid, id))
                    {
                        _popupTime = DateTime.UtcNow.AddSeconds(_mainConfig.Current.ProfileDelay);
                    }

                    _lastMouseOverUid = id;

                    if (_popupTime < DateTime.UtcNow && !_popupShown && _mainConfig.Current.ShowProfiles)
                    {
                        _popupShown = true;
                        _mediator.Publish(new OpenProfilePopout(pair.UserData));
                    }
                }

                // Draw an invisible item that matches the width of the editable text box
                ImGui.SameLine();
                ImGui.InvisibleButton("hoverArea", new Vector2(textWidth - ImGui.CalcTextSize(playerText).X, ImGui.GetTextLineHeight()));
                if (ImGui.IsItemHovered()) hovered = true;


                if (hovered)
                {
                    ImGui.SetTooltip("Left click to switch between UID display and nick" + Environment.NewLine
                        + "Right click to change nick for " + pair.UserData.UID + Environment.NewLine
                        + "Middle Mouse Button to open their profile in a separate window");
                }
                else
                {
                    if (string.Equals(_lastMouseOverUid, id))
                    {
                        _mediator.Publish(new CloseProfilePopout());
                        _lastMouseOverUid = string.Empty;
                        _popupShown = false;
                    }
                }
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                var prevState = textIsUid;
                if (_showIdForEntry.ContainsKey(pair.UserData.UID))
                    prevState = _showIdForEntry[pair.UserData.UID];

                _showIdForEntry[pair.UserData.UID] = !prevState;
                returnVal = true;
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Middle))
                _mediator.Publish(new ProfileOpenMessage(pair.UserData));

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _serverConfig.SetNicknameForUid(_editEntry, _editComment);
                _editComment = pair.GetNickname() ?? string.Empty;
                _editEntry = pair.UserData.UID;
            }
        }
        else
        {
            ImGui.AlignTextToFramePadding();

            ImGui.SetNextItemWidth(editBoxWidth.Invoke());
            if (ImGui.InputTextWithHint("##"+pair.UserData.UID, "Nick/Nicknames", ref _editComment, 255, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _serverConfig.SetNicknameForUid(pair.UserData.UID, _editComment);
                _editEntry = string.Empty;
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _editEntry = string.Empty;
            }
            CkGui.AttachToolTip("Hit ENTER to save\nRight click to cancel");
        }
        return returnVal;
    }
    public (bool isUid, string text) GetPlayerText(Sundesmo pair)
    {
        var textIsUid = true;
        var showUidInsteadOfName = ShowUidInsteadOfName(pair);
        var playerText = _serverConfig.GetNicknameForUid(pair.UserData.UID);
        if (!showUidInsteadOfName && playerText != null)
        {
            if (string.IsNullOrEmpty(playerText))
            {
                playerText = pair.UserData.AliasOrUID;
            }
            else
            {
                textIsUid = false;
            }
        }
        else
        {
            playerText = pair.UserData.AliasOrUID;
        }

        if (pair.PlayerRendered && !showUidInsteadOfName)
        {
            playerText = pair.PlayerName;
            textIsUid = false;
            if (_mainConfig.Current.PreferNicknamesOverNames)
            {
                var note = pair.GetNickname();
                if (note != null)
                {
                    playerText = note;
                }
            }
        }

        return (textIsUid, playerText!);
    }

    internal void Clear()
    {
        _editEntry = string.Empty;
        _editComment = string.Empty;
    }

    private bool ShowUidInsteadOfName(Sundesmo pair)
    {
        _showIdForEntry.TryGetValue(pair.UserData.UID, out var showidInsteadOfName);

        return showidInsteadOfName;
    }
}
