using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CustomWorkers;

internal static class ScrollHelper
{
    internal static void RefreshExpandedHireWorkerLayout(
        HireWorkerScreen screen,
        FieldInfo scrollEndPosParentField,
        MethodInfo evaluateScrollerMethod,
        FieldInfo maxPosXField,
        FieldInfo posXField,
        FieldInfo lerpPosXField,
        FieldInfo maxPosFoundField,
        FieldInfo maxPosAccurateFoundField)
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

        ClampExpandedHireWorkerScroll(screen, maxPosXField, posXField, lerpPosXField, maxPosFoundField, maxPosAccurateFoundField);
    }

    private static void ClampExpandedHireWorkerScroll(
        HireWorkerScreen screen,
        FieldInfo maxPosXField,
        FieldInfo posXField,
        FieldInfo lerpPosXField,
        FieldInfo maxPosFoundField,
        FieldInfo maxPosAccurateFoundField)
    {
        HireWorkerPanelUI? lastActivePanel = null;
        for (int index = screen.m_HireWorkerPanelUIList.Count - 1; index >= 0; index--)
        {
            HireWorkerPanelUI? panel = screen.m_HireWorkerPanelUIList[index];
            if (panel != null && panel.gameObject.activeSelf)
            {
                lastActivePanel = panel;
                break;
            }
        }

        if (lastActivePanel == null || screen.m_SliderGrp == null)
        {
            return;
        }

        float originalPos = posXField?.GetValue(screen) as float? ?? 0f;
        float originalLerpPos = lerpPosXField?.GetValue(screen) as float? ?? 0f;

        screen.ScrollToUI(lastActivePanel.gameObject, instantSnapToPos: true, offsetY: 0f);

        float computedMaxPos = posXField?.GetValue(screen) as float? ?? 0f;
        if (computedMaxPos < 0f)
        {
            computedMaxPos = 0f;
        }

        maxPosXField?.SetValue(screen, computedMaxPos);
        maxPosFoundField?.SetValue(screen, true);
        maxPosAccurateFoundField?.SetValue(screen, true);

        float clampedPos = Mathf.Clamp(originalPos, 0f, computedMaxPos);
        float clampedLerpPos = Mathf.Clamp(originalLerpPos, 0f, computedMaxPos);
        posXField?.SetValue(screen, clampedPos);
        lerpPosXField?.SetValue(screen, clampedLerpPos);
        screen.m_SliderGrp.transform.localPosition = new Vector3(
            screen.m_SliderGrp.transform.localPosition.x,
            clampedLerpPos,
            screen.m_SliderGrp.transform.localPosition.z);
    }
}
