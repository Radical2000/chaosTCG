using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardPlayManager : MonoBehaviour
{
    public static CardPlayManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Transform fieldZone;
    public HandManager handManager;
    public GameObject cardPrefab;
    public CardView deferredCardView;
    public CardView selectedCardToSummon;

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

        var costList = card.GetSummonCostRequirements();

        // ✅ すべてのコストが支払可能かチェック（選択式も含めて！）
        foreach (var cost in costList)
        {
            if (!cost.isPayable())
            {
                Debug.LogWarning($"コスト {cost.type} を支払えないため、召喚を中止します");
                return;
            }
        }

        // ✅ 選択を必要とするコストがある場合 → defer
        var deferredCost = costList.FirstOrDefault(c =>
            c.type == CostType.DiscardX ||
            c.type == CostType.DiscardXUnit ||
            c.type == CostType.ReturnOneToHand);

        if (deferredCost != null)
        {
            deferredCardView = cardView;
            deferredCost.doPay(); // UI起動 → 完了後 OnCostPaymentComplete 呼ばれる
            return;
        }

        // ✅ すべて即時支払い可能 → 即支払い
        foreach (var cost in costList)
        {
            cost.doPay();
        }

        ActionLimiter.Instance.UseSummon();
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
            handManager.AddToHand(cardView.GetCardData());
            Destroy(target.gameObject);
        }
    }

    public void SpawnCardToField(CardData data)
    {
        GameObject cardGO = Instantiate(cardPrefab, fieldZone);
        CardView view = cardGO.GetComponent<CardView>();
        view.SetCard(data, true);
        view.isNewlySummoned = true;
    }

    public bool IsSameNameCardOnField(CardData card)
    {
        foreach (Transform child in fieldZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null)
            {
                if (view.cardData.cardName.Contains(card.cardName) || card.cardName.Contains(view.cardData.cardName))
                {
                    Debug.LogWarning($" 同名カードがすでに場に存在します：{view.cardData.cardName}");
                    return true;
                }
            }
        }
        return false;
    }

    public void ResolveSummonAfterCost()
    {
        if (selectedCardToSummon == null)
        {
            Debug.LogWarning(" 召喚対象のカードが未設定です");
            return;
        }

        Debug.Log(" コスト支払い完了 → スロットをクリックしてください");
    }

    public void OnCostPaymentComplete()
    {
        Debug.Log(" コスト支払い完了 → 召喚再開");

        if (deferredCardView != null)
        {
            ActionLimiter.Instance.UseSummon();
            FieldManager.Instance.selectedCardToSummon = deferredCardView;
            Debug.Log("PlayCard 完了（コストあり）→ スロットをクリックしてください！");
            deferredCardView = null;
        }
        else
        {
            Debug.LogWarning(" OnCostPaymentComplete: deferredCardView が null");
        }
    }
}
