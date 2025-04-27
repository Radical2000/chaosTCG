using UnityEngine;

public enum EventTiming
{
    MainOnly,
    BattleOnly,
    Both // ���C���͎����̂݁A�o�g�����͗���OK
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

    // ���̑��K�v�Ȃ���ʗp�t���O�ǉ�
}
