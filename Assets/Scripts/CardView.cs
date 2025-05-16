using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    public CardData debugCardData;

    public enum CardClickMode { None, Attack, GuardSelect, SelectTarget, UseEvent }
    public CardClickMode clickMode = CardClickMode.None;

    public bool isFaceUp = true;
    public bool isRested = false;
    public bool tempHasPenetrate = false;
    public bool isNewlySummoned = false;

    public int tempPowerBoost = 0;
    public int tempDamage = 0;
    public int tempSupportBoost = 0;
    public int permSupportBoost = 0;
    public int power;
    public int maxHP;
    public int currentHP;

    public GameObject highlightFrame;
    private bool isHighlighted;
    public bool isBeingCostProcessed = false;
    public bool isBeingReturnedToHand = false;


    [Header("UI参照")]
    public Image cardImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI hpText;
    public GameObject backside;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI levelText;
    public bool isPartner = false;
    public bool IsPartner => isPartner;
    public bool IsRested => isRested;


    public CardData cardData;
    //EXステータス
    public int currentLevel = 1;
    public int accumulatedAtkBoost = 0;
    public int accumulatedHpBoost = 0;

    private bool isSelectable = false;

    private void Start()
    {
        if (cardData == null && debugCardData != null)
        {
            SetCard(debugCardData, true);
        }
    }

    public void SetCard(CardData data, bool faceUp = true)
    {
        cardData = data;
        InitHP();
        power=cardData.power;
        if (nameText != null) nameText.text = data.cardName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (powerText != null) powerText.text = GetCurrentPower().ToString();
        if (hpText != null) hpText.text = $"{currentHP}/{maxHP}";
        if (cardImage != null && data.cardImage != null) cardImage.sprite = data.cardImage;
        if (backside != null) backside.SetActive(!faceUp);
        if (levelText != null) levelText.text = $"Lv{cardData.currentLevel}";

    }

    public void InitHP()
    {
        maxHP = GetEffectiveSupport();
        currentHP = maxHP;
        tempDamage = 0;
    }

    public int GetEffectiveSupport() => cardData.support + tempSupportBoost + permSupportBoost;

    public CardData GetCardData() => cardData;

    public int GetCurrentPower() => cardData.power + tempPowerBoost;

    public void UpdatePowerText()
    {
        if (powerText != null)
            powerText.text = power.ToString(); 
    }

    public void UpdateHPText()
    {
        if (hpText != null)
            hpText.text = $"{currentHP}/{maxHP}";
    }

    public bool GetEffectivePenetrate() => cardData.hasPenetrate || tempHasPenetrate;

    public void SetFaceUp(bool face)
    {
        isFaceUp = face;
        backside.SetActive(!face);
        if (face)
        {
            currentHP = maxHP; // 表に戻ったら最大HPに回復
            Debug.Log($"{cardData.cardName} を表に戻しました → HP回復 {currentHP}/{maxHP}");
        }
        else
        {
            currentHP = 0;
            Debug.Log($"{cardData.cardName} が裏になりました → HP 0 にリセット");
        }
           

        UpdateHPText();
    }

    public void SetRest(bool rest)
    {
        isRested = rest;
        transform.rotation = rest ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
    }

    public void TakeDamage(int amount)
    {
        tempDamage += amount;
        currentHP = maxHP - tempDamage;

        Debug.Log($"{cardData.cardName} が {amount} ダメージを受けました → 現在HP: {currentHP}");

        if (currentHP <= 0)
        {
            SetFaceUp(false);       // ✅ 裏にするだけ
            Debug.Log($"{cardData.cardName} は裏になりました（HP0）");
        }

        if (hpText != null)
        {
            hpText.text = $"{currentHP}/{maxHP}";
        }
    }



    public void EndTurnReset()
    {
        tempDamage = 0;
        currentHP = maxHP;
        tempSupportBoost = 0;
        tempHasPenetrate = false;
        isNewlySummoned = false;

        SetCard(cardData);
        UpdatePowerText();
        UpdateHPText();
    }

    public void OnClick()
    {
        if (!cardData.isUnit) return;
        if (transform.parent.name != "PlayerHand") return;

        FieldManager.Instance.SelectCardForSummon(this);
    }

    public void OnClickActionButton()
    {
        Debug.Log($" cardName: {cardData.cardName}, isUnit: {cardData.isUnit}, parent: {transform.parent.name}");
        Debug.Log($" OnClickActionButton 実行！clickMode = {clickMode}");
        Debug.Log($" isSelectable = {isSelectable}, HandSelectionUI.Instance?.IsSelecting = {(HandSelectionUI.Instance != null ? HandSelectionUI.Instance.IsSelecting.ToString() : "null")}");

        // EXレベルアップ用素材カードの選択
        if (transform.parent.name == "PlayerHand")
        {
            /// EXレベルアップ用素材カードの選択
            if (transform.parent.name == "PlayerHand")
            {
                //  ① レベルアップ素材選択（最優先でチェック）
                if (EXManager.Instance.IsWaitingForLevelUpMaterial())
                {
                    Debug.Log($" レベルアップ素材として {cardData.cardName} を選択しました");
                    EXManager.Instance.OnSelectLevelUpMaterial(this);
                    return;
                }

                //  ② EX素材選択（materialMode が EX のときだけ）
                if (EXManager.Instance.HasSelectedEXCard() && EXManager.Instance.materialMode == EXManager.MaterialUseMode.EX)
                {
                    Debug.Log($" EX素材カードとして {cardData.cardName} を選択しました");
                    EXManager.Instance.OnClickMaterialCard(this);
                    return;
                }
            }
        }
        if (DiscardSelectionUI.Instance != null && DiscardSelectionUI.Instance.IsSelecting)
        {
            Debug.Log(" 墓地選択モードでクリック検出 → DiscardSelectionUI に通知");
            DiscardSelectionUI.Instance.OnCardClickedFromDiscard(this);
            return;
        }

        if (FieldSelectionUI.Instance != null && FieldSelectionUI.Instance.IsSelecting)
        {
            Debug.Log(" フィールド選択モードでクリック検出 → FieldSelectionUI に通知");
            FieldSelectionUI.Instance.OnCardClickedFromField(this);
            return;
        }

        //  手札選択モードが有効なら優先で処理を渡す
        if (isSelectable && HandSelectionUI.Instance != null && HandSelectionUI.Instance.IsSelecting)
        {
            Debug.Log(" 手札選択モードでカードをクリック → OnCardClickedFromHand に進む");
            HandSelectionUI.Instance.OnCardClickedFromHand(this);
            return;
        }
        else
        {
            if (!isSelectable) Debug.Log(" isSelectable が false のため選択できません");
            if (HandSelectionUI.Instance == null) Debug.Log(" HandSelectionUI.Instance が null");
            else if (!HandSelectionUI.Instance.IsSelecting) Debug.Log(" HandSelectionUI は選択モードではありません");
        }



        // EXカード（EXUnitPanel）の選択処理
        if (transform.parent.name == "EXUnitPanel")
        {
            Debug.Log($" EXカード {cardData.cardName} を選択しました");
            EXManager.Instance.OnSelectEXCard(cardData); // ✅ これが必要！
            return;
        }
        // ▼ EX C型用：墓地素材クリック
        Debug.Log($" cardName: {cardData.cardName}, isUnit: {cardData.isUnit}, parent: {transform.parent.name}");

        // --- 墓地素材クリック判定 ---
        if (transform.parent != null && transform.parent.parent != null && transform.parent.parent.parent != null)
        {
            Debug.Log($"[DEBUG] 親: {transform.parent.name}, 祖父: {transform.parent.parent.name}, 曾祖父: {transform.parent.parent.parent.name}");

            if (transform.parent.parent.parent.name.Contains("Discard"))
            {
                Debug.Log("[DEBUG] 墓地内カードと判定できた！（曾祖父がDiscard）");

                if (EXManager.Instance.HasSelectedEXCard())
                {
                    Debug.Log("[DEBUG] EXカードも選ばれている！素材登録に進む！");
                    EXManager.Instance.OnClickMaterialCardFromDiscard(this);
                    return;
                }
                else
                {
                    Debug.LogWarning("[DEBUG] EXカードが選ばれていません！");
                }
            }
        }




        // ① 選択対象モード（イベント発動中）
        if (clickMode == CardClickMode.SelectTarget)
        {
            Debug.Log("選択モード：ResolveTargetを呼びます");
            EventManager.Instance.ResolveTarget(this);
            return;
        }

        // ② イベントカードの発動
        if (!cardData.isUnit)
        {
            if (transform.parent.name != "PlayerHand")
            {
                Debug.Log(" 手札以外のイベントカードは使用できません");
                return;
            }

            if (cardData.linkedEvent != null)
            {
                EventManager.Instance.UseEvent(cardData.linkedEvent, this);
            }
            else
            {
                Debug.LogWarning("イベント発動に失敗しました（linkedEventがnull）");
            }
            return;
        }

        //  ユニット召喚（カードクリックによる通常召喚）
        if (clickMode == CardClickMode.None && cardData.isUnit)
        {
            if (transform.parent.name != "PlayerHand")
            {
                Debug.LogWarning(" 手札以外のユニットカードは出せません！");
                return;
            }

            Debug.Log("ユニットを召喚しようとしています");
            CardPlayManager manager = FindObjectOfType<CardPlayManager>();
            if (manager != null)
            {
                manager.PlayCard(this); // 召喚処理へ
            }
            return;
        }

        // ③ ユニットカードの処理（攻撃・防御）
        switch (clickMode)
        {
            case CardClickMode.Attack:
                OnClickAttack();
                break;
            case CardClickMode.GuardSelect:
                TrySelectAsGuard();
                break;
            case CardClickMode.UseEvent:
                // イベントカードは上で処理済み
                break;
            default:
                Debug.Log("このカードには現在アクションがありません");
                break;
        }

    }

    public void OnClickAttack()
    {
        if (isRested || !isFaceUp)
        {
            Debug.Log("🛑 攻撃できない状態（レスト or 裏）");
            return;
        }

        SetRest(true);
        Debug.Log($"{cardData.cardName} が攻撃を宣言しました");

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.RegisterAttacker(this);
        }
    }

    private void TrySelectAsGuard()
    {
        BattleManager.Instance?.TrySelectGuard(this);
    }
    //カード枠のハイライト
    public void SetHighlight(bool isActive)
    {
        isHighlighted = isActive;
        if (highlightFrame != null)
        {
            highlightFrame.SetActive(isActive);
        }
    }

    public void OnClickSelectEXCard()
    {
        if (!cardData.isUnit)
        {
            Debug.LogWarning("EX化対象として有効なカードではありません");
            return;
        }

        Debug.Log($"🟢 EX候補カード {cardData.cardName} を選択しました");
        EXManager.Instance.OnSelectEXCard(this.cardData); // ✅ CardViewが持っているCardDataを渡す！

    }
    public bool IsHighlighted()
    {
        return isHighlighted;
    }

    /*
     ここからレベルアップ処理
     */
    // レベルアップ権を持っているかどうか（EX or パートナー）
    public bool CanLevelUp()
    {
        return cardData.isEX || isPartner;
    }

    // レベルアップ実行処理
    public void LevelUp()
    {
        if (!CanLevelUp())
        {
            Debug.LogWarning($"{cardData.cardName} はレベルアップできません！");
            return;
        }

        currentLevel += 1;
        accumulatedAtkBoost += cardData.exLevelUpAtkBoost;
        accumulatedHpBoost += cardData.exLevelUpHpBoost;

        // ステータス再計算
        UpdatePowerText();
        UpdateHPText();

        // 表＆スタンドに戻す
        SetFaceUp(true);
        SetRest(false);

        // HPも最大まで回復
        currentHP = GetEffectiveSupport();
        UpdateHPText();

        Debug.Log($"✅ {cardData.cardName} がレベル{currentLevel}になりました！ 攻撃力+{accumulatedAtkBoost}, HP+{accumulatedHpBoost}");
        if (levelText != null) levelText.text = $"Lv{cardData.currentLevel}";

    }
    public void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = $"Lv{currentLevel}";
    }
    public void SetSelected(bool isSelected)
    {
        // 例：カードに色をつけるなどの見た目変更
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            image.color = isSelected ? Color.yellow : Color.white;
    }

    public void SetSelectable(bool value)
    {
        isSelectable = value;

        if (cardData != null)
            Debug.Log($"SetSelectable({value}) → {cardData.cardName}");
        else
            Debug.LogWarning($"SetSelectable({value}) → cardData が null");

        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = value ? new Color(1f, 1f, 0.8f) : Color.white;
        }
    }

    public bool IsEXUnit()
    {
        return cardData != null && cardData.isEX;
    }
    public void DestroyFromField()
    {
        // Slot情報を消す処理も必要ならここで
        Destroy(gameObject);
    }
    public bool IsSelectable()
    {
        return isSelectable;
    }

}
