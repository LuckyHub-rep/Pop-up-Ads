using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;

namespace PopUpAds;

public sealed class PopupWindow : Window
{
    private readonly Random random;

    private ISharedImmediateTexture? texture;



    public PopupWindow(Random random)
        : base($"Pop-up Advertisement##Popup_{Guid.NewGuid()}")
    {
        this.random = random;

        Size = new Vector2(390, 310);
        SizeCondition = ImGuiCond.FirstUseEver;

        Flags = ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse;

        ShowCloseButton = true;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
    }



    public void Show(
        ISharedImmediateTexture image,
        Vector2 viewportPosition,
        Vector2 viewportSize)
    {
        texture = image;

        const float windowWidth = 390;
        const float windowHeight = 310;
        const float margin = 12;

        float maxX = Math.Max(
            viewportPosition.X + margin,
            viewportPosition.X + viewportSize.X
                - windowWidth - margin);

        float maxY = Math.Max(
            viewportPosition.Y + margin,
            viewportPosition.Y + viewportSize.Y
                - windowHeight - margin);

        float x = maxX > viewportPosition.X + margin
            ? RandomRange(viewportPosition.X + margin, maxX)
            : viewportPosition.X + margin;

        float y = maxY > viewportPosition.Y + margin
            ? RandomRange(viewportPosition.Y + margin, maxY)
            : viewportPosition.Y + margin;

        Position = new Vector2(x, y);
        PositionCondition = ImGuiCond.Always;

        IsOpen = true;
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    public override void Draw()
    {
        if (texture is not null
            && texture.TryGetWrap(out var wrap, out _)
            && wrap is not null)
        {
            var imageSize = new Vector2(340, 220);

            float scale = Math.Min(
                imageSize.X / wrap.Width,
                imageSize.Y / wrap.Height);

            var fittedSize = new Vector2(
                wrap.Width * scale,
                wrap.Height * scale);

            ImGui.SetCursorPosX(
                Math.Max(
                    ImGui.GetCursorPosX(),
                    (ImGui.GetWindowWidth() - fittedSize.X) / 2));

            ImGui.Image(wrap.Handle, fittedSize);
        }
        else
        {
            ImGui.TextUnformatted("Image is not ready.");
        }

        ImGui.Spacing();

        float buttonWidth = 100;
        ImGui.SetCursorPosX(
            (ImGui.GetWindowWidth() - buttonWidth) / 2);

        if (ImGui.Button("Close", new Vector2(buttonWidth, 0)))
        {
            IsOpen = false;
        }
    }

    public override void OnClose()
    {
        texture = null;
    }
}
