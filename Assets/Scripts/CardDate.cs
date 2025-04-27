using UnityEngine;

public enum CardType { Character, Event, Set }

[CreateAssetMenu(fileName = "NewCard", menuName = "TCG/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public Sprite cardImage;
    [TextArea] public string description;

    // キャラ用
    public CardCostType costType;
    public int costAmount; // 手札を捨てる枚数などに使う（不要なら0）
    public int power;
    public int support;
    public bool isPartner;
    public bool hasPenetrate; // 貫通持ちかどうか
    public CardData exTarget; // EX先

    // イベント・セット用（必要に応じて）
    public string effectScriptName; // 実行スクリプトの名前など
    public bool isUnit; // true: ユニット, false: イベント
    public CardEventData linkedEvent; // イベントなら効果をここに

    //EX用
    public bool isEX;
    public EXType exType = EXType.None;// このカードがEXカードかどうか
    public string exBaseA;           // EX化条件となるベースカードAの名前（例："A"）
    public string exBaseB;           // EX化条件となる素材カードBの名前（例："B"）

    public enum EXType
    {
        None,
        AB型,
        C型
    }
}
