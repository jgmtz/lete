using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Lete.Core.Presentation;
using Lete.Core.State;
using Lete.Game.Debug;
using Lete.Game.UI;
using Lete.Game.World;

namespace Lete.Game;

/// <summary>
/// Cena principal do M0: o mundo 3D dentro de um SubViewport (para o teste de baixa resolução),
/// a densidade de pixel única, o HUD e o painel de debug.
/// </summary>
public partial class Main : Node
{
    /// <summary>Altura dos sprites de mapa (ARTE.md 7.8).</summary>
    [Export] public int SpriteHeightPx { get; set; } = 56;
    [Export] public float CharacterHeightMeters { get; set; } = 1.7f;

    /// <summary>Altura do personagem na tela em 1080p. Múltipla de 56 para o viewport de baixa resolução (168 = 3×).</summary>
    [Export] public int CharacterScreenHeightPx { get; set; } = 168;

    [Export] public bool LowResRender { get; set; } = true;

    private SubViewportContainer _container = null!;
    private Node3D _world = null!;
    private Player _player = null!;
    private CameraRig _cameraRig = null!;
    private Hud _hud = null!;
    private DebugPanel _debug = null!;

    public IReadOnlyList<Marker3D> TeleportPoints =>
        GetTree().GetNodesInGroup("teleport_points").OfType<Marker3D>().ToList();

    public override void _Ready()
    {
        _container = GetNode<SubViewportContainer>("ViewportContainer");
        _world = _container.GetNode<Node3D>("SubViewport/TestRoom");
        _player = _world.GetNode<Player>("Player");
        _cameraRig = _world.GetNode<CameraRig>("CameraRig");

        _hud = new Hud(_player);
        AddChild(_hud);
        _debug = new DebugPanel(this);
        AddChild(_debug);
        _hud.OpenChanged += RefreshPlayerInput;
        _debug.OpenChanged += RefreshPlayerInput;

        ApplyPixelSettings();
        _ = RunCommandLineAsync();
    }

    public void SetLowResRender(bool lowRes)
    {
        LowResRender = lowRes;
        ApplyPixelSettings();
        _debug.Refresh();
    }

    public void TeleportPlayer(Marker3D point)
    {
        _player.TeleportTo(point.GlobalPosition);
        _cameraRig.SnapToTarget();
    }

    /// <summary>Chamado quando algo muda o texto exibido (por exemplo, o gênero).</summary>
    public void OnTextsChanged() => _hud.RefreshTexts();

    private void RefreshPlayerInput() => _player.InputEnabled = !_hud.JournalOpen && !_debug.IsOpen;

    /// <summary>Aplica a mesma densidade de pixel a sprites, texturas do cenário, câmera e viewport.</summary>
    private void ApplyPixelSettings()
    {
        var viewportHeight = UI.Screen.LogicalHeight(this);
        var density = new PixelDensity(SpriteHeightPx, CharacterHeightMeters, CharacterScreenHeightPx, viewportHeight);

        _player.PixelSize = density.PixelSize;
        _cameraRig.Camera.Size = density.CameraSize;
        _container.StretchShrink = LowResRender ? density.LowResShrink : 1;

        // Superfícies do cenário usam triplanar no espaço do mundo; a escala define texels por metro.
        foreach (var node in GetTree().GetNodesInGroup("pixel_surface"))
        {
            if (node.Get("material").As<StandardMaterial3D>() is { AlbedoTexture: { } texture } material)
            {
                material.Uv1Scale = Vector3.One * density.TriplanarScale(texture.GetWidth());
            }
        }
    }

    /// <summary>
    /// Argumentos depois de "--" na linha de comando, para conferir a cena sem jogar à mão:
    /// --render=low|direct  --gender=m|f  --teleport=Nome  --examine
    /// --ui=journal|debug  --shot=caminho.png  --frames=N
    /// </summary>
    private async Task RunCommandLineAsync()
    {
        var args = OS.GetCmdlineUserArgs()
            .Select(a => a.TrimStart('-').Split('=', 2))
            .ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "", StringComparer.Ordinal);

        if (args.Count == 0)
        {
            return;
        }

        if (args.TryGetValue("render", out var render))
        {
            LowResRender = render != "direct";
        }

        if (args.TryGetValue("gender", out var gender))
        {
            GameRoot.Instance.State.Gender = gender == "f" ? Gender.Feminine : Gender.Masculine;
        }

        ApplyPixelSettings();

        if (args.TryGetValue("teleport", out var pointName) &&
            TeleportPoints.FirstOrDefault(p => p.Name == pointName) is { } point)
        {
            TeleportPlayer(point);
        }

        // Espera a física detectar as áreas de interação depois do teleporte.
        await WaitFrames(10);

        if (args.ContainsKey("examine"))
        {
            _player.CurrentInteractable?.Examine();
        }

        if (args.TryGetValue("ui", out var ui))
        {
            if (ui == "journal")
            {
                _hud.SetJournalOpen(true);
            }
            else if (ui == "debug")
            {
                _debug.SetOpen(true);
            }
        }

        if (args.TryGetValue("shot", out var shotPath))
        {
            await WaitFrames(args.TryGetValue("frames", out var frames) ? int.Parse(frames) : 30);
            var error = GetViewport().GetTexture().GetImage().SavePng(shotPath);
            GD.Print($"screenshot: {shotPath} ({error})");
            GetTree().Quit();
        }
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}
