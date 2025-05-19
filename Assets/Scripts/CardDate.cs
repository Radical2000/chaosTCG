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

    // キャラ用
    public CostType costType;
    public int costAmount; // 手札を捨てる枚数などに使う（不要なら0）
    public int power;
    public int support;
    public bool hasPenetrate; // 貫通持ちかどうか
    public CardData exTarget; // EX先
    public Sprite illustration;

    //  コスト情報
    public List<CostType> summonCostTypes = new List<CostType>();
    public List<int> summonCostAmounts = new List<int>();
    public string requiredUnitName;

    // レベルアップ管理用
    public int currentLevel = 1; // 最初はLv1スタート
    public int exLevelUpAtkBoost = 0;
    public int exLevelUpHpBoost = 0;
    // レベルアップできるかどうか
    public bool canLevelUp = false;

    // イベント・セット用（必要に応じて）
    public string effectScriptName; // 実行スクリプトの名前など
    public bool isUnit; // true: ユニット, false: イベント
    public CardEventData linkedEvent; // イベントなら効果をここに

    //CIP効果用
    public bool hasCIPDiscardEffect;
    //EX用
    public bool isEX;
    public EXType exType = EXType.None;// このカードがEXカードかどうか
    public string exBaseA;           // EX化条件となるベースカードAの名前（例："A"）
    public string exBaseB;           // EX化条件となる素材カードBの名前（例："B"）

    //特殊召喚の有無
    public bool isSpecialSummon = false;

    public List<CostRequirement> summonCosts = new List<CostRequirement>();

    public enum EXType
    {
        None,
        AB型,
        C型
    }


    //  実行用オブジェクト生成
    public List<CostRequirement> GetSummonCostRequirements()
    {
        var list = new List<CostRequirement>();
        int count = Mathf.Min(summonCostTypes.Count, summonCostAmounts.Count); // 長さが短い方に合わせる
        for (int i = 0; i < count; i++)
        {
            list.Add(CostManager.CostFactory.Create(summonCostTypes[i], summonCostAmounts[i]));
        }
        return list;
    }

}
