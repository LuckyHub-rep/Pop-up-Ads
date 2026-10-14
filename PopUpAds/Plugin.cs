using Dalamud.IoC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using System.IO;
using NAudio.Wave;
using System.Numerics;

namespace PopUpAds;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new("PopImage");
    private readonly Random random = new();

    private readonly List<ISharedImmediateTexture> images = new();

    private readonly Configuration config;
    private readonly List<PopupWindow> popupWindows = new();
    private readonly SettingsWindow settingsWindow;

    private long nextPopupTimestamp;
    private bool disposed;
    private byte[]? popupSoundData;
    private WaveOut? soundOutput;
    private WaveStream? soundStream;   

    public Plugin()
    {
        config = PluginInterface.GetPluginConfig() as Configuration
                 ?? new Configuration();

        LoadEmbeddedImages();

        LoadPopupSound();

        settingsWindow = new SettingsWindow(
            config,
            OnSettingsChanged,
            () => PluginInterface.SavePluginConfig(config));

        windowSystem.AddWindow(settingsWindow);

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleSettings;

        CommandManager.AddHandler(
            "/pua",
            new CommandInfo(OnCommand)
            {
                HelpMessage =
                    "Open Pop-up ads settings. Use /pua test to show a popup."
            });

        ScheduleNextPopup();
    }

    private void LoadPopupSound()
    {
        var assembly = Assembly.GetExecutingAssembly();

        string? resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(name =>
                name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));

        if (resource == null)
        {
            Log.Warning("PopUpAds: No embedded WAV sound found.");
            return;
        }

        using var stream = assembly.GetManifestResourceStream(resource);

        if (stream == null)
        {
            Log.Warning($"PopUpAds: Could not open sound resource {resource}");
            return;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        popupSoundData = memory.ToArray();

        Log.Information($"PopUpAds: Loaded sound resource {resource}");
    }

    private void PlayPopupSound()
    {
        if (popupSoundData == null || popupSoundData.Length == 0)
            return;

        soundOutput?.Stop();
        soundOutput?.Dispose();
        soundOutput = null;

        soundStream?.Dispose();
        soundStream = null;

        try
        {
            var reader = new WaveFileReader(new MemoryStream(popupSoundData));
            soundStream = reader;

            soundOutput = new WaveOut();
            soundOutput.Volume = Math.Clamp(config.SoundVolume, 0f, 1f);
            soundOutput.Init(reader);
            soundOutput.Play();
        }
        catch (Exception ex)
        {
            Log.Error($"PopUpAds: Failed to play popup sound: {ex}");

            soundOutput?.Dispose();
            soundOutput = null;

            soundStream?.Dispose();
            soundStream = null;
        }
    }

    private void LoadEmbeddedImages()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        Log.Information(
            "Embedded resources: {Resources}",
            string.Join(", ", assembly.GetManifestResourceNames()));

        string[] resources = assembly.GetManifestResourceNames()
            .Where(name =>
                name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (string resource in resources)
        {
            try
            {
                images.Add(
                    TextureProvider.GetFromManifestResource(
                        assembly,
                        resource));

                Log.Information(
                    "Loaded popup image resource: {Resource}",
                    resource);
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Failed to load popup image resource {Resource}",
                    resource);
            }
        }

        if (images.Count == 0)
        {
            Log.Warning(
                "No embedded popup images found. Check the resource names "
                + "and project build settings.");
        }
    }

    private void DrawUi()
    {
        if (disposed)
            return;

        TickScheduler();
        windowSystem.Draw();
    }

    private void TickScheduler()
    {
        if (!config.Enabled || images.Count == 0)
         return;

        if (Stopwatch.GetTimestamp() < nextPopupTimestamp)
          return;

         // Schedule the next popup immediately, without waiting
         // for any currently open popup to close.
        ScheduleNextPopup();

        ShowRandomPopup();
    }

    
    private void ShowRandomPopup()
    {
        if (images.Count == 0 || !config.Enabled)
            return;

        ISharedImmediateTexture image =
            images[random.Next(images.Count)];

        var viewport = ImGui.GetMainViewport();

        var popup = new PopupWindow(
            random);

        popup.Show(
            image,
            viewport.Pos,
            viewport.Size);

        PlayPopupSound();

        popupWindows.Add(popup);
        windowSystem.AddWindow(popup);
    }


    private void ScheduleNextPopup()
    {
        int average = Math.Clamp(
            config.AverageIntervalSeconds,
            10,
            600);

        int variation = Math.Clamp(
            config.IntervalVariationPercent,
            0,
            90);

        int minSeconds = Math.Max(
            1,
            average * (100 - variation) / 100);

        int maxSeconds = Math.Max(
            minSeconds,
            average * (100 + variation) / 100);

        int delaySeconds = random.Next(
            minSeconds,
            maxSeconds + 1);

        nextPopupTimestamp = Stopwatch.GetTimestamp()
            + (long)(delaySeconds * (double)Stopwatch.Frequency);
    }


    private void OnSettingsChanged()
    {
        if (!config.Enabled)
        {
            foreach (var popup in popupWindows)
                popup.IsOpen = false;
        }

        ScheduleNextPopup();
    }

    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals(
                "test",
                StringComparison.OrdinalIgnoreCase))
        {
            ShowRandomPopup();
            return;
        }

        settingsWindow.Toggle();
    }

    private void ToggleSettings()
    {
        settingsWindow.Toggle();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleSettings;

        CommandManager.RemoveHandler("/pua");

        windowSystem.RemoveAllWindows();
        popupWindows.Clear();

        images.Clear();
    }
}
