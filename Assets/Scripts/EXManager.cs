using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CardData;

public class EXManager : MonoBehaviour
{
    public static EXManager Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public TextMeshProUGUI intructionText;
    public List<FieldSlot> fieldSlots;
    public CardData exCardToTest;
    public CardData cExCard;
    public Transform exPanel;
    public GameObject cardPrefab;
    public List<CardData> exCardList;

    public CardData selectedEXCard;
    public CardView selectedMaterialCard;

    public bool CanEXFromAB(CardData exCard)
    {
        foreach (var slot in fieldSlots)
        {
            if (slot.currentCard == null) continue;

            // Aがフィールドにいる
            if (slot.currentCard.cardData.cardName.Contains(exCard.exBaseA))
            {
                // Bが手札にいればOK
                return HandManager.Instance.HasCardWithName(exCard.exBaseB);
            }
            // Bがフィールドにいる
            else if (slot.currentCard.cardData.cardName.Contains(exCard.exBaseB))
            {
                // Aが手札にいればOK
                return HandManager.Instance.HasCardWithName(exCard.exBaseA);
            }
        }

        return false;
    }


    public bool CanEXFromC(CardData exCard)
    {
        bool hasBase = fieldSlots.Any(slot =>
            slot.currentCard != null && slot.currentCard.cardData.cardName.Contains(exCard.exBaseA));

        var targetInDiscard = DiscardManager.Instance.FindCardByName(exCard.exBaseA);

        return hasBase && targetInDiscard != null;
    }
    public void TryEXSummonAB(CardData exCard, FieldSlot targetSlot)
    {
        if (!ActionLimiter.Instance.CanEX()) return;
        if (selectedMaterialCard == null)
        {
            Debug.LogWarning("❌ 素材カードが選択されていません");
            return;
        }

        var baseCard = targetSlot.currentCard;
        if (baseCard == null)
        {
            Debug.LogWarning("❌ フィールドにEXベースカードが見つかりません");
            return;
        }

        string baseName = exCard.exBaseA;
        string otherName = exCard.exBaseB;

        bool isBaseA = baseCard.cardData.cardName.Contains(baseName);
        string expectedMaterialName = isBaseA ? otherName : baseName;

        // 選ばれた素材が一致するか再確認
        if (!selectedMaterialCard.cardData.cardName.Contains(expectedMaterialName))
        {
            Debug.LogWarning($"❌ 選ばれた素材が一致しません（必要: {expectedMaterialName}）");
            return;
        }

        if (!HandManager.Instance.RemoveCardByName(expectedMaterialName))
        {
            Debug.LogWarning("❌ 手札に素材カードが存在しません（再確認）");
            return;
        }

        DiscardManager.Instance.AddToDiscard(selectedMaterialCard.cardData);
        ActionLimiter.Instance.UseEX();

        baseCard.SetCard(exCard);
        baseCard.SetFaceUp(true);
        baseCard.SetRest(false);
        baseCard.InitHP();

        Debug.Log($"✅ EXユニット {exCard.cardName} を展開！");

        // 後処理
        selectedEXCard = null;
        selectedMaterialCard = null;
        exPanel.gameObject.SetActive(false);

        // スロットと手札のハイライト解除
        foreach (var slot in fieldSlots) slot.SetHighlight(false);
        ClearAllHandHighlights();
    }



    public void TryEXSummonC(CardData exCard, FieldSlot targetSlot)
    {
        if (!ActionLimiter.Instance.CanEX()) return;
        if (selectedMaterialCard == null)
        {
            Debug.LogWarning("❌ 墓地素材カードが選ばれていません");
            return;
        }

        var baseCard = targetSlot.currentCard;
        if (baseCard == null) return;

        // --- ここで選んだ墓地カードを除外する！ ---
        DiscardManager.Instance.BanishCard(selectedMaterialCard);

        ActionLimiter.Instance.UseEX();

        // --- フィールドのベースカードをEXカードに上書きする ---
        baseCard.SetCard(exCard);
        baseCard.SetFaceUp(true);
        baseCard.SetRest(false);
        baseCard.InitHP();

        Debug.Log($"✅ C型EXユニット {exCard.cardName} を展開！");

        // 後処理
        selectedEXCard = null;
        selectedMaterialCard = null;
    }


    public void OnClickOpenEXList()
    {
        ShowEXList();
        SetInstructionVisible(true);
        UpdateInstruction("EXユニットを選んでください");
    }

    public void ShowEXList()
    {
        foreach (Transform child in exPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (var card in exCardList)
        {
            GameObject cardGO = Instantiate(cardPrefab, exPanel);
            CardView view = cardGO.GetComponent<CardView>();
            view.SetCard(card, true);

            bool canSummon = false;
            if (card.exType == EXType.AB型)
                canSummon = CanEXFromAB(card);
            else if (card.exType == EXType.C型)
                canSummon = CanEXFromC(card);

            view.SetHighlight(canSummon);

            Button btn = cardGO.GetComponent<Button>();
            if (btn != null)
            {
                CardData capturedCard = view.GetCardData();  // ✅ 正しい修正
                btn.onClick.AddListener(() => OnSelectEXCard(capturedCard));
            }
        }

        exPanel.gameObject.SetActive(true);
    }


    public void OnSelectEXCard(CardData selected)
    {
        selectedEXCard = selected;
        Debug.Log($"✅ EXカード選択完了：{selectedEXCard.cardName}");

        HighlightValidBaseSlots();
        HighlightRequiredMaterials(selectedEXCard);

        UpdateInstruction("出す場所を選んでください");
    }

    public void OnClickSlotForEX(FieldSlot slot)
    {
        if (selectedEXCard == null)
        {
            Debug.LogWarning("EXカードが選択されていません");
            return;
        }

        // --- 素材カードが未選択なら ---
        if (selectedMaterialCard == null)
        {
            if (selectedEXCard.exType == EXType.AB型)
            {
                HighlightMaterialCards(selectedEXCard);
                UpdateInstruction("素材カードを選んでください");
            }
            else if (selectedEXCard.exType == EXType.C型)
            {
                Debug.Log(" C型EX: 墓地パネルを開いて素材カードを選ばせます");
                OpenDiscardPanelForC(); // ここが違う！！
                UpdateInstruction("墓地から素材カードを選んでください");
            }
            return; // ← ここで止める（素材選び待ち）
        }

        // --- 素材カードが選ばれていたら、EX化する ---
        if (selectedEXCard.exType == EXType.AB型)
        {
            TryEXSummonAB(selectedEXCard, slot);
        }
        else if (selectedEXCard.exType == EXType.C型)
        {
            TryEXSummonC(selectedEXCard, slot);
        }

        // --- EX化後、後片付け ---
        exPanel.gameObject.SetActive(false);
        foreach (var s in fieldSlots)
        {
            s.SetHighlight(false);
        }
        SetInstructionVisible(false);
    }




    public void HighlightValidBaseSlots()
    {
        foreach (var slot in fieldSlots)
        {
            if (selectedEXCard.exType == EXType.AB型)
            {
                if (slot.currentCard != null &&
                    (slot.currentCard.cardData.cardName.Contains(selectedEXCard.exBaseA) ||
                     slot.currentCard.cardData.cardName.Contains(selectedEXCard.exBaseB)))
                {
                    slot.SetHighlight(true);
                }
            }
            else if (selectedEXCard.exType == EXType.C型)
            {
                if (slot.currentCard != null &&
                    slot.currentCard.cardData.cardName.Contains(selectedEXCard.exBaseA) &&
                    DiscardManager.Instance.HasCardWithName(selectedEXCard.exBaseA))
                {
                    slot.SetHighlight(true);
                }
            }
        }
    }

    public bool HasSelectedEXCard()
    {
        return selectedEXCard != null;
    }

    public void HighlightRequiredMaterials(CardData selectedEX)
    {
        // まず全カードのハイライトをOFF
        foreach (Transform child in HandManager.Instance.handZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null) view.SetHighlight(false);
        }

        foreach (Transform child in DiscardManager.Instance.discardZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null) view.SetHighlight(false);
        }

        // A&B型 → 手札のもう一方をハイライト
        if (selectedEX.exType == EXType.AB型)
        {
            string baseName = selectedEX.exBaseA;
            string otherName = selectedEX.exBaseB;

            // どちらがフィールドにいるかで、必要な素材を判断
            bool hasAonField = fieldSlots.Any(slot =>
                slot.currentCard != null && slot.currentCard.cardData.cardName.Contains(baseName));

            bool hasBonField = fieldSlots.Any(slot =>
                slot.currentCard != null && slot.currentCard.cardData.cardName.Contains(otherName));

            string needed = hasAonField ? otherName : (hasBonField ? baseName : null);

            Debug.Log($"🔎 ハイライト対象素材名: \"{needed}\"");

            if (needed == null)
            {
                Debug.LogWarning("フィールドにEXベース候補が存在しません");
                return;
            }

            foreach (Transform cardObj in HandManager.Instance.handZone)
            {
                CardView view = cardObj.GetComponent<CardView>();
                if (view != null && view.cardData.cardName.Contains(needed))
                {
                    view.SetHighlight(true);
                }
            }
        }

        // C型 → 墓地の同名カードをハイライト
        else if (selectedEX.exType == EXType.C型)
        {
            foreach (Transform cardObj in DiscardManager.Instance.discardZone)
            {
                CardView view = cardObj.GetComponent<CardView>();
                if (view != null && view.cardData.cardName.Contains(selectedEX.exBaseA))
                {
                    view.SetHighlight(true);
                }
            }
        }
    }



    public void OnClickMaterialCard(CardView card)
    {
        if (selectedEXCard == null) return;

        string baseA = selectedEXCard.exBaseA;
        string baseB = selectedEXCard.exBaseB;

        bool isAonField = fieldSlots.Any(slot =>
            slot.currentCard != null &&
            slot.currentCard.cardData.cardName.Contains(baseA));

        bool isBonField = fieldSlots.Any(slot =>
            slot.currentCard != null &&
            slot.currentCard.cardData.cardName.Contains(baseB));

        string expected = null;

        if (isAonField)
            expected = baseB;
        else if (isBonField)
            expected = baseA;

        if (expected == null)
        {
            Debug.LogWarning("❌ フィールドにEXベースカードが存在しません");
            return;
        }

        if (card.cardData.cardName.Contains(expected))
        {
            selectedMaterialCard = card;
            Debug.Log($"✅ 素材カードとして {card.cardData.cardName} を選択しました");

            HighlightValidBaseSlots(); // ベーススロットを再度光らせる
        }
        else
        {
            Debug.LogWarning($"❌ {card.cardData.cardName} は素材として無効です（期待: {expected}）");
        }
    }


    public void HighlightMaterialCards(CardData exCard)
    {
        foreach (Transform child in HandManager.Instance.handZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null) view.SetHighlight(false);
        }

        string baseName = exCard.exBaseA;
        string otherName = exCard.exBaseB;

        FieldSlot selectedSlot = fieldSlots.FirstOrDefault(slot => slot.IsHighlighted());
        if (selectedSlot == null || selectedSlot.currentCard == null) return;

        bool baseIsA = selectedSlot.currentCard.cardData.cardName.Contains(baseName);
        string requiredName = baseIsA ? otherName : baseName;

        Debug.Log($"🔎 ハイライト対象素材名: {requiredName}");

        foreach (Transform child in HandManager.Instance.handZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null &&
                view.cardData.cardName.Contains(requiredName))
            {
                view.SetHighlight(true);
            }
        }
    }
    //ハイライト消す関数
    private void ClearAllHandHighlights()
    {
        foreach (Transform cardObj in HandManager.Instance.handZone)
        {
            CardView view = cardObj.GetComponent<CardView>();
            if (view != null) view.SetHighlight(false);
        }
    }
    //中央Textのsetactive
    public void SetactiveInstruction()
    {

    }
    // 中央テキストの表示ON/OFF
    public void SetInstructionVisible(bool visible)
    {
        if (intructionText != null)
        {
            intructionText.gameObject.SetActive(visible);
        }
    }

    // 中央テキストの内容変更
    public void UpdateInstruction(string message)
    {
        if (intructionText != null)
        {
            intructionText.text = message;
        }
    }
    //墓地panelを開く用
    public void OpenDiscardPanelForC()
    {
        Debug.Log(" C型EX用に墓地パネルを開きます");

        // ここで墓地の素材カード(BaseA一致)をハイライトする
        HighlightDiscardBaseCards();

        // 墓地パネル自体を表示
        DiscardManager.Instance.OpenDiscardPanel();
    }

    public void HighlightDiscardBaseCards()
    {
        if (selectedEXCard == null)
        {
            Debug.LogWarning("❌ EXカードが選ばれていません");
            return;
        }

        string baseName = selectedEXCard.exBaseA;

        foreach (Transform child in DiscardManager.Instance.discardZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null)
            {
                if (view.cardData.cardName.Contains(baseName))
                {
                    view.SetHighlight(true);
                    Debug.Log($" ハイライト: {view.cardData.cardName}");
                }
                else
                {
                    view.SetHighlight(false);
                }
            }
        }
    }
    public void OnClickMaterialCardFromDiscard(CardView card)
    {
        if (selectedEXCard == null)
        {
            Debug.LogWarning("❌ EXカードが選ばれていません");
            return;
        }

        if (card == null || card.cardData == null)
        {
            Debug.LogWarning("❌ クリックされたカードが無効です");
            return;
        }

        string baseName = selectedEXCard.exBaseA;

        if (card.cardData.cardName.Contains(baseName))
        {
            selectedMaterialCard = card;
            Debug.Log($"✅ 墓地素材カード選択完了: {card.cardData.cardName}");

            // クリックしたら墓地パネルを閉じる！
            DiscardManager.Instance.CloseDiscardPanel();

            // スロット選択の待ち状態に戻る（再度スロットをクリックしてEX化へ）
            UpdateInstruction("出すスロットをもう一度選んでください");
        }
        else
        {
            Debug.LogWarning($"❌ {card.cardData.cardName} は素材として無効です（必要: {baseName}）");
        }
    }

}

