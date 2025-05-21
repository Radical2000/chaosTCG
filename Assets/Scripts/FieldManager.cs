using System.Collections;
using System.Collections.Generic;
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
    public void ResolveEndPhase(System.Action onComplete)
    {
        Debug.Log(" エンドフェイズ処理開始：表戻し・スタンド処理");

        List<CardView> flipCandidates = new List<CardView>();
        List<CardView> standCandidates = new List<CardView>();

        foreach (Transform slotObj in playerFieldZone)
        {
            FieldSlot slot = slotObj.GetComponent<FieldSlot>();
            if (slot == null || slot.currentCard == null) continue;

            CardView view = slot.currentCard;
            if (!view.isFaceUp) flipCandidates.Add(view);
            if (view.isRested) standCandidates.Add(view);
        }

        void ResolveStandPhase()
        {
            if (standCandidates.Count == 1)
            {
                var target = standCandidates[0];
                target.SetRest(false);
                Debug.Log($" スタンドさせました（自動）：{target.cardData.cardName}");
                ResetAllTempPower();
                onComplete?.Invoke();
            }
            else if (standCandidates.Count > 1)
            {
                FieldSelectionUI.Instance.StartSelection(
                    1,
                    card => standCandidates.Contains(card),
                    selected =>
                    {
                        if (selected != null)
                        {
                            selected.SetRest(false);
                            Debug.Log($" スタンドさせました（選択）：{selected.cardData.cardName}");
                        }
                        ResetAllTempPower();
                        onComplete?.Invoke();
                    },
                    null
                );
            }
            else
            {
                ResetAllTempPower();
                onComplete?.Invoke();
            }
        }

        if (flipCandidates.Count == 1)
        {
            var target = flipCandidates[0];
            target.SetFaceUp(true);
            Debug.Log($" 表に戻しました（自動）：{target.cardData.cardName}");
            ResolveStandPhase();
        }
        else if (flipCandidates.Count > 1)
        {
            FieldSelectionUI.Instance.StartSelection(
                1,
                card => flipCandidates.Contains(card),
                selected =>
                {
                    if (selected != null)
                    {
                        selected.SetFaceUp(true);
                        Debug.Log($" 表に戻しました（選択）：{selected.cardData.cardName}");
                    }
                    ResolveStandPhase();
                },
                null
            );
        }
        else
        {
            ResolveStandPhase();
        }
    }



    //ターン終了時にステータスリセット用
    public void ResetAllTempPower()
    {
        Debug.Log(" ResetAllTempPowerを呼んだ");
        foreach (Transform slotObj in playerFieldZone)
        {
            FieldSlot slot = slotObj.GetComponent<FieldSlot>();
            if (slot == null || slot.cardAnchor == null) continue;

            if (slot.cardAnchor.childCount == 0) continue;

            Transform cardObj = slot.cardAnchor.GetChild(0);
            CardView view = cardObj.GetComponent<CardView>();

            if (view != null && view.cardData != null)
            {
                view.tempPowerBoost = 0;
                view.UpdatePowerText();
                Debug.Log($"ATKリセット：{view.cardData.cardName}");
            }
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
                Debug.Log(" 手札に戻せるユニットがいる");
                return true;
            }
        }
        Debug.Log(" 手札に戻せるユニットがいない");
        return false;
    }
    public void RemoveFromField(CardView view)
    {
        if (view == null) return;

        foreach (Transform child in playerFieldZone)
        {
            FieldSlot slot = child.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard == view)
            {
                slot.currentCard = null; 
                break;
            }
        }

        view.transform.SetParent(null); // 親から切り離す、削除はなし
    }

    public bool HasRestableUnit()
    {
        foreach (Transform t in playerFieldZone)
        {
            FieldSlot slot = t.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard != null && !slot.currentCard.IsRested)
            {
                return true;
            }
        }
        return false;
    }
 
    public bool HasFacedownUnit()
    {
        foreach (Transform t in playerFieldZone)
        {
            FieldSlot slot = t.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard != null && !slot.currentCard.isFaceUp)
            {
                return true;
            }
        }
        return false;
    }
    public bool HasUnitThatCanRest()
    {
        foreach (Transform t in playerFieldZone)
        {
            FieldSlot slot = t.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard != null)
            {
                var card = slot.currentCard;
                if (card.isFaceUp)
                    return true;
            }
        }
        return false;
    }

    public bool HasUnitWithName(string name)
    {
        foreach (Transform t in playerFieldZone)
        {
            FieldSlot slot = t.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard != null)
            {
                if (slot.currentCard.cardData.cardName.Contains(name))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
