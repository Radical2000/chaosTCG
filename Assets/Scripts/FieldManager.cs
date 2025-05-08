using System.Collections;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public static FieldManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Transform playerFieldZone;
    public Transform enemyFieldZone;
    public CardView selectedCardToSummon;
    public FieldSlot selectedSlot;

    // エンドフェイズ：表＆スタンド＆エンド効果を処理
    public void ResolveEndPhase()
    {
        Debug.Log("🌀 エンドフェイズ処理開始：表戻し・スタンド処理");

        bool flipped = false;
        bool stood = false;

        foreach (Transform unit in playerFieldZone)
        {
            var view = unit.GetComponent<CardView>();

            if (!flipped && !view.isFaceUp)
            {
                view.SetFaceUp(true);
                Debug.Log($" 表に戻しました：{view.cardData.cardName}");
                flipped = true;
            }

            if (!stood && view.isRested)
            {
                view.SetRest(false);
                Debug.Log($" スタンドさせました：{view.cardData.cardName}");
                stood = true;
            }

            if (flipped && stood) break;
        }
    }


    private void FlipAndStandOne(Transform fieldZone)
    {
        foreach (Transform child in fieldZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null)
            {
                // 表にする（任意の条件で）
                if (!view.isFaceUp)
                {
                    view.SetFaceUp(true);
                    Debug.Log($"{view.cardData.cardName} を表に戻した！");
                    break;
                }
            }
        }

        foreach (Transform child in fieldZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null)
            {
                // スタンドさせる（任意の条件で）
                if (view.isRested)
                {
                    view.SetRest(false);
                    Debug.Log($"⬆️ {view.cardData.cardName} をスタンドさせた！");
                    break;
                }
            }
        }
    }

    // 全ユニットに EndTurnReset を適用
    public void EndTurnResetAll()
    {
        ResetZone(playerFieldZone);
        ResetZone(enemyFieldZone);
    }

    private void ResetZone(Transform zone)
    {
        foreach (Transform child in zone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null)
            {
                view.EndTurnReset(); // ダメージなどリセット
            }
        }
    }
    public void SelectCardForSummon(CardView cardView)
    {
        selectedCardToSummon = cardView;
        //HighlightAvailableSlots(); // 出せるスロットを光らせる処理
    }
    public void SelectSlot(FieldSlot slot)
    {
        selectedSlot = slot;
        Debug.Log($" スロットを選択：{slot.name}");
    }

    //デバッグ用

    private IEnumerator Start()
    {
        // 敵フィールド側のスロットの初期化
        foreach (Transform child in enemyFieldZone)
        {
            FieldSlot slot = child.GetComponent<FieldSlot>();
            if (slot != null)
            {
                CardView card = slot.GetComponentInChildren<CardView>();
                if (card != null)
                {
                    slot.currentCard = card;
                    Debug.Log($"Slot {slot.name} に {card.cardData.cardName} を初期登録しました");
                }
            }
        }

        yield return null; // 次のフレームまで待つ（カードが配置された後になる）

        foreach (Transform child in playerFieldZone)
        {
            FieldSlot slot = child.GetComponent<FieldSlot>();
            if (slot != null)
            {
                Debug.Log($"[DEBUG] スロット: {slot.name}, cardAnchorの子数: {slot.cardAnchor.childCount}");

                CardView card = slot.cardAnchor.GetComponentInChildren<CardView>();
                if (card != null)
                {
                    slot.currentCard = card;
                    Debug.Log("Card found");

                    if (slot.isPartnerSlot)
                    {
                        card.isPartner = true;
                        Debug.Log($"isPartner = true → {card.cardData.cardName}");
                    }
                    else
                    {
                        Debug.Log($"isPartner = false → {card.cardData.cardName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"cardAnchor に CardView が見つかりません（{slot.name}）");
                }
            }
        }
    }
    


    public bool HasReturnableUnit()
    {
        foreach (Transform child in playerFieldZone)
        {
            var slot = child.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard != null && !slot.currentCard.isPartner)
            {
                return true;
            }
        }
        return false;
    }
    public void RemoveFromField(CardView view)
    {
        if (view == null) return;

        // スロットから取り除く
        foreach (Transform child in playerFieldZone)
        {
            FieldSlot slot = child.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard == view)
            {
                slot.currentCard = null;
                break;
            }
        }

        view.transform.SetParent(null); // 親から切り離す（削除はしない）
    }

}
