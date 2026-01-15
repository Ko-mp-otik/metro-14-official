using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Decals;
using Content.Shared._Metro14.FlameThrower;
using Content.Shared.Atmos.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Decals;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Metro14.Flamethrower;

public sealed class FlamethrowerServerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly DecalSystem _decalSys = null!;
    [Dependency] private readonly SharedGunSystem _gunSys = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly FlammableSystem _flameSys = null!;
    [Dependency] private readonly DamageableSystem _damageableSystem = null!;
    [Dependency] private readonly AnchorableSystem _anchorableSystem = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = null!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = null!;

    private ISawmill _sawmill = null!;
    private const string BurntTag = "burnt";
    private const int MaxBurntDecals = 3;
    private readonly List<string> _decals = [];

    public override void Initialize()
    {
        SetupDecals();
        SubscribeLocalEvent<FlameThrowerComponent, InteractUsingEvent>(OnUsingInteract);
        SubscribeLocalEvent<FlameThrowerComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<FlameThrowerComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<FlameThrowerComponent, GunShotEvent>(AddIgnitionTime);

        SubscribeLocalEvent<FlameParticleComponent, StartCollideEvent>(OnFlameCollide);
        SubscribeLocalEvent<FlameParticleComponent, LandEvent>((uid, comp, _) => OnFlameLand(uid, comp));

        _sawmill = LogManager.GetSawmill("FlameParticleSystem");
    }

    private void SetupDecals()
    {
        var enumerator = _prototypeManager.EnumeratePrototypes<DecalPrototype>().GetEnumerator();
        while (enumerator.MoveNext())
        {
            var decal = enumerator.Current;
            if (!decal.Tags.Contains(BurntTag))
                continue;

            _decals.Add(decal.ID);
        }
        enumerator.Dispose();
    }

    private static void AddIgnitionTime(EntityUid uid, FlameThrowerComponent component, GunShotEvent args)
    {
        if (!component.Ignited || !component.IgnitionTime.HasValue)
            return;

        component.IgnitionTime = MathHelper.Min(component.IgnitionTime.Value + component.IgnitionTimeOnShot, component.MaxIgnitionTime);
    }

    /// <summary>
    /// Метод обрабатывающий событие использования чего - то на огнемёте.
    /// Мы ожидаем, что это будет что - то что сможет его поджечь, иначе игнорируем.
    /// </summary>
    private void OnUsingInteract(EntityUid uid, FlameThrowerComponent component, InteractUsingEvent args)
    {
        if (component.Ignited || !TryComp<IgnitionSourceComponent>(args.Used, out var ignitionSource) || !ignitionSource.Ignited)
            return;

        TryIgniteFlameThrower(uid, component);
    }

    /// <summary>
    /// Обрабатываем событие когда мы используем огнемёт, чтобы сделать что - то.
    /// Мы ожидаем, что огнемёт будет взаимодействовать с источником огня, чтобы поджечься, инае игнорируем.
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
    /// Метод который, устанавливает максимальную дальность для зарядов пламени
    /// </summary>
    /// <param name="uid">Оружие из которого ведётся стрельба</param>
    /// <param name="component">Компонент который, устанавливает верхний порог дальности</param>
    /// <param name="args">Событие стрельбы</param>
    private void OnAttemptShoot(EntityUid uid, FlameThrowerComponent component, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !TryComp<GunComponent>(uid, out var gun))
            return;

        if (!component.Ignited) // Мы не можем стрелять если у нас огня нет лол
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("flamethrower-not-ignited");
            return;
        }

        var newProjectileSpeed = component.MaxFireSpeed;
        var newCoords = gun.ShootCoordinates!.Value;
        var from = Transform(uid).Coordinates.Position;
        if (gun.ShootCoordinates is var (_, pos))
        {
            var fireDistance = (from - pos).Length();

            var projectileSpeed =
            GetActualProjectileSpeed(fireDistance, component.MaxFireSpeed, component.MinFireSpeed);
            newProjectileSpeed = projectileSpeed * component.SpeedMod; // Going to go FAST!

            var coords = GetActualFireCoords(uid, from, fireDistance, pos.Normalized(), component.MaxFireDistance, component.MinFireDistance);
            if (coords != EntityCoordinates.Invalid)
                newCoords = coords;
        }

        _gunSys.SetProjectileSpeedModified((uid, gun), newProjectileSpeed); // Чем курсор дальше от места стрельбы - тем пламя быстрее
        _gunSys.SetShootCoords((uid, gun), newCoords); // Меняем точку стрельбы если достигли максимальной дальности заряда пламени
    }

    /// <summary>
    /// Получает актуальные координаты полёта пули (Положение курсора на экране относительно мира)
    /// Если координаты оказываются выше максимального порога дальности стрельбы обрезает дальность до максимума
    /// </summary>
    /// <param name="uid">Оружие из которого ведётся огонь</param>
    /// <param name="from">Место откуда ведётся огонь</param>
    /// <param name="fireDistance">Разница векторов между <c>from</c> и точкой стрельбы и прогнаная через метод <see cref="Vector2.Length()"/></param>
    /// <param name="normalized">Направление стрельбы</param>
    /// <param name="maxDistance">Максимальное расстояние полёта пламени</param>
    /// <param name="minDistance">Минимальное расстояние полёта пламени. (Мы не хотим стрелять себе под ноги)</param>
    /// <returns></returns>
    private static EntityCoordinates GetActualFireCoords(EntityUid uid, Vector2 from, float fireDistance,
        Vector2 normalized, float maxDistance, float minDistance)
    {
        var maxPos = normalized * maxDistance;
        var minPos = normalized * minDistance;
        if (fireDistance > maxDistance) // Дальность полёта пламени
            return new EntityCoordinates(uid, maxPos + from);

        if (fireDistance < minDistance)
            return new EntityCoordinates(uid, minPos + from);

        return EntityCoordinates.Invalid;
    }

    /// <summary>
    /// Вычисляет текущую скорость заряда пламени по расстоянию курсора от точки начала стрельбы
    /// </summary>
    /// <param name="fireDistance">Разница векторов между "from" и точкой стрельбы и прогнанная через метод <see cref="Vector2.Length()"/></param>
    /// <param name="maxFireSpeed">Максимальная скорость полёта заряда пламени</param>
    /// <param name="minFireSpeed">Минимальная скорость полёта заряда пламени</param>
    /// <returns>Возвращает <c>maxFireSpeed</c> если расстояние больше <c>maxFireSpeed</c>,
    /// если нет то берёт значение в диапазоне от <c>minFireSpeed до maxFireSpeed</c></returns>
    private float GetActualProjectileSpeed(float fireDistance, float maxFireSpeed, float minFireSpeed)
    {
        return fireDistance < maxFireSpeed // Скорость пламени
            ? float.Clamp(fireDistance, minFireSpeed, maxFireSpeed)
            : maxFireSpeed;
    }

    /// <summary>
    /// Обработка события столкновения заряда пламени с объектом
    /// При коллизии с другим объектом если он, имеет <see cref="DamageableComponent"/> наносит урон
    /// который указан в <see cref="FlameParticleComponent.Damage"/>.
    /// </summary>
    private void OnFlameCollide(EntityUid uid, FlameParticleComponent component, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != "fire" || !args.OtherFixture.Hard)
            return;

        var target = args.OtherEntity;
        var ev = new ProjectileHitEvent(component.Damage, target);
        RaiseLocalEvent(uid, ref ev);

        if (TryComp<FlammableComponent>(target, out var flame))
            _flameSys.AdjustFireStacks(target, component.Stacks, flame, true);

        if (!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        _damageableSystem.ChangeDamage((target, damageableComponent), ev.Damage);
        OnFlameLand(uid, component);
    }

    /// <summary>
    /// Метод который с 20% шанцем создаёт лужу огня под местом приземления
    /// заряда огнемёта.
    /// </summary>
    private void OnFlameLand(EntityUid uid, FlameParticleComponent component)
    {
        var coordinates = Transform(uid).Coordinates;
        TryAddDecalOnCoordinates(coordinates);
        if (!_random.Prob(component.FireDropProb) || !TryComp<PhysicsComponent>(uid, out var physics)) // Шанц оставить после попадания лужу огня
        {
            QueueDel(uid);
            return;
        }

        if (_anchorableSystem.TileFree(coordinates, physics))
        {
            var flame = SpawnAtPosition(component.FireProto, coordinates);
            _flameSys.SetFireStacks(flame, _random.Next(2, 10), ignite: true);
        }

        QueueDel(uid);
    }

    /// <summary>
    /// Обрабатывает событие появления лужи пламени
    /// И удаляет его когда пламя потушено.
    /// </summary>
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        var deltaTimeInSec = TimeSpan.FromSeconds(deltaTime);
        var query = EntityManager.EntityQueryEnumerator<FlameFloorFireComponent, FlammableComponent>();
        while (query.MoveNext(out var uid, out var floor, out var flammable))
        {
            floor.NextDecalSpawn ??= _gameTiming.CurTime + floor.DecalSpawnPeriod;
            if (floor.NextDecalSpawn <= _gameTiming.CurTime && floor.SpawnedDecal == false)
                floor.SpawnedDecal = TryAddDecalOnCoordinates(Transform(uid).Coordinates);

            if (flammable.FireStacks <= 0)
                QueueDel(uid);
        }
        query.Dispose();

        var query0 = EntityManager.EntityQueryEnumerator<FlameThrowerComponent>();
        while (query0.MoveNext(out var uid, out var flame))
        {
            if (!flame.Ignited)
                continue;

            if (flame.IgnitionTime > TimeSpan.Zero)
            {
                flame.IgnitionTime -= deltaTimeInSec;
                continue;
            }

            flame.Ignited = false;
            flame.IgnitionTime = TimeSpan.Zero;
            _appearanceSystem.SetData(uid, FlameThrower.Base, false);
            DirtyField<FlameThrowerComponent>((uid, flame), nameof(FlameThrowerComponent.Ignited));
        }
        query0.Dispose();
    }

    private bool TryAddDecalOnCoordinates(EntityCoordinates coords)
    {
        if (this._decals.Count <= 0)
        {
            _sawmill.Error($"Not found decals with tag: [\"burnt\"] in {this._decals} array!");
            _sawmill.Error("Empty array = cant add decals on floor, try add decals with tag: [\"burnt\"] next time.");
            return false;
        }

        var objCenter = coords.Position - new Vector2(0.5f);
        var coordinates = new EntityCoordinates(coords.EntityId, objCenter);
        if (_transformSystem.GetGrid(coordinates) is not {} grid)
            return false;

        var burntDecals = 0;
        var decals = _decalSys.GetDecalsInRange(grid, coordinates.Position);
        foreach (var (_, decal) in decals)
        {
            if (this._decals.Contains(decal.Id))
                burntDecals++;
        }

        if (burntDecals >= MaxBurntDecals)
            return false;

        var randomId = this._decals[_random.Next(0, this._decals.Count - 1)];
        return _decalSys.TryAddDecal(randomId, coordinates, out _, cleanable: true);
    }
}
