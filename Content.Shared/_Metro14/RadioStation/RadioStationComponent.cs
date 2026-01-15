using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Metro14.RadioStation;

[RegisterComponent]
public sealed partial class RadioStationComponent : Component
{
    /// <summary>
    /// Маппинг доступных на карте фракций (частот) и ролей, которые им принадлежат.
    /// </summary>
    [DataField]
    public Dictionary<string, List<string>> RolesFrequenciesMapping = new()
    {
        ["hydra_frequency"] = new() { "M14HeadOfHydra", "M14Minister", "M14SoldierHydra" },
        ["redline_frequency"] = new() { "M14HeadOfRedLine", "M14PolitHandRedLine", "M14SoldierRedline" },
        ["hansa_frequency"] = new() { "M14HeadOfHansa", "M14TradeAgentHansa", "M14OfficerHansa", "M14CivilianHansa" },
        ["sparta_frequency"] = new() { "M14HeadOfSparta", "M14LieutenantOfHeadOfSparta", "M14MedicSparta", "M14SoldierSparta" },
        ["tech_frequency"] = new() { "M14HeadOfTech", "M14SecurityTech", "M14TechnicTech" },
        ["vdnh_frequency"] = new() { "M14HeadOfVDNH", "M14SecurityVDNH", "M14StalkerVDNH" }
    };

    /// <summary>
    /// Маппинг фракций (частот) и выражений для отображения текущей частоты вышки при осмотре сущности.
    /// Поле необходимо для того, чтобы в будущем прототиперы могли спокойно добавить новую фракцию,
    /// например, бандитов на кастомной карте и при осмотре вышки это выглядело адекватно.
    /// </summary>
    [DataField]
    public Dictionary<string, string> FrequenciesLocalizationMapping = new()
    {
        { "neutral_frequency", "neutral-frequency-named" },
        { "hydra_frequency", "hydra-frequency-named" },
        { "redline_frequency", "redline-frequency-named" },
        { "hansa_frequency", "hansa-frequency-named" },
        { "sparta_frequency", "sparta-frequency-named" },
        { "tech_frequency", "tech-frequency-named" },
        { "vdnh_frequency", "vdnh-frequency-named" }
    };

    /// <summary>
    /// Текущая частота.
    /// </summary>
    [DataField]
    public string CurrentFrequence = "neutral_frequency";

    /// <summary>
    /// Время захвата вышки. В секундах.
    /// </summary>
    [DataField]
    public int DoAfterActionTime = 20;

    /// <summary>
    /// Время для возможности восстановить свою фракцию после потери последней радиостанции. В секундах.
    /// </summary>
    [DataField]
    public int TimeToRecaptureStation = 900;
}

[Serializable, NetSerializable]
public sealed partial class RadioStationChangeFrequencieDoAfterEvent : SimpleDoAfterEvent { }
