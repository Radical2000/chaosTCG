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

    public int maxHP;
    public int currentHP;

    public GameObject highlightFrame;
    private bool isHighlighted;

    [Header("UI参照")]
    public Image cardImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI hpText;
    public GameObject backside;
    public TextMeshProUGUI descriptionText;

    public CardData cardData;

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

        if (nameText != null) nameText.text = data.cardName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (powerText != null) powerText.text = GetCurrentPower().ToString();
        if (hpText != null) hpText.text = $"{currentHP}/{maxHP}";
        if (cardImage != null && data.cardImage != null) cardImage.sprite = data.cardImage;
        if (backside != null) backside.SetActive(!faceUp);
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
            powerText.text = GetCurrentPower().ToString();
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
        Debug.Log($"🧪 cardName: {cardData.cardName}, isUnit: {cardData.isUnit}, parent: {transform.parent.name}");
        Debug.Log($"🟡 OnClickActionButton 実行！clickMode = {clickMode}");

        // EX用素材カード選択モード
        if (transform.parent.name == "PlayerHand" )
        {
            if (EXManager.Instance.HasSelectedEXCard())
            {
               EXManager.Instance.OnClickMaterialCard(this);
                return;
            }
            else
            {
                Debug.Log("EXManager.Instance.HasSelectedEXCard()でない");
            }
            
        }

        if (transform.parent.name == "EXUnitPanel")
        {
            Debug.Log($"🔷 EXカード {cardData.cardName} を選択しました");

            EXManager.Instance.selectedEXCard = cardData;

            if (EXManager.Instance.selectedEXCard != null)
                Debug.Log($"✅ セット成功: {EXManager.Instance.selectedEXCard.cardName}");

            EXManager.Instance.HighlightValidBaseSlots();

            return;
        }
        // ▼ EX C型用：墓地素材クリック
        if (transform.parent != null && transform.parent.name.Contains("Discard") && EXManager.Instance.HasSelectedEXCard())
        {
           // EXManager.Instance.OnClickMaterialCardFromDiscard(this);
            return;
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

        // 🟢 ユニット召喚（カードクリックによる通常召喚）
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
  
}
