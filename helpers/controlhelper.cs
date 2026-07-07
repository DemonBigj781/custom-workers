using System.Collections.Generic;
using UnityEngine;

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
}
