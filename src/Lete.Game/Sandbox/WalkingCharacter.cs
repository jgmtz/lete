using System.Collections.Generic;
using System.Linq;
using Godot;
using Lete.Core.Movement;

namespace Lete.Game.Sandbox;

/// <summary>
/// Personagem de teste que anda nas quatro direções da tela com as animações de andar/parado
/// e troca entre os conjuntos de sprites com <c>switch_character</c>.
/// </summary>
public partial class WalkingCharacter : CharacterBody3D
{
    /// <summary>Velocidade de caminhada em metros por segundo (sensação de controle, não balanceamento).</summary>
    [Export] public float MoveSpeed { get; set; } = 4f;

    /// <summary>
    /// Multiplica a velocidade enquanto <c>run</c> está pressionado. A animação acelera na mesma
    /// proporção, para os pés continuarem acompanhando o chão.
    /// </summary>
    [Export] public float RunMultiplier { get; set; } = 1.75f;

    [Export] public WalkSpriteSet[] Characters { get; set; } = System.Array.Empty<WalkSpriteSet>();

    private List<SpriteFrames> _frames = new();
    private AnimatedSprite3D _sprite = null!;
    private int _current;
    private ScreenDirection _direction = ScreenDirection.Down;

    public float PixelSize
    {
        get => _sprite.PixelSize;
        set => _sprite.PixelSize = value;
    }

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite3D>("Sprite");
        _frames = Characters.Select(c => c.BuildFrames()).ToList();
        ShowCharacter(0);
    }

    public override void _PhysicsProcess(double delta)
    {
        var raw = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var input = WalkRules.LimitLength(new System.Numerics.Vector2(raw.X, raw.Y));

        var speedScale = Input.IsActionPressed("run") ? RunMultiplier : 1f;
        Velocity = ScreenToWorld(new Vector2(input.X, input.Y)) * MoveSpeed * speedScale;
        MoveAndSlide();

        _direction = WalkRules.Resolve(input, _direction);
        var animation = input == System.Numerics.Vector2.Zero
            ? WalkSpriteSet.IdleAnimation(_direction)
            : WalkSpriteSet.WalkAnimation(_direction);
        _sprite.SpeedScale = speedScale;
        if (_sprite.Animation != animation)
        {
            _sprite.Play(animation);
        }
    }

    public override void _Process(double delta)
    {
        if (_frames.Count > 1 && Input.IsActionJustPressed("switch_character"))
        {
            ShowCharacter((_current + 1) % _frames.Count);
        }
    }

    private void ShowCharacter(int index)
    {
        if (_frames.Count == 0)
        {
            return;
        }

        _current = index;
        var animation = _sprite.Animation;
        var frame = _sprite.Frame;
        _sprite.SpriteFrames = _frames[index];

        // Pés na origem do nó: todos os quadros têm a mesma altura e a base é o chão.
        var height = _frames[index].GetFrameTexture(WalkSpriteSet.IdleAnimation(ScreenDirection.Down), 0).GetHeight();
        _sprite.Offset = new Vector2(0, height / 2f);

        if (_frames[index].HasAnimation(animation))
        {
            _sprite.Play(animation);
            _sprite.Frame = frame;
        }
        else
        {
            _sprite.Play(WalkSpriteSet.IdleAnimation(_direction));
        }
    }

    /// <summary>Converte a entrada da tela (y para baixo) numa direção no chão, segundo a câmera atual.</summary>
    private Vector3 ScreenToWorld(Vector2 input)
    {
        var camera = GetViewport().GetCamera3D();
        if (camera is null || input == Vector2.Zero)
        {
            return Vector3.Zero;
        }

        var basis = camera.GlobalBasis;
        var right = new Vector3(basis.X.X, 0, basis.X.Z).Normalized();
        var forward = new Vector3(-basis.Z.X, 0, -basis.Z.Z).Normalized();

        // A câmera inclinada encurta na tela o que se anda "para dentro" (1 m vira ~sen(inclinação)).
        // Dividir por esse encurtamento deixa a velocidade na tela igual em todas as direções.
        var screenShortening = Mathf.Max(Mathf.Abs(forward.Dot(basis.Y)), 0.1f);
        return right * input.X + forward * (-input.Y / screenShortening);
    }
}
