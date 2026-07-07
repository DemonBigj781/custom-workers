using UnityEngine;

namespace CustomWorkers;

internal static class HireHelper
{
    internal static bool EnsureHireWorkerPanels(HireWorkerScreen? screen)
    {
        if (screen?.m_HireWorkerPanelUIList == null || screen.m_HireWorkerPanelUIList.Count == 0)
        {
            return false;
        }

        int requiredCount = CSingleton<WorkerManager>.Instance?.m_WorkerDataList?.Count ?? screen.m_HireWorkerPanelUIList.Count;
        int panelCountBefore = screen.m_HireWorkerPanelUIList.Count;
        while (screen.m_HireWorkerPanelUIList.Count < requiredCount)
        {
            HireWorkerPanelUI template = screen.m_HireWorkerPanelUIList[screen.m_HireWorkerPanelUIList.Count - 1];
            GameObject cloneObject = Object.Instantiate(template.gameObject, template.transform.parent);
            cloneObject.name = $"HireWorkerPanelUI_{screen.m_HireWorkerPanelUIList.Count}";
            HireWorkerPanelUI clonePanel = cloneObject.GetComponent<HireWorkerPanelUI>();
            NormalizeClonedPanelLayout(template, clonePanel);
            clonePanel.SetActive(false);
            screen.m_HireWorkerPanelUIList.Add(clonePanel);
        }

        bool layoutRefreshed = HireWorkerLayoutRules.NeedsLayoutRefresh(panelCountBefore, screen.m_HireWorkerPanelUIList.Count);
        bool controllerRowsExpanded = ControlHelper.EnsureHireWorkerControllerRows(screen);
        return layoutRefreshed || controllerRowsExpanded;
    }

    private static void NormalizeClonedPanelLayout(HireWorkerPanelUI template, HireWorkerPanelUI clone)
    {
        if (template == null || clone == null)
        {
            return;
        }

        if (template.transform is RectTransform templateRect && clone.transform is RectTransform cloneRect)
        {
            cloneRect.anchorMin = templateRect.anchorMin;
            cloneRect.anchorMax = templateRect.anchorMax;
            cloneRect.pivot = templateRect.pivot;
            cloneRect.sizeDelta = templateRect.sizeDelta;
            cloneRect.anchoredPosition3D = templateRect.anchoredPosition3D;
            cloneRect.localScale = templateRect.localScale;
            cloneRect.localRotation = templateRect.localRotation;
        }
    }
}
