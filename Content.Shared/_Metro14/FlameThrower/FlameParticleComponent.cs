using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Metro14.FlameThrower;

/// <summary>
/// Используется для отслеживания зарядов пламени которые могут поджечь, кого или что-либо.
/// </summary>
[RegisterComponent]
public sealed partial class FlameParticleComponent : Component
{
    /// <summary>
    /// Отвечает за то какой огонь будет ложиться на пол
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("floorFireProto")]
    public EntProtoId FireProto = "DefaultFloorFire";

    /// <summary>
    /// Отвечает за то какой шанц, что пламя при коллизии/достижении конца полёта
    /// оставит на своём месте лужу огня.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float FireDropProb = 0.2f; // 20% Сбросить на пол лужу огня

    /// <summary>
    /// Поле которое используется, для того, чтобы понять сколько стаков
    /// "Поджога" будет добавлено к объекту при соприкосновении.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fireStacks")]
    public float Stacks = 2f;

    /// <summary>
    /// Урон наносящийся при прямом попадании зарядом пламени
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public DamageSpecifier Damage = new();
}
