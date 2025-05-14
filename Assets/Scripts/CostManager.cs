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
                        card => HandManager.Instance.HandCountExcluding(card) >= amount,
                        () =>
                        {
                            var targetCard = CardPlayManager.Instance.deferredCardView;
                            HandSelectionUI.Instance.StartSelection(
                                amount,
                                view => view != targetCard && view.cardData != null,
                                views =>
                                {
                                    foreach (var view in views)
                                    {
                                        HandManager.Instance.RemoveCard(view);
                                        DiscardManager.Instance.AddToDiscard(view.cardData);
                                        Debug.Log($"捨てました: {view.cardData.cardName}");
                                    }

                                    CardPlayManager.Instance.OnCostPaymentComplete();
                                },
                                null
                            );
                        }
                    );

                case CostType.DiscardXUnit:
                    return new CostRequirement(
                        type,
                        amount,
                        card => HandManager.Instance.CountUnitCardsInHand() >= amount + 1,
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
                        card => true,
                        () =>
                        {
                            CardPlayManager.Instance.OnCostPaymentComplete();
                        });

                case CostType.ReturnOneToHand:
                    return new CostRequirement(
                        type,
                        1,
                        card => FieldManager.Instance.HasReturnableUnit(),
                        () =>
                        {
                            FieldSelectionUI.Instance.StartSelection(
                                1,
                                // フィルタ（boolを返す）
                                cardView =>
                                {
                                    if (cardView == null || cardView.cardData == null) return false;
                                    if (cardView.IsPartner) return false;

                                    // フィールド上に存在するスロットのcurrentCardと一致しているか
                                    foreach (Transform t in FieldManager.Instance.playerFieldZone)
                                    {
                                        FieldSlot slot = t.GetComponent<FieldSlot>();
                                        if (slot != null && slot.currentCard == cardView)
                                        {
                                            return true;
                                        }
                                    }

                                    return false;
                                },
                                // 選択結果の処理（void）
                                cardView =>
                                {
                                    if (cardView.IsEXUnit())
                                    {
                                        DiscardManager.Instance.BanishCardData(cardView.cardData);
                                        cardView.DestroyFromField();
                                        Debug.Log($"EXユニットを除外: {cardView.cardData.cardName}");
                                    }
                                    else
                                    {
                                        cardView.isBeingReturnedToHand = true;
                                        FieldManager.Instance.RemoveFromField(cardView);
                                        HandManager.Instance.AddToHand(cardView.cardData);
                                        GameObject.Destroy(cardView.gameObject);
                                        Debug.Log($"通常ユニットを手札に戻した: {cardView.cardData.cardName}");
                                    }
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete
                            );
                        }
                    );

                case CostType.RestOneUnit:
                    return new CostRequirement(
                        type,
                        1,
                        card => FieldManager.Instance.HasRestableUnit(), // 条件チェック（未レストかつ非パートナー）
                        () =>
                        {
                            FieldSelectionUI.Instance.StartSelection(
                                1,
                                cardView =>
                                {
                                    if (cardView == null || cardView.cardData == null) return false;
                                    if (cardView.IsRested || cardView.IsPartner) return false;

                                    foreach (Transform t in FieldManager.Instance.playerFieldZone)
                                    {
                                        FieldSlot slot = t.GetComponent<FieldSlot>();
                                        if (slot != null && slot.currentCard == cardView) return true;
                                    }
                                    return false;
                                },
                                cardView =>
                                {
                                    cardView.SetRest(true); //  表示と内部両方
                                    Debug.Log($"{cardView.cardData.cardName} をレストしました");
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete //  コスト完了後、召喚フローを再開
                            );
                        }
                    );


            }
        }
    }



}