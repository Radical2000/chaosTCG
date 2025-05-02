using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardPlayManager : MonoBehaviour
{
    public Transform fieldZone;
    public HandManager handManager;
    public GameObject cardPrefab;

    public void PlayCard(CardView cardView)
    {
        CardData card = cardView.GetCardData();

        if (cardView.transform.parent.name != "PlayerHand")
        {
            Debug.LogWarning("手札以外のカードは場に出せません！");
            return;
        }

        if (!ActionLimiter.Instance.CanSummon())
        {
            Debug.LogWarning("このターンの召喚はすでに1回行われました！");
            return;
        }

        if (IsSameNameCardOnField(card))
        {
            Debug.Log("同名カードが場にあるため召喚できません");
            return;
        }

        // ✅ コスト判定・支払い（新方式）
        var costList = card.GetSummonCostRequirements();
        foreach (var cost in costList)
        {
            if (!cost.isPayable())
            {
                Debug.LogWarning($"コスト {cost.type} を支払えません！");
                return;
            }
        }
        foreach (var cost in costList)
        {
            cost.doPay();
        }

        ActionLimiter.Instance.UseSummon(); // 制限カウント

        // ★ スロットに配置するのは OnClickSlot 側でやる！
        FieldManager.Instance.selectedCardToSummon = cardView;
        Debug.Log("PlayCard 完了 → スロットをクリックしてください！");
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
