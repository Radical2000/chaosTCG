using System.Collections.Generic;
using UnityEngine;

public class CIPEffectManager : MonoBehaviour
{
    public static CIPEffectManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TryTriggerCIPEffect(CardView cardView)
    {
        if (cardView == null || cardView.cardData == null) return;

        switch (cardView.cardData.cipEffectType)
        {
            case CIPEffectType.None:
                break;
            case CIPEffectType.Discard1:
                HandleDiscard1Effect(cardView);
                break;
            case CIPEffectType.Draw2:
                HandleDraw2Effect(cardView);
                break;
            case CIPEffectType.FlipFaceUp:
                HandleFlipFaceUp(cardView);
                break;
            case CIPEffectType.DamageToTarget5:
                DealSingleDamage(cardView, 5);
                break;
            case CIPEffectType.DamageToAllEnemy3:
                HandleDamageToAllEnemy3(cardView);
                break;
            case CIPEffectType.BuffAllAlliesPower2:
                HandleBuffAllAlliesPower2(cardView);
                break;

            case CIPEffectType.FlipEnemyFaceDown:
                HandleFlipEnemyFaceDown(cardView);
                break;
        }
    }

    // ================= 共通処理 =================

    private bool IsSelectableForEffect(CardView cardView)
    {
        if (cardView == null || cardView.cardData == null) return false;

        foreach (Transform t in FieldManager.Instance.playerFieldZone)
        {
            FieldSlot slot = t.GetComponent<FieldSlot>();
            if (slot?.currentCard == cardView) return true;
        }

        foreach (Transform t in FieldManager.Instance.enemyFieldZone)
        {
            FieldSlot slot = t.GetComponent<FieldSlot>();
            if (slot?.currentCard == cardView) return true;
        }

        return false;
    }

    private void SelectFieldUnit(
        int count,
        System.Func<CardView, bool> filter,
        System.Action<CardView> onSelected,
        string debugMessageIfNone)
    {
        bool hasCandidate = false;

        foreach (Transform t in FieldManager.Instance.playerFieldZone)
        {
            var slot = t.GetComponent<FieldSlot>();
            if (slot?.currentCard != null && filter(slot.currentCard))
            {
                hasCandidate = true;
                break;
            }
        }

        foreach (Transform t in FieldManager.Instance.enemyFieldZone)
        {
            var slot = t.GetComponent<FieldSlot>();
            if (slot?.currentCard != null && filter(slot.currentCard))
            {
                hasCandidate = true;
                break;
            }
        }

        if (!hasCandidate)
        {
            Debug.Log($"CIP効果：{debugMessageIfNone}");
            return;
        }

        FieldSelectionUI.Instance.StartSelection(
            count,
            cardView => cardView != null && cardView.cardData != null && filter(cardView),
            onSelected,
            null
        );
    }

    private void SelectEnemyFieldUnit(
        int count,
        System.Func<CardView, bool> filter,
        System.Action<CardView> onSelected,
        string debugMessageIfNone)
    {
        bool hasCandidate = false;

        foreach (Transform t in FieldManager.Instance.enemyFieldZone)
        {
            var slot = t.GetComponent<FieldSlot>();
            if (slot?.currentCard != null && filter(slot.currentCard))
            {
                hasCandidate = true;
                break;
            }
        }

        if (!hasCandidate)
        {
            Debug.Log($"CIP効果：{debugMessageIfNone}");
            return;
        }

        FieldSelectionUI.Instance.StartSelection(
            count,
            cardView => cardView != null && cardView.cardData != null && filter(cardView),
            onSelected,
            null
        );
    }

    // ================= 効果処理 =================

    private void HandleDiscard1Effect(CardView sourceCard)
    {
        Debug.Log($"CIP効果：{sourceCard.cardData.cardName} の効果で1枚捨てます");

        HandSelectionUI.Instance.StartSelection(
            1,
            view => view != null && view != sourceCard,
            selected =>
            {
                if (selected == null || selected.Count == 0)
                {
                    Debug.Log("CIP効果の選択がキャンセルされました");
                    return;
                }

                foreach (var discard in selected)
                {
                    HandManager.Instance.RemoveCard(discard);
                    DiscardManager.Instance.AddToDiscard(discard.cardData);
                    Debug.Log($"CIP効果で {discard.cardData.cardName} を捨てました");
                }
            },
            null
        );
    }

    private void HandleDraw2Effect(CardView sourceCard)
    {
        Debug.Log($"CIP効果：{sourceCard.cardData.cardName} の効果で2枚引きます");
        HandManager.Instance.DrawCard(2);
    }

    private void HandleFlipFaceUp(CardView source)
    {
        SelectFieldUnit(
            1,
            unit => !unit.isFaceUp && IsSelectableForEffect(unit), 
            target =>
            {
                target.SetFaceUp(true);
                Debug.Log($"CIP効果：{source.cardData.cardName} により {target.cardData.cardName} を表にしました！");
            },
            "裏向きユニットが存在しません"
        );
    }


    private void DealSingleDamage(CardView source, int damageAmount)
    {
        SelectEnemyFieldUnit(
            1,
            unit => unit.cardData.isUnit && IsSelectableForEffect(unit),
            target =>
            {
                target.TakeDamage(damageAmount);
                Debug.Log($"CIP効果：{source.cardData.cardName} が {target.cardData.cardName} に{damageAmount}ダメージを与えました");
            },
            "敵ユニットが存在しません"
        );
    }

    private void HandleDamageToAllEnemy3(CardView source)
    {
        int damage = 3;
        bool hasTarget = false;

        foreach (Transform t in FieldManager.Instance.enemyFieldZone)
        {
            var slot = t.GetComponent<FieldSlot>();
            if (slot?.currentCard != null && slot.currentCard.cardData.isUnit)
            {
                hasTarget = true;
                slot.currentCard.TakeDamage(damage);
                Debug.Log($"CIP効果：{source.cardData.cardName} が {slot.currentCard.cardData.cardName} に{damage}ダメージ");
            }
        }

        if (!hasTarget)
        {
            Debug.Log("CIP効果：敵ユニットが存在しないため、効果を発動しません");
        }
    }
    private void HandleBuffAllAlliesPower2(CardView source)
    {
        int boost = 2;
        bool hasTarget = false;

        foreach (Transform t in FieldManager.Instance.playerFieldZone)
        {
            FieldSlot slot = t.GetComponent<FieldSlot>();
            if (slot?.currentCard != null && slot.currentCard.cardData.isUnit)
            {
                var unit = slot.currentCard;
                unit.tempPowerBoost += boost;
                unit.UpdatePowerText();

                Debug.Log($"CIP効果：{source.cardData.cardName} により {unit.cardData.cardName} の攻撃力が {boost} 上がった → 現在 {unit.GetCurrentPower()}");
                hasTarget = true;
            }
        }

        if (!hasTarget)
        {
            Debug.Log("CIP効果：味方ユニットが存在しないため、パワーアップは行われません");
        }
    }

    private void HandleFlipEnemyFaceDown(CardView source)
    {
        SelectEnemyFieldUnit(
            1,
            unit => unit.isFaceUp && !unit.IsPartner && IsSelectableForEffect(unit),
            target =>
            {
                target.SetFaceUp(false);
                Debug.Log($"CIP効果：{source.cardData.cardName} により {target.cardData.cardName} を裏向きにしました！");
            },
            "表の相手ユニット（パートナー除く）が存在しません"
        );
    }
}
