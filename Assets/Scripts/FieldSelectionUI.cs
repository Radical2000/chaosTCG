using System;
using System.Collections.Generic;
using UnityEngine;

public class FieldSelectionUI : MonoBehaviour
{
    public static FieldSelectionUI Instance;

    private bool isSelecting = false;
    private int requiredCount = 1;
    private List<CardView> selectedCards = new List<CardView>();
    private Action<CardView> onSelect;
    private Action onComplete;
    private Func<CardView, bool> filter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool IsSelecting => isSelecting;

    public void StartSelection(int count, Func<CardView, bool> filter, Action<CardView> onSelect, Action onComplete)
    {
        isSelecting = true;
        requiredCount = count;
        selectedCards.Clear();
        this.filter = filter;
        this.onSelect = onSelect;
        this.onComplete = onComplete;

        foreach (Transform slotTransform in FieldManager.Instance.playerFieldZone)
        {
            var slot = slotTransform.GetComponent<FieldSlot>();
            if (slot != null && slot.currentCard != null && filter(slot.currentCard)) // ✅ filter を適用
            {
                slot.currentCard.SetSelectable(true);
            }
        }

        Debug.Log($" フィールド選択開始（フィルタあり）: {count}体");
    }

    public void OnCardClickedFromField(CardView card)
    {
        if (!isSelecting || !filter(card)) // ← filter チェックを追加
        {
            Debug.LogWarning(" 不正なカードがクリックされた、または選択モードでない");
            return;
        }
        Debug.Log($" OnCardClickedFromField 呼ばれた → {card.cardData.cardName}");

        if (!isSelecting)
        {
            Debug.LogWarning(" isSelecting = false。処理を中断");
            return;
        }

        if (selectedCards.Contains(card))
        {
            Debug.LogWarning(" すでに選択済みのカードがクリックされた");
            return;
        }

        selectedCards.Add(card);
        card.SetSelectable(false);

        onSelect?.Invoke(card);

        if (selectedCards.Count >= requiredCount)
        {
            Debug.Log(" フィールド選択完了 → onComplete 呼び出し");
            isSelecting = false;
            onComplete?.Invoke();

            foreach (Transform slotTransform in FieldManager.Instance.playerFieldZone)
            {
                var slot = slotTransform.GetComponent<FieldSlot>();
                if (slot != null && slot.currentCard != null)
                {
                    slot.currentCard.SetSelectable(false);
                }
            }
        }
    }
}
