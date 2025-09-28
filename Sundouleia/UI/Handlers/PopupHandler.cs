using CkCommons.Gui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Sundouleia.Services.Mediator;
using Dalamud.Bindings.ImGui;

namespace Sundouleia.Gui.Components;

/// <summary> The handler class for the popups in the UI. </summary>
public class PopupHandler : WindowMediatorSubscriberBase
{
    protected bool _openPopup = false;
    private readonly HashSet<IPopupHandler> _handlers;
    private IPopupHandler? _currentHandler = null;
    private bool ThemePushed = false;

    public PopupHandler(ILogger<PopupHandler> logger, SundouleiaMediator mediator, IEnumerable<IPopupHandler> popups)
        : base(logger, mediator, "SundouleiaPopupHandler")
    {
        // Adjust to restore some functionality?
        Flags = WFlags.NoBringToFrontOnFocus
          | WFlags.NoDecoration
          | WFlags.NoInputs
          | WFlags.NoSavedSettings
          | WFlags.NoBackground
          | WFlags.NoMove
          | WFlags.NoTitleBar;
        IsOpen = true;

        _handlers = popups.ToHashSet();

        Mediator.Subscribe<VerificationPopupMessage>(this, (msg) =>
        {
            // open the verification popup, and label the handler that one is open.
            _openPopup = true;
            // set the current popup handler to the verification popup handler
            _currentHandler = _handlers.OfType<VerificationPopupHandler>().Single();
            ((VerificationPopupHandler)_currentHandler).Open(msg);
            // set is open to true after processing the open function.
            IsOpen = true;
        });

        Mediator.Subscribe<PatternSavePromptMessage>(this, (msg) =>
        {
            // open the save pattern popup, and label the handler that one is open.
            _openPopup = true;
            // set the current popup handler to the save pattern popup handler
            _currentHandler = _handlers.OfType<SavePatternPopupHandler>().Single();
            ((SavePatternPopupHandler)_currentHandler).Open(msg);
            // set is open to true after processing the open function.
            IsOpen = true;
        });

        Mediator.Subscribe<ClosePatternSavePromptMessage>(this, (msg) =>
        {
            // call the close method in the currently open popup handler.
            _currentHandler = _handlers.OfType<SavePatternPopupHandler>().Single();
            ((SavePatternPopupHandler)_currentHandler).Close();
            // then close the window and popup.
            IsOpen = false;
            _openPopup = false;
        });

        Mediator.Subscribe<OpenReportUIMessage>(this, (msg) =>
        {
            // open the save pattern popup, and label the handler that one is open.
            _openPopup = true;
            // set the current popup handler to the save pattern popup handler
            _currentHandler = _handlers.OfType<ReportPopupHandler>().Single();
            ((ReportPopupHandler)_currentHandler).Open(msg);
            // set is open to true after processing the open function.
            IsOpen = true;
        });
    }

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            if (_currentHandler != null)
            {
                if (_currentHandler.WindowPadding.HasValue)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, _currentHandler.WindowPadding.Value);
                }
                if (_currentHandler.WindowRounding.HasValue)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, _currentHandler.WindowRounding.Value);
                }
            }
            ThemePushed = true;
        }
    }
    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            if (_currentHandler != null)
            {
                if (_currentHandler.WindowPadding.HasValue)
                {
                    ImGui.PopStyleVar();
                }
                if (_currentHandler.WindowRounding.HasValue)
                {
                    ImGui.PopStyleVar();
                }
            }
            ThemePushed = false;
        }
    }
    protected override void DrawInternal()
    {
        // If there is no handler, do nothing
        if (_currentHandler == null) return;

        // if we need to open a popup, do so
        if (_openPopup)
        {
            // open the popup and set open popup to false.
            ImGui.OpenPopup(WindowName);
            _openPopup = false;
        }

        // Get the size of the viewport
        var viewportSize = ImGui.GetWindowViewport().Size;
        // Set the window size and position
        ImGui.SetNextWindowSize(_currentHandler!.PopupSize * ImGuiHelpers.GlobalScale);
        ImGui.SetNextWindowPos(viewportSize / 2, ImGuiCond.Always, new Vector2(0.5f));
        
        // Open the popup
        using var popup = ImRaii.Popup(WindowName, WFlags.Modal);
        if (!popup) return;
        // draw the popups content
        _currentHandler.DrawContent();
        // if the handler of this content should show a close button (not sure what this is for yet)
        if (_currentHandler.ShowClosed)
        {
            ImGui.Separator();
            if (CkGui.IconTextButton(FAI.Times, "Close", ImGui.GetContentRegionAvail().X))
            {
                ImGui.CloseCurrentPopup();
            }
        }
    }
}
