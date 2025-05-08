using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandSelectionUI : MonoBehaviour
{
    public static HandSelectionUI Instance;

    private List<CardView> selectedCards = new List<CardView>();
    private int targetCount;
    private Action<List<CardView>> onFinish;
    public bool IsSelecting => isSelecting;

    private bool isSelecting = false;
    private int requiredCount = 0;
    private Action<CardView> onSelect;
    private Action onComplete;
    private Action<List<CardView>> onSelectList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartSelecting(int count, Action<List<CardView>> callback)
    {
        targetCount = count;
        onFinish = callback;
        selectedCards.Clear();
        HighlightHandCards(true);
    }

    public void SelectCard(CardView card)
    {
        if (selectedCards.Contains(card)) return;

        selectedCards.Add(card);
        card.SetSelected(true);

        if (selectedCards.Count >= targetCount)
        {
            FinishSelection();
        }
    }

    private void FinishSelection()
    {
        HighlightHandCards(false);
        onFinish?.Invoke(new List<CardView>(selectedCards));
        selectedCards.Clear();
    }

    private void HighlightHandCards(bool highlight)
    {
        foreach (Transform child in HandManager.Instance.handZone)
        {
            var view = child.GetComponent<CardView>();
            if (view != null) view.SetHighlight(highlight);
        }
    }
    public void StartSelection(int count, Func<CardView, bool> filter, Action<List<CardView>> onSelect, Action onComplete)
    {
        isSelecting = true;
        requiredCount = count;
        selectedCards.Clear();
        this.onSelectList = onSelect;
        this.onComplete = onComplete;

        foreach (Transform child in HandManager.Instance.handZone)
        {
            var view = child.GetComponent<CardView>();
            if (view != null && filter(view))
            {
                view.SetSelectable(true);
            }
        }

        Debug.Log($" 手札選択開始: {count}枚");
    }


    public void OnCardClickedFromHand(CardView card)
    {
        Debug.Log($" OnCardClickedFromHand 呼ばれた → {card.cardData.cardName}");

        if (!isSelecting)
        {
            Debug.LogWarning(" isSelecting = false。処理を中断します");
            return;
        }

        if (selectedCards.Contains(card))
        {
            Debug.LogWarning(" すでに選択済みのカードがクリックされました");
            return;
        }

        selectedCards.Add(card);
        card.SetSelectable(false);

        Debug.Log($" {card.cardData.cardName} を選択しました（{selectedCards.Count}/{requiredCount}）");

        if (selectedCards.Count >= requiredCount)
        {
            Debug.Log(" 必要な枚数が揃いました → onSelect & onComplete 実行");
            isSelecting = false;
            onSelectList?.Invoke(new List<CardView>(selectedCards));
            onComplete?.Invoke();

            foreach (Transform child in HandManager.Instance.handZone)
            {
                var view = child.GetComponent<CardView>();
                view?.SetSelectable(false);
            }
        }
    }



}
