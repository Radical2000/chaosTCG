using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardData;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public class CostManager : MonoBehaviour
{
    public static CostManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }

    // コスト支払い中の情報（召喚元など）
    private int costToPay = 0;
    private Action<List<CardView>> onSelectedCards;

    // 選択による支払い開始
    public void StartSelectDiscard(int count, Action<List<CardView>> onDone)
    {
        costToPay = count;
        onSelectedCards = onDone;
        HandSelectionUI.Instance.StartSelecting(count, OnCardsSelected);
    }

    private void OnCardsSelected(List<CardView> selected)
    {
        onSelectedCards?.Invoke(selected);
        costToPay = 0;
        onSelectedCards = null;
    }


    public static class CostFactory
    {

        public static CostRequirement Create(CostType type, int amount)
        {
            var cardView = CardPlayManager.Instance.deferredCardView;
            if (cardView != null)
            {
                cardView.isBeingCostProcessed = true;
                FieldManager.Instance.selectedCardToSummon = cardView;
            }

            switch (type)
            {
                case CostType.DiscardX:
                    return new CostRequirement(
                        type,
                        amount,
                        () => HandManager.Instance.HandCount >= amount,
                        () =>
                        {
                            var targetCard = CardPlayManager.Instance.deferredCardView;
                            HandSelectionUI.Instance.StartSelection(
                                amount,
                                view => view != targetCard, // 出そうとしているカードを除外
                                views =>
                                {
                                    foreach (var view in views)
                                    {
                                        HandManager.Instance.RemoveCard(view);
                                        DiscardManager.Instance.AddToDiscard(view.cardData);
                                        Debug.Log($"捨てました: {view.cardData.cardName}");
                                    }
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete
                            );
                        });

                case CostType.DiscardXUnit:
                    return new CostRequirement(
                        type,
                        amount,
                        () => HandManager.Instance.CountUnitCardsInHand() >= amount,
                        () =>
                        {
                            var targetCard = CardPlayManager.Instance.deferredCardView;
                            HandSelectionUI.Instance.StartSelection(
                                amount,
                                view => view != targetCard && view.cardData.isUnit, // 除外＋ユニット条件
                                views =>
                                {
                                    foreach (var view in views)
                                    {
                                        HandManager.Instance.RemoveCard(view);
                                        DiscardManager.Instance.AddToDiscard(view.cardData);
                                        Debug.Log($"ユニットを捨てました: {view.cardData.cardName}");
                                    }
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete
                            );
                        });

                case CostType.None:
                default:
                    return new CostRequirement(
                        type,
                        amount,
                        () => true,
                        () =>
                        {
                            CardPlayManager.Instance.OnCostPaymentComplete();
                        });

                case CostType.ReturnOneToHand:
                    return new CostRequirement(
                        type,
                        1,
                        () => FieldManager.Instance.HasReturnableUnit(),
                        () =>
                        {
                            FieldSelectionUI.Instance.StartSelection(
                                1,
                                cardView =>
                                {
                                    if (cardView == null || cardView.cardData == null)
                                    {
                                        Debug.LogWarning("ReturnOneToHand: cardViewまたはcardDataがnullです");
                                        return;
                                    }

                                    if (cardView.IsEXUnit())
                                    {
                                        DiscardManager.Instance.BanishCardData(cardView.cardData);
                                        cardView.DestroyFromField();
                                        Debug.Log($"EXユニットを除外: {cardView.cardData.cardName}");
                                    }
                                    else
                                    {
                                        FieldManager.Instance.RemoveFromField(cardView);
                                        HandManager.Instance.AddToHand(cardView.cardData); //  生成付き追加
                                        GameObject.Destroy(cardView.gameObject); //  元カードを破棄
                                        Debug.Log($"通常ユニットを手札に戻した: {cardView.cardData.cardName}");
                                    }
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete
                            );
                        }
                    );

            }
        }
    }



}