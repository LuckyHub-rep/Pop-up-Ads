using Dalamud.Configuration;
using System;

namespace PopUpAds;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

     // Average target interval in seconds.
    public int AverageIntervalSeconds { get; set; } = 60;

    // Random interval is 50%-150% of the configured average.
    public int IntervalVariationPercent { get; set; } = 50;

    public int ImageWidth { get; set; } = 360;

    public int ImageHeight { get; set; } = 260;

    public float SoundVolume { get; set; } = 0.5f;
    
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
