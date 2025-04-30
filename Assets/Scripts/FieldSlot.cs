using UnityEngine;
using UnityEngine.UI;

public class FieldSlot : MonoBehaviour
{
    public int slotIndex; // 0〜4
    public bool isPartnerSlot; // trueならパートナー

    public Transform cardAnchor; // カードをここに置く（UI整列用）

    public CardView currentCard; // 今このスロットにいるカード（null = 空）

    public bool IsEmpty => currentCard == null;
    [Header("ハイライトUI")]
    public Image highlightFrame; // 枠画像（Inspectorで設定）
    private bool isHighlighted = false;

    /// <summary>
    /// このスロットにカードを置く
    /// </summary>
    public void PlaceCard(CardView card)
    {
        if (!IsEmpty)
        {
            if (isPartnerSlot)
            {
                Debug.LogWarning(" パートナー枠のユニットは破棄できません！");
                return;
            }

            // 既存のユニットを破棄（控え室へ）
            DiscardManager.Instance.MoveCardViewToDiscard(currentCard);
        }

        currentCard = card;
        card.transform.SetParent(cardAnchor);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one;

        Debug.Log($" Slot {slotIndex} に {card.cardData.cardName} を配置しました");
    }
    public void OnClickSlot()
    {
        if (EXManager.Instance != null && EXManager.Instance.IsWaitingForLevelUpTarget())
        {
            CardView selectedMaterial = EXManager.Instance.GetSelectedLevelUpMaterial();
            if (currentCard != null && selectedMaterial != null)
            {
                EXManager.Instance.TryLevelUp(this, selectedMaterial);
                return;
            }
        }
        if (EXManager.Instance != null && EXManager.Instance.HasSelectedEXCard())
        {
            EXManager.Instance.OnClickSlotForEX(this);
            return;
        }
        CardView card = FieldManager.Instance.selectedCardToSummon;
        if (card == null) return;

        if (isPartnerSlot)
        {
            Debug.Log(" パートナースロットには配置できません");
            return;
        }

        // すでにカードがいたら墓地へ送る
        if (currentCard != null)
        {
            Debug.Log($" スロット{slotIndex}にいた {currentCard.cardData.cardName} を破棄します");

            // 装備やエフェクトもあればここで一括処理する
            DiscardManager.Instance.AddToDiscard(currentCard.cardData);
            Destroy(currentCard.gameObject);
        }

        // 手札から移動（召喚）
        card.transform.SetParent(cardAnchor);
        card.transform.localPosition = Vector3.zero;

        // 登場フラグ
        card.isNewlySummoned = true;

        currentCard = card;
        FieldManager.Instance.selectedCardToSummon = null;

        Debug.Log($"スロット {slotIndex} に {card.cardData.cardName} を配置しました");


    }
    //slotのハイライト
    public void SetHighlight(bool enable)
    {
        isHighlighted= enable;
        if (highlightFrame != null)
        {
            highlightFrame.enabled = enable;
        }
    }

    public bool IsHighlighted()
    {
        return isHighlighted;
    }
    
}
