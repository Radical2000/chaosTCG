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
        if (isPartnerSlot)
        {
            card.isPartner = true;
        }
        
        card.transform.SetParent(cardAnchor);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one;

        Debug.Log($" Slot {slotIndex} に {card.cardData.cardName} を配置しました");
    }
    public void OnClickSlot()
    {
        // ① EXレベルアップ処理
        if (EXManager.Instance != null && EXManager.Instance.IsWaitingForLevelUpTarget())
        {
            CardView selectedMaterial = EXManager.Instance.GetSelectedLevelUpMaterial();
            if (currentCard != null && selectedMaterial != null)
            {
                EXManager.Instance.TryLevelUp(this, selectedMaterial);
                return;
            }
        }

        // ② EX召喚処理
        if (EXManager.Instance != null && EXManager.Instance.HasSelectedEXCard())
        {
            EXManager.Instance.OnClickSlotForEX(this);
            return;
        }

        // ③ 通常召喚処理（ReturnOneToHand 対応込み）
        CardView card = FieldManager.Instance.selectedCardToSummon;
        if (card == null)
        {
            Debug.Log("召喚対象がないため処理を中断します");
            return;
        }

        // パートナーを破棄しない
        if (currentCard != null && currentCard.IsPartner)
        {
            Debug.Log("このスロットはパートナーのため破棄できません");
            return;
        }

        // 既存カードがいる場合の処理（破棄 or 特殊処理）
        if (currentCard != null)
        {
            if (card.isBeingCostProcessed)
            {
                Debug.Log("この処理はReturnOneToHand中のため、スロットのカードは破棄しません");
                Destroy(currentCard.gameObject); // あくまで見た目だけ除去

                FieldManager.Instance.selectedCardToSummon = null; 
                return; 
            }
            else
            {
                if (currentCard.IsEXUnit())
                {
                    DiscardManager.Instance.BanishCardData(currentCard.cardData);
                }
                else
                {
                    DiscardManager.Instance.AddToDiscard(currentCard.cardData);
                }
                Destroy(currentCard.gameObject);
            }
        }

        // 新しいカードをセット
        card.transform.SetParent(cardAnchor);
        card.transform.localPosition = Vector3.zero;
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
