using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{

    public static EventManager Instance;

    private void Awake()
    {
        Instance = this;
    }



    public Transform playerFieldZone;
    public CardEventData activeEvent; // 現在発動中のイベント（対象選択待ち）
    private CardView activeCardView; // 現在発動中のイベントカード




    void StandOneFaceDownUnit()
    {
        foreach (Transform child in playerFieldZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && !view.isFaceUp)
            {
                view.SetFaceUp(true);
                Debug.Log($" ユニット {view.GetCardData().cardName} を表にしました！");
                return;
            }
        }

        Debug.Log(" 裏のユニットがいませんでした");
    }

    public void UseEvent(CardEventData eventData, CardView sourceView)
    {
        activeEvent = eventData;
        activeCardView = sourceView;

        if (eventData.targetType == TargetType.ChooseOne)
        {
            BattleManager.Instance.SetAllCardModes(CardView.CardClickMode.SelectTarget);
        }
        else
        {
            ApplyEffect(eventData, null);
        }
    }

    public void ResolveTarget(CardView target)
    {
        if (activeEvent == null)
        {
            Debug.LogWarning("activeEvent が null です");
            return;
        }

        Debug.Log($" 対象に選ばれた：{target.GetCardData().cardName}");

        ApplyEffect(activeEvent, target);

        // 状態クリア！
        activeEvent = null;

        // クリックモードを元に戻す（重要！！）
        BattleManager.Instance.SetAllCardModes(CardView.CardClickMode.None);
        BattleManager.Instance.effectChoicePanel.SetActive(false);//ここでパネルを消す

        BattleManager.Instance.OnEffectUsed();
    }

    private void ApplyEffect(CardEventData eventData, CardView target)
    {
        switch (eventData.effectType)
        {
            case EventEffectType.StandFaceUp:
                // 対象が存在するかチェック（発動前キャンセル条件）
                if (eventData.targetType == TargetType.ChooseOne)
                {
                    bool hasFaceDown = false;
                    foreach (Transform cardObj in BattleManager.Instance.playerFieldZone)
                    {
                        CardView view = cardObj.GetComponent<CardView>();
                        if (view != null && !view.isFaceUp)
                        {
                            hasFaceDown = true;
                            break;
                        }
                    }

                    if (!hasFaceDown)
                    {
                        Debug.Log(" 裏向きユニットが存在しないため、イベント発動をキャンセルします");
                        return;
                    }
                }

                if (target != null && !target.isFaceUp)
                {
                    target.SetFaceUp(true);
                    Debug.Log($" ユニット {target.GetCardData().cardName} を表にしました！");

                    // 使用したイベントカード自身を控え室へ移動（activeCardView に事前にセットしておく）
                    if (activeCardView != null)
                    {
                        DiscardManager.Instance.MoveCardViewToDiscard(activeCardView);
                        //Destroy(activeCardView.gameObject); // UI上からも削除
                        activeCardView = null; // 念のためリセット
                    }
                }
                else if (target == null && eventData.targetType == TargetType.None)
                {
                    // 即時発動用（裏ユニット1体自動で表にする）
                    var all = FindObjectsOfType<CardView>();
                    foreach (var view in all)
                    {
                        if (!view.isFaceUp)
                        {
                            view.SetFaceUp(true);
                            Debug.Log($"🟢 自動：{view.GetCardData().cardName} を表にしました！");
                            return;
                        }
                    }
                    Debug.Log(" 自動：裏向きユニットが見つかりませんでした");
                }
                else
                {
                    Debug.Log(" 対象が無効（null または 既に表）");
                }
                break;

            case EventEffectType.PowerUp:
                if (target != null)
                {
                    target.tempPowerBoost += eventData.amount;
                    target.UpdatePowerText();
                    Debug.Log($" {target.GetCardData().cardName} の攻撃力が {eventData.amount} 上がった → 現在 {target.GetCurrentPower()}");

                    if (activeCardView != null)
                    {
                        DiscardManager.Instance.MoveCardViewToDiscard(activeCardView);
                        activeCardView = null;
                    }
                }
                else
                {
                    Debug.LogWarning(" PowerUpイベントの対象が無効です");
                }
                break;

            case EventEffectType.DamageSingle:
                if (target != null)
                {
                    target.TakeDamage(3); //  3点ダメージ
                    Debug.Log($" {target.cardData.cardName} に3ダメージを与えました");
                }
                else
                {
                    Debug.LogWarning("⚠️ Damageイベントの対象が無効です");
                }

                if (activeCardView != null)
                {
                    DiscardManager.Instance.MoveCardViewToDiscard(activeCardView);
                    activeCardView = null;
                }
                break;
            case EventEffectType.GlobalDamage:
                Debug.Log($" 全体{eventData.amount}ダメージを与えます");

                CardView[] allCards = FindObjectsOfType<CardView>();
                foreach (var view in allCards)
                {
                    if (view.cardData.isUnit)
                    {
                        view.TakeDamage(eventData.amount);
                    }
                }

                // 墓地へ送る処理（手札から使用された場合）
                if (activeCardView != null)
                {
                    DiscardManager.Instance.MoveCardViewToDiscard(activeCardView);
                    activeCardView = null;
                }
                break;
            case EventEffectType.SupportUp:
                if (target != null)
                {
                    target.tempSupportBoost += eventData.amount;

                    // 最大耐久も更新しなおす（耐久UP後に回復）
                    target.maxHP = target.GetEffectiveSupport();

                    // 現在HPがMAXを超えないように調整（任意）
                    target.currentHP = Mathf.Min(target.currentHP + eventData.amount, target.maxHP);

                    target.UpdateHPText();

                    Debug.Log($" {target.GetCardData().cardName} のHPが {eventData.amount} 上がった → 現在HP {target.currentHP}/{target.maxHP}");

                    if (activeCardView != null)
                    {
                        DiscardManager.Instance.MoveCardViewToDiscard(activeCardView);
                        activeCardView = null;
                    }
                }
                else
                {
                    Debug.LogWarning(" SupportUpイベントの対象が無効です");
                }
                break;
            case EventEffectType.GrantPenetrate:
                 if (target != null)
                {
                    target.tempHasPenetrate = true;
                    Debug.Log($" {target.GetCardData().cardName} にこのターン中【貫通】を付与しました");

                    if (activeCardView != null)
                    {
                        DiscardManager.Instance.MoveCardViewToDiscard(activeCardView);
                        activeCardView = null;
                    }
                }
                else
                {
                    Debug.LogWarning(" GrantPenetrateイベントの対象が無効です");
                }
                break;
            case EventEffectType.RestIfNew:
                if (target != null && target.isNewlySummoned)
                {
                    target.SetRest(true);
                    Debug.Log($" このターン登場した {target.GetCardData().cardName} をレストさせました！");

                    if (activeCardView != null)
                    {
                        DiscardManager.Instance.MoveCardViewToDiscard(activeCardView);
                        activeCardView = null;
                    }
                }
                else
                {
                    Debug.LogWarning(" 対象がこのターン登場していないため、効果は無効です");
                }
                break;
                // 他のイベント効果があればここに続けて追加可能！
                // case EventEffectType.PowerUp: ...
        }
    }


}

