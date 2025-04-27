using UnityEngine;

public enum EventTiming
{
    MainOnly,
    BattleOnly,
    Both // メインは自分のみ、バトル中は両者OK
}

public enum EventEffectType
{
    StandFaceUp,
    PowerUp,
    SupportUp,
    DamageToOne,
    DamageToAll,
    GrantPenetrate,
    RestIfNew,
    FlipSelf,
    FlipOneEach,
    DamageSingle,
    GlobalDamage,
}

public enum TargetType
{
    None,
    SelfUnit,
    OpponentUnit,
    AllUnits,
    ChooseOne
}

[CreateAssetMenu(fileName = "NewEvent", menuName = "TCG/Event Card")]
public class CardEventData : ScriptableObject
{
    public string eventName;
    [TextArea] public string description;

    public EventTiming eventTiming;
    public EventEffectType effectType;
    public int amount;

    public TargetType targetType;

    // その他必要なら効果用フラグ追加
}
