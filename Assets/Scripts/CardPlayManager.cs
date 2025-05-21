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

        if (!ActionLimiter.Instance.CanSummon()&&!card.isSpecialSummon)
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
        //  すべてのコストが支払可能かチェック
        foreach (var cost in costList)
        {
            bool payable = cost.isPayable(cardView);
            Debug.Log($"コスト {cost.type} の支払い可能性: {payable}");

            if (!payable)
            {
                Debug.LogWarning($"コスト {cost.type} を支払えないため、召喚を中止します");
                return;
            }
        }
       
        //  選択を必要とするコストがある場合 → defer
        var deferredCost = costList.FirstOrDefault(c =>
            c.type == CostType.DiscardX ||
            c.type == CostType.DiscardXUnit ||
            c.type == CostType.ReturnOneToHand ||
            c.type == CostType.RestOneUnit ||
            c.type == CostType.FlipUnitFaceUp||
            c.type ==CostType.FlipUnitFaceDown||
            c.type==CostType.FlipUnitFaceDownRest||
            c.type==CostType.BanishFromDiscard||
            c.type==CostType.BanishFromDiscardX||
            c.type==CostType.Return1_Discard1_SS||
            c.type==CostType.使用しないDraw2_Discard1);

        if (deferredCost != null)
        {
            deferredCardView = cardView;
            deferredCost.doPay(); // UI起動 → 完了後 OnCostPaymentComplete 呼ばれる
            return;
        }

        //  すべて即時支払い可能 → 即支払い
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

        // スロットの空きを探して配置（ここは元々あなたの実装に合わせて調整）
        foreach (Transform child in fieldZone)
        {
            FieldSlot slot = child.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard == null)
            {
                slot.PlaceCard(view);

                //  配置後にCIP効果発動
                CIPEffectManager.Instance.TryTriggerCIPEffect(view);
                break;
            }
        }
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
