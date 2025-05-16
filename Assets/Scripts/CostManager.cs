using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                                    if (cardView.IsRested ) return false;

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
                case CostType.FlipUnitFaceUp:
                    return new CostRequirement(
                        type,
                        1,
                        card =>
                        {
                            foreach (Transform t in FieldManager.Instance.playerFieldZone)
                            {
                                FieldSlot slot = t.GetComponent<FieldSlot>();
                                if (slot != null && slot.currentCard != null && !slot.currentCard.isFaceUp)
                                {
                                    return true;
                                }
                            }
                            return false;
                        },
                        () =>
                        {
                            FieldSelectionUI.Instance.StartSelection(
                                1,
                                cardView =>
                                {
                                    if (cardView == null || cardView.cardData == null) return false;
                                    if (cardView.isFaceUp) return false;

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
                                cardView =>
                                {
                                    cardView.isBeingCostProcessed = true; //  フラグで召喚処理制御
                                    cardView.SetFaceUp(true);
                                    Debug.Log($"{cardView.cardData.cardName} を表にしました（コスト処理済み）");
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete
                            );
                        }
                    );
                case CostType.FlipUnitFaceDown:
                    return new CostRequirement(
                        type,
                        1,
                        card =>
                        {
                            foreach (Transform t in FieldManager.Instance.playerFieldZone)
                            {
                                FieldSlot slot = t.GetComponent<FieldSlot>();
                                if (slot != null && slot.currentCard != null && slot.currentCard.isFaceUp)
                                {
                                    return true;
                                }
                            }
                            return false;
                        },
                        () =>
                        {
                            FieldSelectionUI.Instance.StartSelection(
                                1,
                                cardView =>
                                {
                                    if (cardView == null || cardView.cardData == null) return false;
                                    if (!cardView.isFaceUp) return false;

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
                                cardView =>
                                {
                                    cardView.isBeingCostProcessed = true;
                                    cardView.SetFaceUp(false);
                                    Debug.Log($"{cardView.cardData.cardName} を裏にしました（コスト処理済み）");
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete
                            );
                        }
                    );
                case CostType.FlipUnitFaceDownRest:
                    return new CostRequirement(
                        type,
                        1,
                        card => FieldManager.Instance.HasUnitThatCanRest(), // 裏＋レストにできるユニットがいるか
                        () =>
                        {
                            FieldSelectionUI.Instance.StartSelection(
                                1,
                                cardView =>
                                {
                                    if (cardView == null || cardView.cardData == null) return false;
                                    if (!cardView.isFaceUp) return false;

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
                                cardView =>
                                {
                                    cardView.SetFaceUp(false);  // 裏にする
                                    cardView.SetRest(true);     // レスト状態にする
                                    Debug.Log($"{cardView.cardData.cardName} を裏＋レスト状態にしました（コスト）");
                                },
                                CardPlayManager.Instance.OnCostPaymentComplete
                            );
                        }
                    );
                case CostType.BanishFromDiscard:
                    return new CostRequirement(
                        type,
                        1,
                        card => DiscardManager.Instance.GetPlayerDiscardViews().Count > 0,
                        () =>
                        {
                            DiscardSelectionUI.Instance.StartSelection(
                                1,
                                view => view != null && view.transform.parent == DiscardManager.Instance.discardZone, // ✅ 墓地内カードだけ通す
                                views =>
                                {
                                    foreach (var v in views)
                                    {
                                        if (v.transform.parent != DiscardManager.Instance.discardZone)
                                        {
                                            Debug.LogWarning($" 手札などの不正カードが選ばれました: {v.cardData.cardName}");
                                            continue;
                                        }

                                        DiscardManager.Instance.BanishCardData(v.cardData);
                                        GameObject.Destroy(v.gameObject);
                                        Debug.Log($" 墓地から除外: {v.cardData.cardName}");
                                    }
                                    CardPlayManager.Instance.OnCostPaymentComplete();
                                }
                            );
                        }
                    );
                case CostType.HasSpecificUnitOnField:
                    return new CostRequirement(
                        type,
                        0,
                        card => FieldManager.Instance.HasUnitWithName(card.cardData.requiredUnitName),
                        () =>
                        {
                            //Debug.Log($"場に {cardView.cardData.requiredUnitName} を含むカードが存在 → 条件達成");
                            CardPlayManager.Instance.OnCostPaymentComplete();
                        }
                    );
                case CostType.BanishFromDiscardX:
                    return new CostRequirement(
                        type,
                        amount,
                        card => DiscardManager.Instance.GetPlayerDiscardViews().Count >= amount,
                        () =>
                        {
                            DiscardSelectionUI.Instance.StartSelection(
                                amount,
                                view => view != null && view.transform.parent == DiscardManager.Instance.discardZone,
                                views =>
                                {
                                    foreach (var v in views)
                                    {
                                        DiscardManager.Instance.BanishCardData(v.cardData);
                                        GameObject.Destroy(v.gameObject);
                                        Debug.Log($"墓地から除外: {v.cardData.cardName}");
                                    }
                                    CardPlayManager.Instance.OnCostPaymentComplete();
                                }
                            );
                        }
                    );
                case CostType.Return1_Discard1_SS:
                    return new CostRequirement(
                        type,
                        2,
                        card =>
                        {
                            // 自分以外の手札カードが1枚以上必要
                            int validHandCount = HandManager.Instance.GetCardViewsInHand()
                                .Where(view => view != card)
                                .Count();

                            Debug.Log($"[SS判定] 自分以外の手札枚数: {validHandCount}");

                            return FieldManager.Instance.HasReturnableUnit() && validHandCount >= 1;
                        },
                        () =>
                        {
                            // ① フィールドのユニットを選んで手札に戻す
                            FieldSelectionUI.Instance.StartSelection(
                                1,
                                view => view != null && !view.IsPartner,
                                view =>
                                {
                                    if (view.IsEXUnit())
                                    {
                                        DiscardManager.Instance.BanishCardData(view.cardData);
                                        view.DestroyFromField();
                                        Debug.Log($"EXユニットを除外しました: {view.cardData.cardName}");
                                    }
                                    else
                                    {
                                        view.isBeingReturnedToHand = true;
                                        FieldManager.Instance.RemoveFromField(view);
                                        HandManager.Instance.AddToHand(view.cardData);
                                        GameObject.Destroy(view.gameObject);
                                        Debug.Log($"ユニットを手札に戻しました: {view.cardData.cardName}");
                                    }

                                    // ② 手札を1枚捨てる（自身を除外）
                                    var targetCard = CardPlayManager.Instance.deferredCardView;
                                    HandSelectionUI.Instance.StartSelection(
                                        1,
                                        handView => handView != targetCard,
                                        views =>
                                        {
                                            foreach (var discard in views)
                                            {
                                                HandManager.Instance.RemoveCard(discard);
                                                DiscardManager.Instance.AddToDiscard(discard.cardData);
                                                Debug.Log($"手札から捨てました: {discard.cardData.cardName}");
                                            }

                                            CardPlayManager.Instance.OnCostPaymentComplete();
                                        },
                                        null
                                    );
                                },
                                null
                            );
                        }
                    );



            }
        }
    }



}