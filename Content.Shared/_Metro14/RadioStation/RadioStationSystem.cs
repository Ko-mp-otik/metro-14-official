using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Metro14.RadioStation;

/// <summary>
/// Система управления радиостанциями. Отслеживает взаимодействие с точками
/// связи и поднимает событие переключения частоты.
/// </summary>
public sealed class RadioStationSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RadioStationComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    /// <summary>
    /// При взаимодействии с вышкой через меню действия предлагаем возможность сменить частоту.
    /// </summary>
    private void OnGetInteractionVerbs(EntityUid uid, RadioStationComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User,
                    TimeSpan.FromSeconds(component.DoAfterActionTime),
                    new RadioStationChangeFrequencieDoAfterEvent(),
                    args.Target)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                };

                _doAfterSystem.TryStartDoAfter(doAfterArgs);
            },
            Text = Loc.GetString("radistation-switch-frequency-successfully"),
            Message = Loc.GetString("radistation-switch-frequency-failed"),
            Disabled = false,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }
}
