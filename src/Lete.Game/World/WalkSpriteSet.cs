using System.Collections.Generic;
using Godot;
using Lete.Core.Movement;

namespace Lete.Game.World;

/// <summary>
/// Onde estão os quadros de andar e de parado de um personagem, seguindo a convenção de
/// <c>art/sprites</c>: <c>andar_56px/&lt;dir&gt;/&lt;prefixo&gt;_&lt;dir&gt;_NN.png</c> e
/// <c>parado/&lt;prefixo&gt;_parado_&lt;dir&gt;.png</c>.
/// </summary>
[GlobalClass]
public partial class WalkSpriteSet : Resource
{
    private static readonly Dictionary<ScreenDirection, string> FolderNames = new()
    {
        [ScreenDirection.Down] = "frente",
        [ScreenDirection.Up] = "costas",
        [ScreenDirection.Left] = "esquerda",
        [ScreenDirection.Right] = "direita",
    };

    [Export(PropertyHint.Dir)] public string Folder { get; set; } = "";
    [Export] public string Prefix { get; set; } = "";
    [Export] public string WalkFolder { get; set; } = "andar_56px";
    [Export] public int WalkFrameCount { get; set; } = 12;
    [Export] public float WalkFps { get; set; } = 16f;

    public static string WalkAnimation(ScreenDirection direction) => $"walk_{direction}";

    public static string IdleAnimation(ScreenDirection direction) => $"idle_{direction}";

    /// <summary>Monta as animações andar/parado das quatro direções.</summary>
    public SpriteFrames BuildFrames()
    {
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");

        foreach (var (direction, dir) in FolderNames)
        {
            var walk = WalkAnimation(direction);
            frames.AddAnimation(walk);
            frames.SetAnimationSpeed(walk, WalkFps);
            for (var i = 1; i <= WalkFrameCount; i++)
            {
                frames.AddFrame(walk, GD.Load<Texture2D>($"{Folder}/{WalkFolder}/{dir}/{Prefix}_{dir}_{i:00}.png"));
            }

            var idle = IdleAnimation(direction);
            frames.AddAnimation(idle);
            frames.AddFrame(idle, GD.Load<Texture2D>($"{Folder}/parado/{Prefix}_parado_{dir}.png"));
        }

        return frames;
    }
}
