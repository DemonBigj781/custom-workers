using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CustomWorkers;

internal static class InputTrackingHelper
{
    internal static string DescribeButton(Button? button)
    {
        if (button == null)
        {
            return "button=<null>";
        }

        Image? image = button.GetComponent<Image>();
        return $"buttonPath={GetTransformPath(button.transform)} activeInHierarchy={button.gameObject.activeInHierarchy} interactable={button.interactable} raycastTarget={image?.raycastTarget} canvasBlockers={DescribeCanvasGroups(button.transform)}";
    }

    internal static string DescribeCanvasGroups(Transform? transform)
    {
        if (transform == null)
        {
            return "<none>";
        }

        List<string> parts = new List<string>();
        CanvasGroup[] groups = transform.GetComponentsInParent<CanvasGroup>(true);
        for (int index = 0; index < groups.Length; index++)
        {
            CanvasGroup group = groups[index];
            parts.Add($"{group.name}(active={group.gameObject.activeInHierarchy},interactable={group.interactable},blocksRaycasts={group.blocksRaycasts},alpha={group.alpha:0.##})");
        }

        return parts.Count > 0 ? string.Join(";", parts.ToArray()) : "<none>";
    }

    internal static string GetTransformPath(Transform? transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        Stack<string> segments = new Stack<string>();
        Transform? current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        StringBuilder builder = new StringBuilder();
        bool first = true;
        foreach (string segment in segments)
        {
            if (!first)
            {
                builder.Append('/');
            }

            builder.Append(segment);
            first = false;
        }

        return builder.ToString();
    }
}
