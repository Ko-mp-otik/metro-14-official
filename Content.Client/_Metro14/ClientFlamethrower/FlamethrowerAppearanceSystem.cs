using Content.Shared._Metro14.FlameThrower;
using Robust.Client.GameObjects;

namespace Content.Client._Metro14.ClientFlamethrower;

/// <summary>
/// Обрабатывает изменение состояния спрайта огнемёта
/// </summary>
public sealed class FlamethrowerAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlameThrowerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, FlameThrowerComponent component, AppearanceChangeEvent args)
    {
        if (args.Sprite is null || !args.AppearanceData.TryGetValue(FlameThrower.Base, out var fire))
            return;

        _sprite.LayerSetRsiState((uid, args.Sprite), FlameThrower.Base, fire is true ? "fire" : "base");
    }
}
