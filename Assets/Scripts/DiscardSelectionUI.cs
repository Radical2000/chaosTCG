
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiscardSelectionUI : MonoBehaviour
{
    public static DiscardSelectionUI Instance;

    private int requiredCount;
    private Action<List<CardView>> onSelected;
    private List<CardView> selectedCards = new List<CardView>();
    private bool isSelecting = false;
    private int selectionCount;
    public bool IsSelecting => isSelecting;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private List<CardView> currentCandidates;

    public void StartSelection(int count, Func<CardView, bool> originalFilter, Action<List<CardView>> onSelect)
    {
        isSelecting = true;
        requiredCount = count;
        selectionCount = count;
        this.onSelected = onSelect;
        selectedCards.Clear();
        currentCandidates = new List<CardView>();

        foreach (var cardView in DiscardManager.Instance.GetPlayerDiscardViews())
        {
            
            if (originalFilter(cardView))
            {
                cardView.SetSelectable(true);
                currentCandidates.Add(cardView);
            }
        }
        Debug.Log($" ��n�I���J�n: {count}��");
        Debug.Log("��n�J�[�h�I�����[�h�J�n�i�t�B���^�ρj");
    }

    public void OnCardClickedFromDiscard(CardView view)
    {
        Debug.Log("OnCardClickedFromDiscard�ɓ�����");
        if (!currentCandidates.Contains(view)) return; // ���S�m�F
        
        if (!view.IsSelectable()) return;
        if (!selectedCards.Contains(view))
        {
            selectedCards.Add(view);
            Debug.Log("selectionCount:"+selectionCount);
            Debug.Log("selectedCards.Count:" + selectedCards.Count);
            if (selectedCards.Count == selectionCount)
            {
               
                FinishSelection();
            }
        }
    }
    private void FinishSelection()
    {
        Debug.Log("FinishSelection()�J�n");

        foreach (var v in currentCandidates)
            v.SetSelectable(false);

        onSelected?.Invoke(selectedCards.ToList()); 

        selectedCards.Clear();
        currentCandidates.Clear();
        isSelecting = false;
    }
}
