using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardPlayManager : MonoBehaviour
{
    public Transform fieldZone;
    public HandManager handManager;
    public GameObject cardPrefab;
    


    public bool CanPlayCard(CardData card)
    {
        switch (card.costType)
        {
            case CardCostType.裏キャラを表に:
                return fieldZone.GetComponentsInChildren<CardView>().Any(c => !c.isFaceUp);
            case CardCostType.手札に戻して登場:
            case CardCostType.キャラを捨てて登場:
                return fieldZone.childCount > 0;
            case CardCostType.手札を捨てて控えに送る:
            case CardCostType.手札を枚捨てて発動:
                return handManager.HandCount >= card.costAmount;
            case CardCostType.無し:
            default:
                return true;
        }
    }

    public void PlayCard(CardView cardView)
    {
        CardData card = cardView.GetCardData();
        if (!CanPlayCard(cardView.GetCardData()))
        {
            Debug.Log("コスト条件が満たされていません！");
            return;
        }
        if (cardView.transform.parent.name != "PlayerHand")
        {
            Debug.LogWarning(" 手札以外のカードは場に出せません！");
            return;
        }
        if (!ActionLimiter.Instance.CanSummon())
        {
            Debug.LogWarning("このターンの召喚はすでに1回行われました！");
            return;
        }
        if (IsSameNameCardOnField(card))
        {
            Debug.Log(" 同名カードが場にあるため召喚できません");
            return;
        }

        ActionLimiter.Instance.UseSummon(); // ? 制限カウント

        PayCost(card);

        // ★ スロットに配置するのは OnClickSlot 側でやる！
        FieldManager.Instance.selectedCardToSummon = cardView;
        Debug.Log("? PlayCard 処理完了 → スロットをクリックしてください！");
    }
    

    private void PayCost(CardData card)
    {
        switch (card.costType)
        {
            case CardCostType.手札を捨てて控えに送る:
            case CardCostType.手札を枚捨てて発動:
                handManager.DiscardFromHand(card.costAmount);
                break;
            case CardCostType.裏キャラを表に:
                FlipOneFacedownCard();
                break;
            case CardCostType.手札に戻して登場:
                ReturnCharacterToHand(); // 仮処理
                break;
            case CardCostType.キャラを捨てて登場:
                SacrificeRandomFieldCharacter(); // 仮処理
                break;
        }
    }

    private void FlipOneFacedownCard()
    {
        var facedown = fieldZone.GetComponentsInChildren<CardView>().FirstOrDefault(c => !c.isFaceUp);
        if (facedown != null) facedown.SetFaceUp(true);
    }

    private void SacrificeRandomFieldCharacter()
    {
        if (fieldZone.childCount > 0)
        {
            Transform target = fieldZone.GetChild(0);
            Destroy(target.gameObject);
        }
    }
    private void ReturnCharacterToHand()
    {
        if (fieldZone.childCount == 0) return;

        Transform target = fieldZone.GetChild(0); // 仮で最初のカード
        CardView cardView = target.GetComponent<CardView>();

        if (cardView != null)
        {
            handManager.AddToHand(cardView.GetCardData()); // 手札に戻す
            Destroy(target.gameObject); // フィールドから削除
        }
    }

    public void SpawnCardToField(CardData data)
    {
        GameObject cardGO = Instantiate(cardPrefab, fieldZone);
        CardView view = cardGO.GetComponent<CardView>();
        view.SetCard(data, true); // 表向きで表示
        view.isNewlySummoned = true;//ここでこのターン召喚したかのboolを変更
    }

    //EX処理（未実装）
    public void OnClickEX()
    {
        if (!ActionLimiter.Instance.CanEX())
        {
            Debug.Log("EX化はこのターンすでに使用済みです！");
            return;
        }

        // EX化処理をここに書く

        ActionLimiter.Instance.UseEX();
    }
    //レベルアップ処理（未実装）
    public void OnClickLevelUp()
    {
        if (!ActionLimiter.Instance.CanLevelUp())
        {
            Debug.Log("レベルアップはこのターンすでに使用済みです！");
            return;
        }

        // レベルアップ処理をここに書く

        ActionLimiter.Instance.UseLevelUp();
    }
    //同名ユニットに関する処理
    public bool IsSameNameCardOnField(CardData card)
    {
        foreach (Transform child in fieldZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null)
            {
                // 部分一致（A や B を含む名前かどうか）
                if (view.cardData.cardName.Contains(card.cardName) ||
                    card.cardName.Contains(view.cardData.cardName))
                {
                    Debug.LogWarning($" 同名カードがすでに場に存在します：{view.cardData.cardName}");
                    return true;
                }
            }
        }
        return false;
    }

}
