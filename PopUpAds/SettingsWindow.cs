
using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PopUpAds;

public sealed class SettingsWindow : Window
{
    private readonly Action saveConfig;
    private readonly Configuration config;
    private readonly Action onSettingsChanged;

    public SettingsWindow(
        Configuration config,
        Action onSettingsChanged,
        Action saveConfig)
        : base("PopImage Settings")
    {
        this.config = config;
        this.onSettingsChanged = onSettingsChanged;
        this.saveConfig = saveConfig;

        Size = new System.Numerics.Vector2(360, 180);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        bool enabled = config.Enabled;

        if (ImGui.Checkbox("Enable random popups", ref enabled))
        {
            config.Enabled = enabled;
            config.Save();
            onSettingsChanged();
        }

        ImGui.Separator();

        int interval = config.AverageIntervalSeconds;

        if (ImGui.SliderInt(
            "Average interval (seconds)",
            ref interval,
            10,
            600))
        {
            config.AverageIntervalSeconds = interval;
            config.Save();
            onSettingsChanged();
        }

        ImGui.TextWrapped(
            "Popups appear at random intervals around the average. "
            + "Close a popup to schedule the next one.");

        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Text("Popup sound");

        float soundVolume = config.SoundVolume * 100f;

        if (ImGui.SliderFloat("Sound volume", ref soundVolume, 0f, 100f, "%.0f%%"))
        {
            config.SoundVolume = soundVolume / 100f;
            saveConfig();
        }

    }
}
