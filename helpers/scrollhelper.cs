using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CustomWorkers;

internal static class ScrollHelper
{
    internal static void RefreshExpandedHireWorkerLayout(HireWorkerScreen screen, FieldInfo scrollEndPosParentField, MethodInfo evaluateScrollerMethod)
    {
        for (int index = 0; index < screen.m_HireWorkerPanelUIList.Count; index++)
        {
            screen.m_HireWorkerPanelUIList[index].transform.SetSiblingIndex(index);
        }

        scrollEndPosParentField?.SetValue(screen, screen.m_HireWorkerPanelUIList[screen.m_HireWorkerPanelUIList.Count - 1].gameObject);
        Canvas.ForceUpdateCanvases();
        if (screen.m_VerticalLayoutGrp is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        if (evaluateScrollerMethod?.Invoke(screen, null) is IEnumerator enumerator)
        {
            screen.StartCoroutine(enumerator);
        }
    }
}
