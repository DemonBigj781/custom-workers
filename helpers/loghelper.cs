using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace CustomWorkers;

internal static class LogHelper
{
    internal static void WriteGeneratedWorkerJson(ManualLogSource logger, IReadOnlyList<GeneratedWorkerLogEntry> entries)
    {
        try
        {
            string pluginDirectory = Path.Combine(Paths.PluginPath, "CustomWorkers");
            Directory.CreateDirectory(pluginDirectory);
            string outputPath = Path.Combine(pluginDirectory, GeneratedWorkerLogRules.FileName);
            File.WriteAllText(outputPath, GeneratedWorkerLogRules.BuildJson(entries));
            logger.LogInfo($"Custom Workers wrote generated worker log to {outputPath}.");
        }
        catch (System.Exception ex)
        {
            logger.LogError($"Custom Workers failed to write generated worker log: {ex}");
        }
    }

    internal static void LogHireWorkerDiagnostics(ManualLogSource logger, HireWorkerScreen screen, FieldInfo maxPosXField)
    {
        int panelCount = screen.m_HireWorkerPanelUIList?.Count ?? 0;
        int controllerRowCount = screen.m_ControllerScreenUIExtension?.m_ControllerBtnColumnList?.Count ?? 0;
        string maxPosText = "n/a";
        if (maxPosXField?.GetValue(screen) is float maxPos)
        {
            maxPosText = maxPos.ToString("0.##");
        }

        logger.LogInfo($"Custom Workers hire diagnostics: panels={panelCount}, controllerRows={controllerRowCount}, maxPosX={maxPosText}");

        if (screen.m_HireWorkerPanelUIList == null)
        {
            return;
        }

        int diagnosticStartIndex = System.Math.Max(0, panelCount - 8);
        if (panelCount >= 10)
        {
            diagnosticStartIndex = System.Math.Min(diagnosticStartIndex, 8);
        }

        for (int index = diagnosticStartIndex; index < panelCount; index++)
        {
            HireWorkerPanelUI panel = screen.m_HireWorkerPanelUIList[index];
            bool panelActive = panel != null && panel.IsActive();
            string panelName = panel?.m_NameText != null ? panel.m_NameText.text : "<none>";
            string iconResolution = "<none>";
            if (panel?.m_IconImage?.sprite?.texture != null)
            {
                Texture2D texture = panel.m_IconImage.sprite.texture;
                iconResolution = $"{texture.width}x{texture.height}";
            }

            logger.LogInfo($"Custom Workers hire panel[{index}] active={panelActive} name={panelName} iconTex={iconResolution}");
        }

        if (screen.m_ControllerScreenUIExtension != null)
        {
            logger.LogInfo(
                $"Custom Workers controller state: currentX={screen.m_ControllerScreenUIExtension.m_CurrentCtrlBtnXIndex} currentY={screen.m_ControllerScreenUIExtension.m_CurrentCtrlBtnYIndex}");
        }
    }
}
