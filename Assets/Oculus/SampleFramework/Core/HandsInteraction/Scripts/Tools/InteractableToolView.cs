// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

namespace OculusSampleFramework
{
    /// <summary>
    ///     The visual abstraction of an interactable tool.
    /// </summary>
    public interface InteractableToolView
    {
        InteractableTool InteractableTool { get; }

        bool EnableState { get; set; }

        // Useful if you want to tool to glow in case it interacts with an object.
        bool ToolActivateState { get; set; }
        void SetFocusedInteractable(Interactable interactable);
    }
}
