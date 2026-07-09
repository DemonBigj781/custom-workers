using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CustomWorkers;

internal static class ControlHelper
{
    internal static bool EnsureHireWorkerControllerRows(HireWorkerScreen? screen)
    {
        ControllerScreenUIExtension? extension = screen?.m_ControllerScreenUIExtension;
        if (screen?.m_HireWorkerPanelUIList == null || extension?.m_ControllerBtnColumnList == null || extension.m_ControllerBtnColumnList.Count == 0)
        {
            return false;
        }

        int missingRowCount = ControllerRowRules.GetMissingRowCount(extension.m_ControllerBtnColumnList.Count, screen.m_HireWorkerPanelUIList.Count);
        if (missingRowCount == 0)
        {
            return false;
        }

        for (int rowOffset = 0; rowOffset < missingRowCount; rowOffset++)
        {
            int templateRowIndex = extension.m_ControllerBtnColumnList.Count - 1;
            int clonePanelIndex = extension.m_ControllerBtnColumnList.Count;
            if (templateRowIndex < 0 || clonePanelIndex >= screen.m_HireWorkerPanelUIList.Count)
            {
                break;
            }

            HireWorkerPanelUI templatePanel = screen.m_HireWorkerPanelUIList[templateRowIndex];
            HireWorkerPanelUI clonePanel = screen.m_HireWorkerPanelUIList[clonePanelIndex];
            extension.m_ControllerBtnColumnList.Add(CloneControllerRow(extension.m_ControllerBtnColumnList[templateRowIndex], templatePanel.transform, clonePanel.transform));

            if (extension.m_CtrlBtnXChangeMethodList != null && extension.m_CtrlBtnXChangeMethodList.Count > templateRowIndex)
            {
                extension.m_CtrlBtnXChangeMethodList.Add(extension.m_CtrlBtnXChangeMethodList[templateRowIndex]);
            }
        }

        return true;
    }

    internal static int RebindControllerButtons(HireWorkerScreen? screen, BepInEx.Logging.ManualLogSource? logger)
    {
        if (screen?.m_HireWorkerPanelUIList == null)
        {
            return 0;
        }

        int reboundCount = 0;
        for (int panelIndex = 0; panelIndex < screen.m_HireWorkerPanelUIList.Count; panelIndex++)
        {
            HireWorkerPanelUI? panel = screen.m_HireWorkerPanelUIList[panelIndex];
            if (panel == null)
            {
                continue;
            }

            ControllerButton[] buttons = panel.GetComponentsInChildren<ControllerButton>(true);
            for (int buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
            {
                ControllerButton controllerButton = buttons[buttonIndex];
                if (controllerButton == null)
                {
                    continue;
                }

                bool rebound = RebindControllerButton(panel, controllerButton, logger, panelIndex, buttonIndex);
                if (rebound)
                {
                    reboundCount++;
                }
            }
        }

        logger?.LogInfo($"Custom Workers rebound {reboundCount} controller button bindings for ARC Recruiter.");
        return reboundCount;
    }

    private static ControllerBtnList CloneControllerRow(ControllerBtnList templateRow, Transform templateRoot, Transform cloneRoot)
    {
        var buttons = new List<ControllerButton>();
        for (int index = 0; index < templateRow.rowList.Count; index++)
        {
            ControllerButton? clonedButton = FindClonedButton(templateRow.rowList[index], templateRoot, cloneRoot, index);
            if (clonedButton != null)
            {
                buttons.Add(clonedButton);
            }
        }

        return new ControllerBtnList
        {
            rowList = buttons
        };
    }

    private static ControllerButton? FindClonedButton(ControllerButton? templateButton, Transform templateRoot, Transform cloneRoot, int fallbackIndex)
    {
        if (templateButton == null)
        {
            return null;
        }

        int[]? siblingPath = BuildSiblingPath(templateButton.transform, templateRoot);
        if (siblingPath != null)
        {
            Transform? clonedTransform = WalkSiblingPath(cloneRoot, siblingPath);
            if (clonedTransform != null)
            {
                ControllerButton? directMatch = clonedTransform.GetComponent<ControllerButton>();
                if (directMatch != null)
                {
                    return directMatch;
                }

                ControllerButton? nestedMatch = clonedTransform.GetComponentInChildren<ControllerButton>(true);
                if (nestedMatch != null)
                {
                    return nestedMatch;
                }
            }
        }

        ControllerButton[] cloneButtons = cloneRoot.GetComponentsInChildren<ControllerButton>(true);
        return fallbackIndex < cloneButtons.Length ? cloneButtons[fallbackIndex] : null;
    }

    private static int[]? BuildSiblingPath(Transform child, Transform ancestor)
    {
        var indices = new List<int>();
        Transform current = child;
        while (current != null && current != ancestor)
        {
            indices.Add(current.GetSiblingIndex());
            current = current.parent;
        }

        if (current != ancestor)
        {
            return null;
        }

        indices.Reverse();
        return indices.ToArray();
    }

    private static Transform? WalkSiblingPath(Transform root, IReadOnlyList<int> siblingPath)
    {
        Transform current = root;
        for (int index = 0; index < siblingPath.Count; index++)
        {
            int childIndex = siblingPath[index];
            if (childIndex < 0 || childIndex >= current.childCount)
            {
                return null;
            }

            current = current.GetChild(childIndex);
        }

        return current;
    }

    private static bool RebindControllerButton(HireWorkerPanelUI panel, ControllerButton controllerButton, BepInEx.Logging.ManualLogSource? logger, int panelIndex, int buttonIndex)
    {
        bool changed = false;

        if (controllerButton.m_Button == null || !controllerButton.m_Button.transform.IsChildOf(panel.transform))
        {
            Button? liveButton = FindLiveButton(controllerButton.transform);
            if (liveButton != null)
            {
                controllerButton.m_Button = liveButton;
                changed = true;
            }
        }

        ControllerSelectorUIGrp? selector = controllerButton.m_ButtonHighlight;
        if (selector == null || !selector.transform.IsChildOf(panel.transform))
        {
            ControllerSelectorUIGrp? liveSelector = controllerButton.GetComponentInChildren<ControllerSelectorUIGrp>(true);
            if (liveSelector == null)
            {
                liveSelector = controllerButton.transform.parent != null
                    ? controllerButton.transform.parent.GetComponentInChildren<ControllerSelectorUIGrp>(true)
                    : null;
            }

            if (liveSelector != null)
            {
                controllerButton.m_ButtonHighlight = liveSelector;
                selector = liveSelector;
                changed = true;
            }
        }

        if (selector != null)
        {
            changed |= RebindSelectorGroup(selector);
        }

        logger?.LogInfo($"Custom Workers controller binding panel={panelIndex} button={buttonIndex} path={GetTransformPath(controllerButton.transform)} button={(controllerButton.m_Button != null ? GetTransformPath(controllerButton.m_Button.transform) : "<none>")} selector={(controllerButton.m_ButtonHighlight != null ? GetTransformPath(controllerButton.m_ButtonHighlight.transform) : "<none>")} changed={changed}");
        return changed;
    }

    private static bool RebindSelectorGroup(ControllerSelectorUIGrp selector)
    {
        bool changed = false;
        if (selector.m_TopUIGrp == null)
        {
            Transform? topUi = selector.transform.Find("TopUIGrp");
            if (topUi != null)
            {
                selector.m_TopUIGrp = topUi.gameObject;
                changed = true;
            }
        }

        if (selector.m_SpriteGrp == null)
        {
            selector.m_SpriteGrp = new List<Transform>();
            changed = true;
        }

        if (selector.m_SpriteGrp.Count == 0)
        {
            for (int childIndex = 0; childIndex < selector.transform.childCount; childIndex++)
            {
                Transform child = selector.transform.GetChild(childIndex);
                if (child.name.StartsWith("ControllerSelectorUIPos", System.StringComparison.Ordinal))
                {
                    selector.m_SpriteGrp.Add(child);
                    changed = true;
                }
            }
        }

        Sprite? highlightSprite = ArcRecruiterHighlightAssets.GetControllerHighlightSprite();
        if (highlightSprite != null)
        {
            Image[] images = selector.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                if (!string.Equals(image.gameObject.name, "Sprite", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (image.sprite == null)
                {
                    image.sprite = highlightSprite;
                    image.overrideSprite = null;
                    image.enabled = true;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static Button? FindLiveButton(Transform root)
    {
        Transform? btnRaycast = root.Find("BtnRaycast");
        if (btnRaycast != null)
        {
            Button? direct = btnRaycast.GetComponent<Button>();
            if (direct != null)
            {
                return direct;
            }
        }

        Button? onSelf = root.GetComponent<Button>();
        if (onSelf != null)
        {
            return onSelf;
        }

        return root.GetComponentInChildren<Button>(true);
    }

    private static string GetTransformPath(Transform transform)
    {
        var segments = new Stack<string>();
        Transform? current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", segments.ToArray());
    }
}
