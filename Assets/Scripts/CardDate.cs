using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public enum CardType { Character, Event, Set }

[CreateAssetMenu(fileName = "NewCard", menuName = "TCG/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public Sprite cardImage;
    [TextArea] public string description;

    // �L�����p
    public CostType costType;
    public int costAmount; // ��D���̂Ă閇���ȂǂɎg���i�s�v�Ȃ�0�j
    public int power;
    public int support;
    public bool hasPenetrate; // �ђʎ������ǂ���
    public CardData exTarget; // EX��
    public Sprite illustration;

    //  �R�X�g���
    public List<CostType> summonCostTypes = new List<CostType>();
    public List<int> summonCostAmounts = new List<int>();
    public string requiredUnitName;

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

    //CIP���ʗp
    public bool hasCIPDiscardEffect;
    //EX�p
    public bool isEX;
    public EXType exType = EXType.None;// ���̃J�[�h��EX�J�[�h���ǂ���
    public string exBaseA;           // EX�������ƂȂ�x�[�X�J�[�hA�̖��O�i��F"A"�j
    public string exBaseB;           // EX�������ƂȂ�f�ރJ�[�hB�̖��O�i��F"B"�j

    //���ꏢ���̗L��
    public bool isSpecialSummon = false;

    public List<CostRequirement> summonCosts = new List<CostRequirement>();

    public enum EXType
    {
        None,
        AB�^,
        C�^
    }


    //  ���s�p�I�u�W�F�N�g����
    public List<CostRequirement> GetSummonCostRequirements()
    {
        var list = new List<CostRequirement>();
        int count = Mathf.Min(summonCostTypes.Count, summonCostAmounts.Count); // �������Z�����ɍ��킹��
        for (int i = 0; i < count; i++)
        {
            list.Add(CostManager.CostFactory.Create(summonCostTypes[i], summonCostAmounts[i]));
        }
        return list;
    }

}
