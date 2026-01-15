using Content.Shared._Metro14.FlameThrower;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Client._Metro14.ClientFlamethrower;

public sealed class FlamethrowerClientSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = null!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlameThrowerComponent, InteractUsingEvent>(OnUsingInteract);
        SubscribeLocalEvent<FlameThrowerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FlameThrowerComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    /// <summary>
    /// Метод обрабатывающий событие использования чего - то на огнемёте.
    /// Мы ожидаем, что это зажгёт огнемёт, иначе игнорируем.
    /// </summary>
    private void OnUsingInteract(EntityUid uid, FlameThrowerComponent component, InteractUsingEvent args)
    {
        if (component.Ignited || !TryComp<IgnitionSourceComponent>(args.Used, out var ignitionSource) || !ignitionSource.Ignited)
            return;

        TryIgniteFlameThrower(uid, component);
    }

    /// <summary>
    /// Обрабатываем событие когда мы используем огнемёт, чтобы сделать что - то.
    /// Мы ожидаем, что огнемёт будет взаимодействовать с источником огня, чтобы подпечься, иначе игнорируем.
    /// </summary>
    private void OnAfterInteract(EntityUid uid, FlameThrowerComponent component, AfterInteractEvent args)
    {
        if (component.Ignited || !TryComp<IgnitionSourceComponent>(args.Target, out var ignitionSource) || !ignitionSource.Ignited)
            return;

        TryIgniteFlameThrower(uid, component);
    }

    /// <summary>
    /// Метод предназначенный для "Зажигания" огнемёта и его последующей работы
    /// </summary>
    private void TryIgniteFlameThrower(EntityUid uid, FlameThrowerComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearanceSystem.SetData(uid, FlameThrower.Base, true, appearance);
        component.IgnitionTime = component.IgnitionTimeOnStart; // Start time
        component.Ignited = true; // FIRE!
    }

    /// <summary>
    /// Проверка на стороне клиента на наличие проблем зажигания у огнемёта
    /// </summary>
    private void OnAttemptShoot(EntityUid uid, FlameThrowerComponent component, ref AttemptShootEvent args)
    {
        if (args.Cancelled || component.Ignited)
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString("flame-fire-shot-not-ignited");
        _audioSystem.PlayPredicted(component.ShotWithoutFire, uid, args.User);
    }
}
