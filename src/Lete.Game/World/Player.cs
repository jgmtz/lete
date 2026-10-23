using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Lete.Core.Movement;

namespace Lete.Game.World;

/// <summary>
/// Protagonista no mapa: anda nas direções da tela (relativas à câmera diagonal),
/// troca o quadro do sprite (frente/costas + espelho) e examina objetos próximos.
/// </summary>
public partial class Player : CharacterBody3D
{
    private const string SpritePathFormat = "res://art/sprites/placeholder/protagonista_{0}_{1}px.png";

    /// <summary>Velocidade de caminhada em metros por segundo (sensação de controle, não balanceamento).</summary>
    [Export] public float MoveSpeed { get; set; } = 4f;

    /// <summary>Balanço vertical ao andar, em pixels do sprite (animação por código no lugar de quadros).</summary>
    [Export] public float WalkBobPixels { get; set; } = 1f;
    [Export] public float WalkBobFrequency { get; set; } = 8f;

    private readonly List<Interactable> _nearby = new();
    private Sprite3D _sprite = null!;
    private Texture2D? _front;
    private Texture2D? _back;
    private Facing _facing = Facing.SouthEast;
    private float _walkTime;

    /// <summary>Quando falso, ignora movimento e interação (por exemplo, com um painel aberto).</summary>
    public bool InputEnabled { get; set; } = true;

    public event Action? NearbyChanged;

    public Interactable? CurrentInteractable =>
        _nearby.OrderBy(i => i.GlobalPosition.DistanceSquaredTo(GlobalPosition)).FirstOrDefault();

    public override void _Ready()
    {
        _sprite = GetNode<Sprite3D>("Sprite");
    }

    /// <summary>Troca os sprites para a altura pedida e ajusta o tamanho do pixel no mundo.</summary>
    public void SetSpriteSize(int heightPx, float pixelSize)
    {
        _front = GD.Load<Texture2D>(string.Format(SpritePathFormat, "frente", heightPx));
        _back = GD.Load<Texture2D>(string.Format(SpritePathFormat, "costas", heightPx));
        _sprite.PixelSize = pixelSize;
        UpdateSprite();
    }

    public void AddNearby(Interactable interactable)
    {
        if (!_nearby.Contains(interactable))
        {
            _nearby.Add(interactable);
            NearbyChanged?.Invoke();
        }
    }

    public void RemoveNearby(Interactable interactable)
    {
        if (_nearby.Remove(interactable))
        {
            NearbyChanged?.Invoke();
        }
    }

    public void TeleportTo(Vector3 position)
    {
        GlobalPosition = position;
        Velocity = Vector3.Zero;
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = InputEnabled
            ? Input.GetVector("move_left", "move_right", "move_up", "move_down")
            : Vector2.Zero;

        Velocity = ScreenToWorld(input) * MoveSpeed;
        MoveAndSlide();

        _facing = FacingRules.Resolve(input.X, input.Y, _facing);
        _walkTime = input == Vector2.Zero ? 0f : _walkTime + (float)delta;
        UpdateSprite();
    }

    public override void _Process(double delta)
    {
        // Input global: o jogador vive dentro do SubViewport, mas a ação vale para o jogo todo.
        if (InputEnabled && Input.IsActionJustPressed("interact") && CurrentInteractable is { } target)
        {
            target.Examine();
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
        return right * input.X + forward * -input.Y;
    }

    private void UpdateSprite()
    {
        if (_front is null || _back is null)
        {
            return;
        }

        var texture = FacingRules.IsBackView(_facing) ? _back : _front;
        _sprite.Texture = texture;
        _sprite.FlipH = FacingRules.IsMirrored(_facing);

        // Pés na origem do nó; o balanço sobe o sprite em pixels inteiros.
        var bob = Mathf.Round(Mathf.Abs(Mathf.Sin(_walkTime * WalkBobFrequency)) * WalkBobPixels);
        _sprite.Offset = new Vector2(0, texture.GetHeight() / 2f + bob);
    }
}
