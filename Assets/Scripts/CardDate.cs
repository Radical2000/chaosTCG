using UnityEngine;

public enum CardType { Character, Event, Set }

[CreateAssetMenu(fileName = "NewCard", menuName = "TCG/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public Sprite cardImage;
    [TextArea] public string description;

    // �L�����p
    public CardCostType costType;
    public int costAmount; // ��D���̂Ă閇���ȂǂɎg���i�s�v�Ȃ�0�j
    public int power;
    public int support;
    public bool isPartner;
    public bool hasPenetrate; // �ђʎ������ǂ���
    public CardData exTarget; // EX��

    // ���x���A�b�v�Ǘ��p
    public int currentLevel = 1; // �ŏ���Lv1�X�^�[�g
    public int exLevelUpAtkBoost = 0;
    public int exLevelUpHpBoost = 0;
    // ���x���A�b�v�ł��邩�ǂ���
    public bool canLevelUp = false;

    // �C�x���g�E�Z�b�g�p�i�K�v�ɉ����āj
    public string effectScriptName; // ���s�X�N���v�g�̖��O�Ȃ�
    public bool isUnit; // true: ���j�b�g, false: �C�x���g
    public CardEventData linkedEvent; // �C�x���g�Ȃ���ʂ�������

    //EX�p
    public bool isEX;
    public EXType exType = EXType.None;// ���̃J�[�h��EX�J�[�h���ǂ���
    public string exBaseA;           // EX�������ƂȂ�x�[�X�J�[�hA�̖��O�i��F"A"�j
    public string exBaseB;           // EX�������ƂȂ�f�ރJ�[�hB�̖��O�i��F"B"�j



    public enum EXType
    {
        None,
        AB�^,
        C�^
    }
}
