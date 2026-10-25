using System.Collections.Generic;
using System.Linq;
using Godot;
using Lete.Core.Combat;
using Lete.Core.Data;
using Lete.Game.World;

namespace Lete.Game.Combat;

/// <summary>
/// Palco da batalha: lê a configuração (<c>content/battle</c>), põe grupo e sombras nas posições do layout,
/// ajusta a câmera para a altura na tela e aplica uma versão de arte. Precisa de um filho "CameraRig".
/// </summary>
public partial class BattleStage : Node3D
{
    [Export(PropertyHint.File, "*.json")] public string SetupPath { get; set; } = "res://content/battle/teste_batalha.json";
    [Export] public string ArtRoot { get; set; } = "res://art/sprites/batalha";

    /// <summary>
    /// Altura na tela (1080p) de um participante de altura 1,0. 640 = 128 × 5 = 160 × 4: as versões
    /// pixelizadas ficam com escala inteira, e a de 160 (a escolhida) com o mesmo 4× do mapa.
    /// </summary>
    [Export] public int ScreenHeightPx { get; set; } = 640;

    [Export] public BattleAnimTuning Tuning { get; set; } = null!;

    public BattleSetup Setup { get; private set; } = null!;
    public List<(BattleActor Node, BattleActorArt Art)> Party { get; } = new();
    public List<(BattleActor Node, BattleActorArt Art)> Enemies { get; } = new();
    public Camera3D Camera { get; private set; } = null!;
    public int Style { get; private set; }

    public override void _Ready()
    {
        // -- --setup=<id> troca a configuração (ex.: teste_septimia), sem mexer na cena.
        var setupArg = System.Array.Find(OS.GetCmdlineUserArgs(), x => x.StartsWith("--setup="));
        if (setupArg is not null)
        {
            SetupPath = $"{SetupPath.GetBaseDir()}/{setupArg["--setup=".Length..]}.json";
        }

        Setup = BattleData.FromJson(FileAccess.GetFileAsString(SetupPath));

        var rig = GetNode<CameraRig>("CameraRig");
        Camera = rig.Camera;
        var viewportHeight = UI.Screen.LogicalHeight(this);
        Camera.Size = viewportHeight / (float)ScreenHeightPx;
        var worldPerScreenPixel = Camera.Size / viewportHeight;

        var basis = Camera.GlobalBasis;
        var right = new Vector3(basis.X.X, 0, basis.X.Z).Normalized();
        var forward = new Vector3(-basis.Z.X, 0, -basis.Z.Z).Normalized();

        BattleActor Spawn(BattleActorArt art, System.Numerics.Vector2 ground, bool spirit)
        {
            var actor = new BattleActor { Name = art.Id, Tuning = Tuning, WorldPerScreenPixel = worldPerScreenPixel, IsSpirit = spirit };
            AddChild(actor);
            actor.Position = right * ground.X + forward * ground.Y;
            actor.FaceCamera(rig.Rotation.Y, Mathf.DegToRad(rig.PitchDegrees));
            actor.SetDepthScale(Setup.Layout.ScaleAt(ground));
            return actor;
        }

        for (var i = 0; i < Setup.Party.Count; i++)
        {
            Party.Add((Spawn(Setup.Party[i], Setup.Layout.Party[i], spirit: false), Setup.Party[i]));
        }

        for (var i = 0; i < Setup.Enemies.Count; i++)
        {
            Enemies.Add((Spawn(Setup.Enemies[i], Setup.Layout.Enemies[i], spirit: true), Setup.Enemies[i]));
        }

        // Para conferir sem jogar: -- --art=<id do estilo> escolhe a arte inicial.
        var artArg = System.Array.Find(OS.GetCmdlineUserArgs(), x => x.StartsWith("--art="));
        var initial = artArg is null ? 0 : Setup.Styles.ToList().FindIndex(st => st.Id == artArg["--art=".Length..]);
        ApplyStyle(System.Math.Max(0, initial));

        foreach (var (node, _) in Party.Concat(Enemies))
        {
            node.StartIdle();
        }
    }

    public void ApplyStyle(int index)
    {
        if (index >= Setup.Styles.Count)
        {
            return;
        }

        Style = index;
        var style = Setup.Styles[index];
        foreach (var (node, art) in Party.Concat(Enemies))
        {
            var texture = GD.Load<Texture2D>($"{ArtRoot}/{style.Folder}/{art.Art}.png");
            var tint = art.Tint is { } hex ? new Color(hex) : Colors.White;
            node.SetArt(texture, art.Height, style.Smooth, art.FlipH, tint);
        }

        // Quantos pixels de tela cada pixel da arte ocupa (inteiro = pixels todos iguais).
        var reference = GD.Load<Texture2D>($"{ArtRoot}/{style.Folder}/{Party[0].Art.Art}.png");
        var scale = ScreenHeightPx * Party[0].Art.Height / reference.GetHeight();
        GD.Print($"Arte '{style.Id}': {reference.GetHeight()} px de altura, {scale:0.##} px de tela por px de arte");
    }
}
