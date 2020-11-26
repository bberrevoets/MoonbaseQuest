// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;

namespace OculusSampleFramework
{
    /// <summary>
    ///     Routes all collisions from interactable tools to the interactables themselves.
    ///     We want to do this in a top-down fashion, because we might want to disable
    ///     far-field interactions if near-field interactions take precendence (for instance).
    /// </summary>
    public class InteractableToolsInputRouter : MonoBehaviour
    {
        private static InteractableToolsInputRouter _instance;
        private        HashSet<InteractableTool>    _leftHandFarTools = new HashSet<InteractableTool>();

        private HashSet<InteractableTool> _leftHandNearTools = new HashSet<InteractableTool>();
        private bool                      _leftPinch, _rightPinch;
        private HashSet<InteractableTool> _rightHandFarTools  = new HashSet<InteractableTool>();
        private HashSet<InteractableTool> _rightHandNearTools = new HashSet<InteractableTool>();

        public static InteractableToolsInputRouter Instance
        {
            get
            {
                if (_instance == null)
                {
                    var instances = FindObjectsOfType<InteractableToolsInputRouter>();
                    if (instances.Length > 0)
                    {
                        _instance = instances[0];
                        // remove extras, if any
                        for (var i = 1; i < instances.Length; i++)
                        {
                            Destroy(instances[i].gameObject);
                        }
                    }
                }

                return _instance;
            }
        }

        private void Update()
        {
            if (!HandsManager.Instance.IsInitialized())
            {
                return;
            }

            var leftHandIsReliable = HandsManager.Instance.LeftHand.IsTracked &&
                                     HandsManager.Instance.LeftHand.HandConfidence == OVRHand.TrackingConfidence.High;
            var rightHandIsReliable = HandsManager.Instance.RightHand.IsTracked &&
                                      HandsManager.Instance.RightHand.HandConfidence == OVRHand.TrackingConfidence.High;
            var leftHandProperlyTracked  = HandsManager.Instance.LeftHand.IsPointerPoseValid;
            var rightHandProperlyTracked = HandsManager.Instance.RightHand.IsPointerPoseValid;

            var encounteredNearObjectsLeftHand = UpdateToolsAndEnableState(_leftHandNearTools, leftHandIsReliable);
            // don't interact with far field if near field is touching something
            UpdateToolsAndEnableState(_leftHandFarTools, !encounteredNearObjectsLeftHand && leftHandIsReliable &&
                                                         leftHandProperlyTracked);

            var encounteredNearObjectsRightHand = UpdateToolsAndEnableState(_rightHandNearTools, rightHandIsReliable);
            // don't interact with far field if near field is touching something
            UpdateToolsAndEnableState(_rightHandFarTools, !encounteredNearObjectsRightHand && rightHandIsReliable &&
                                                          rightHandProperlyTracked);
        }

        public void RegisterInteractableTool(InteractableTool interactableTool)
        {
            if (interactableTool.IsRightHandedTool)
            {
                if (interactableTool.IsFarFieldTool)
                {
                    _rightHandFarTools.Add(interactableTool);
                }
                else
                {
                    _rightHandNearTools.Add(interactableTool);
                }
            }
            else
            {
                if (interactableTool.IsFarFieldTool)
                {
                    _leftHandFarTools.Add(interactableTool);
                }
                else
                {
                    _leftHandNearTools.Add(interactableTool);
                }
            }
        }

        public void UnregisterInteractableTool(InteractableTool interactableTool)
        {
            if (interactableTool.IsRightHandedTool)
            {
                if (interactableTool.IsFarFieldTool)
                {
                    _rightHandFarTools.Remove(interactableTool);
                }
                else
                {
                    _rightHandNearTools.Remove(interactableTool);
                }
            }
            else
            {
                if (interactableTool.IsFarFieldTool)
                {
                    _leftHandFarTools.Remove(interactableTool);
                }
                else
                {
                    _leftHandNearTools.Remove(interactableTool);
                }
            }
        }

        private bool UpdateToolsAndEnableState(HashSet<InteractableTool> tools, bool toolsAreEnabledThisFrame)
        {
            var encounteredObjects = UpdateTools(tools, !toolsAreEnabledThisFrame);
            ToggleToolsEnableState(tools, toolsAreEnabledThisFrame);
            return encounteredObjects;
        }

        /// <summary>
        ///     Update tools specified based on new collisions.
        /// </summary>
        /// <param name="tools">Tools to update.</param>
        /// <param name="resetCollisionData">
        ///     True if we want the tool to be disabled. This can happen
        ///     if near field tools take precedence over far-field tools, for instance.
        /// </param>
        /// <returns></returns>
        private bool UpdateTools(HashSet<InteractableTool> tools, bool resetCollisionData = false)
        {
            var toolsEncounteredObjects = false;

            foreach (var currentInteractableTool in tools)
            {
                var intersectingObjectsFound =
                        currentInteractableTool.GetNextIntersectingObjects();

                if (intersectingObjectsFound.Count > 0 && !resetCollisionData)
                {
                    if (!toolsEncounteredObjects)
                    {
                        toolsEncounteredObjects = intersectingObjectsFound.Count > 0;
                    }

                    // create map that indicates the furthest collider encountered per interactable element
                    currentInteractableTool.UpdateCurrentCollisionsBasedOnDepth();

                    if (currentInteractableTool.IsFarFieldTool)
                    {
                        var firstInteractable = currentInteractableTool.GetFirstCurrentCollisionInfo();
                        // if our tool is activated, make sure depth is set to "action"
                        if (currentInteractableTool.ToolInputState == ToolInputState.PrimaryInputUp)
                        {
                            firstInteractable.Value.InteractableCollider = firstInteractable.Key.ActionCollider;
                            firstInteractable.Value.CollisionDepth       = InteractableCollisionDepth.Action;
                        }
                        else
                        {
                            firstInteractable.Value.InteractableCollider = firstInteractable.Key.ContactCollider;
                            firstInteractable.Value.CollisionDepth       = InteractableCollisionDepth.Contact;
                        }

                        // far field tools only can focus elements -- pick first (for now)
                        currentInteractableTool.FocusOnInteractable(firstInteractable.Key,
                                firstInteractable.Value.InteractableCollider);
                    }
                }
                else
                {
                    currentInteractableTool.DeFocus();
                    currentInteractableTool.ClearAllCurrentCollisionInfos();
                }

                currentInteractableTool.UpdateLatestCollisionData();
            }

            return toolsEncounteredObjects;
        }

        private void ToggleToolsEnableState(HashSet<InteractableTool> tools, bool enableState)
        {
            foreach (var tool in tools)
            {
                if (tool.EnableState != enableState)
                {
                    tool.EnableState = enableState;
                }
            }
        }
    }
}
