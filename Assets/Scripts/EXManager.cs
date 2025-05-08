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

    public Transform zyogaizone;

    // EX化 or レベルアップを選ぶパネル
    public UnityEngine.UI.Button exButton;
    public UnityEngine.UI.Button levelUpButton;
    // 今選んでいるEXカードを一時記憶
    private CardData pendingEXCard;

    private CardView selectedLevelUpMaterial; // 素材カード
    private CardData selectedLevelUpEX;       // EXカード
    public enum MaterialUseMode { None, EX, LevelUp }
    public MaterialUseMode materialMode = MaterialUseMode.None;

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
        if (exPanel == null)
        {
            Debug.LogWarning("exPanelがアタッチされていません");
            return;
        }

        bool isActive = exPanel.gameObject.activeSelf; // ← ここ重要！

        if (isActive)
        {
            // 開いてたら閉じる
            exPanel.gameObject.SetActive(false); // ← ここも重要！
            SetInstructionVisible(false); // 中央メッセージも非表示
            Debug.Log("EXリストパネルを閉じました");
        }
        else
        {
            // 閉じてたら開く
            ShowEXList();
            SetInstructionVisible(true);
            UpdateInstruction("EXユニットを選んでください");
            Debug.Log("EXリストパネルを開きました");
        }
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

        // EXユニットがすでにフィールドにいるなら、レベルアップに進む
        if (FieldHasSameEX(selectedEXCard))
        {
            Debug.Log("✅ フィールドに同じEXカードがいるためレベルアップに進みます");
            StartEXLevelUp(selectedEXCard);
            return;
        }

        // それ以外は従来のEX召喚処理へ
        HighlightValidBaseSlots();
        HighlightRequiredMaterials(selectedEXCard);
        UpdateInstruction("出す場所を選んでください");
    }


    public void OnClickSlotForEX(FieldSlot slot)
    {
        Debug.Log("OnClickSlotForEXに入った");
        Debug.Log($"[DEBUG] materialMode = {materialMode}, selectedMaterialCard = {selectedMaterialCard?.cardData.cardName}");


        if (selectedEXCard == null)
        {
            Debug.LogWarning("EXカードが選択されていません");
            return;
        }

        // ✅ レベルアップモードの優先処理（素材カードありき）
        if (materialMode == MaterialUseMode.LevelUp && selectedMaterialCard != null)
        {
            Debug.Log("🟢 レベルアップ処理に入ります");
            TryLevelUp(slot, selectedMaterialCard);
            CleanupEXProcess();
            return;
        }

        // 素材カードが未選択なら
        if (selectedMaterialCard == null)
        {
            if (selectedEXCard.exType == EXType.AB型)
            {
                HighlightMaterialCards(selectedEXCard);
                materialMode = MaterialUseMode.EX; // ★ これが絶対に必要！
                UpdateInstruction("素材カードを選んでください");
            }
            else if (selectedEXCard.exType == EXType.C型)
            {
                OpenDiscardPanelForC();
                UpdateInstruction("墓地から素材カードを選んでください");
            }
            return;
        }

        // EX化処理
        if (materialMode == MaterialUseMode.EX)
        {
            if (selectedEXCard.exType == EXType.AB型)
                TryEXSummonAB(selectedEXCard, slot);
            else if (selectedEXCard.exType == EXType.C型)
                TryEXSummonC(selectedEXCard, slot);
        }

        CleanupEXProcess(); // クリーンアップ共通関数
    }

    private void CleanupEXProcess()
    {
        exPanel.gameObject.SetActive(false);
        foreach (var s in fieldSlots) s.SetHighlight(false);
        SetInstructionVisible(false);
        materialMode = MaterialUseMode.None;
        selectedMaterialCard = null;
        selectedEXCard = null;
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
                if (view != null && view.cardData != null)
                {
                    if (view.cardData.cardName.Contains(needed))
                    {
                        view.SetHighlight(true);
                    }
                    else
                    {
                        view.SetHighlight(false);
                    }
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
        if (isAonField) expected = baseB;
        else if (isBonField) expected = baseA;

        if (expected == null || !card.cardData.cardName.Contains(expected))
        {
            Debug.LogWarning($"❌ {card.cardData.cardName} は素材として無効です（期待: {expected}）");
            return;
        }

        selectedMaterialCard = card;
        materialMode = MaterialUseMode.EX;
        Debug.Log($"✅ EX素材カードとして {card.cardData.cardName} を選択しました");

        HighlightValidBaseSlots();
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
        // まず墓地のカードだけハイライト
        foreach (Transform child in DiscardManager.Instance.discardZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null &&
                view.cardData.cardName.Contains(selectedEXCard.exBaseA))
            {
                view.SetHighlight(true);
                Debug.Log($"🔵 ハイライト: {view.cardData.cardName}");
            }
            else if (view != null)
            {
                view.SetHighlight(false);
            }
        }

        // 除外ゾーンのカード全部ハイライト解除
        foreach (Transform child in DiscardManager.Instance.banishContent)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null)
            {
                view.SetHighlight(false);
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
            materialMode = MaterialUseMode.EX; // 🔧 これを追加！
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



    private void StartEXSummon(CardData exCard)
    {
        selectedEXCard = exCard;

        // ベースカード（素材）をハイライト
        HighlightValidBaseSlots();

        // プレイヤーに指示
        UpdateInstruction("出す場所を選んでください");
    }
    // フィールドに同じEXがいるかチェック
    private bool FieldHasSameEX(CardData exCard)
    {
        foreach (var slot in fieldSlots)
        {
            if (slot.currentCard != null && slot.currentCard.cardData == exCard)
            {
                return true;
            }
        }
        return false;
    }


    // フィールド上の対象ユニットにレベルアップ処理を行う
    public void TryLevelUp(FieldSlot targetSlot, CardView materialCard)
    {
        Debug.Log("TryLevelUpに入った");
        Debug.Log($"🟢 TryLevelUp called on {targetSlot.currentCard?.cardData.cardName}");
        Debug.Log($"🧪 TryLevelUp対象: {materialCard.cardData.cardName}, isEX: {materialCard.cardData.isEX}");

        if (targetSlot == null || targetSlot.currentCard == null)
        {
            Debug.LogWarning("❌ 対象スロットまたはユニットが無効です");
            return;
        }

        var unitView = targetSlot.currentCard;

        if (!unitView.cardData.isEX)
        {
            Debug.LogWarning("❌ このユニットはEXユニットではありません");
            return;
        }

        if (!HandManager.Instance.RemoveCard(materialCard))
        {
            Debug.LogWarning("❌ 手札に素材カードがありません");
            return;
        }

        DiscardManager.Instance.AddToDiscard(materialCard.cardData);

        unitView.currentLevel += 1;

        unitView.accumulatedAtkBoost += unitView.cardData.exLevelUpAtkBoost;
        unitView.accumulatedHpBoost += unitView.cardData.exLevelUpHpBoost;
        unitView.power += unitView.cardData.exLevelUpAtkBoost;
        unitView.permSupportBoost += unitView.cardData.exLevelUpHpBoost;

        unitView.InitHP(); // permSupportBoostを考慮してhpMax再設定
        unitView.SetRest(false);
        unitView.SetFaceUp(true);
        unitView.UpdatePowerText();
        unitView.UpdateHPText();
        unitView.UpdateLevelText();

        Debug.Log($"✅ {unitView.cardData.cardName} がレベル {unitView.currentLevel} にレベルアップしました");

        selectedMaterialCard = null;
        selectedEXCard = null;
        SetInstructionVisible(false);
    }


    private void StartEXLevelUp(CardData exCard)
    {
        Debug.Log($"EXレベルアップモードに進みます: {exCard.cardName}");
        selectedEXCard = exCard;
        selectedLevelUpEX = exCard;
        materialMode = MaterialUseMode.LevelUp;

        // C型かAB型かで処理を分ける
        if (exCard.exType == EXType.C型)
        {
            string baseName = exCard.exBaseA;
            foreach (Transform child in HandManager.Instance.handZone)
            {
                CardView view = child.GetComponent<CardView>();
                if (view != null && view.cardData != null)
                {
                    view.SetHighlight(view.cardData.cardName.Contains(baseName));
                }
            }
        }
        else // AB型
        {
            string nameA = exCard.exBaseA;
            string nameB = exCard.exBaseB;
            foreach (Transform child in HandManager.Instance.handZone)
            {
                CardView view = child.GetComponent<CardView>();
                if (view != null && view.cardData != null)
                {
                    view.SetHighlight(view.cardData.cardName.Contains(nameA) || view.cardData.cardName.Contains(nameB));
                }
            }
        }

        UpdateInstruction("レベルアップ素材カードを手札から選んでください");
    }




    private void HighlightLevelUpMaterials(CardData exCard)
    {
        // 手札のカードを一旦すべて非ハイライト
        foreach (Transform child in HandManager.Instance.handZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null) view.SetHighlight(false);
        }

        // exBaseA または exBaseB を含むカードをハイライト
        foreach (Transform child in HandManager.Instance.handZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null)
            {
                if (view.cardData.cardName.Contains(exCard.exBaseA) ||
                    view.cardData.cardName.Contains(exCard.exBaseB))
                {
                    view.SetHighlight(true);
                    Debug.Log($"🔶 素材候補: {view.cardData.cardName}");
                }
            }
        }
    }
    public void OnClickLevelUpMaterialCard(CardView card)
    {
        if (materialMode != MaterialUseMode.LevelUp)
        {
            Debug.LogWarning("レベルアップ素材選択モードではありません");
            return;
        }

        selectedMaterialCard = card;
        Debug.Log($"🟢 EXレベルアップ素材カードを選択しました: {card.cardData.cardName}");

        // フィールド上の対象EXユニットをハイライト
        HighlightLevelUpTargets(selectedEXCard);

        UpdateInstruction("レベルアップさせたいユニットを選んでください");
    }


    public void HighlightFieldEXUnitsForLevelUp()
    {
        if (selectedEXCard == null || selectedMaterialCard == null)
        {
            Debug.LogWarning("❌ EXカードまたは素材カードが未選択です");
            return;
        }

        string baseA = selectedEXCard.exBaseA;
        string baseB = selectedEXCard.exBaseB;
        string materialName = selectedMaterialCard.cardData.cardName;

        foreach (var slot in fieldSlots)
        {
            if (slot.currentCard == null) continue;

            var view = slot.currentCard;

            // EXカードで、かつ同じカードで、かつまだレベルアップ可能なやつだけ
            if (view.cardData == selectedEXCard && view.CanLevelUp())
            {
                if (materialName.Contains(baseA) || materialName.Contains(baseB))
                {
                    slot.SetHighlight(true);
                }
            }
        }
    }

    public bool IsWaitingForLevelUpMaterial()
    {
        return materialMode == MaterialUseMode.LevelUp;
    }


    public void OnSelectLevelUpMaterial(CardView card)
    {
        Debug.Log($"🟢 素材カード選択完了: {card.cardData.cardName}");

        // C型かAB型かで素材の条件を分ける
        if (selectedLevelUpEX.exType == EXType.C型)
        {
            // 手札のカード名がEXカードのベース名（＝同名）を含むか
            string baseName = selectedLevelUpEX.exBaseA;
            if (card.cardData.cardName.Contains(baseName))
            {
                selectedMaterialCard = card;
                Debug.Log($"[DEBUG] selectedMaterialCard now set to: {selectedMaterialCard.cardData.cardName}");
                HighlightLevelUpTargets(selectedLevelUpEX);
                UpdateInstruction("レベルアップさせるEXユニットを選んでください");
            }
            else
            {
                Debug.LogWarning($"❌ {card.cardData.cardName} はC型の素材として無効です（必要: {baseName}）");
            }
        }
        else
        {
            // AB型
            string baseA = selectedLevelUpEX.exBaseA;
            string baseB = selectedLevelUpEX.exBaseB;

            if (card.cardData.cardName.Contains(baseA) || card.cardData.cardName.Contains(baseB))
            {
                selectedMaterialCard = card;
                Debug.Log($"[DEBUG] selectedMaterialCard now set to: {selectedMaterialCard.cardData.cardName}");
                HighlightLevelUpTargets(selectedLevelUpEX);
                UpdateInstruction("レベルアップさせるEXユニットを選んでください");
            }
            else
            {
                Debug.LogWarning($"❌ {card.cardData.cardName} は素材として無効です（必要: {baseA} or {baseB}）");
            }
        }
    }


    public bool IsWaitingForLevelUpTarget()
    {
        return selectedLevelUpMaterial != null && selectedLevelUpEX != null;
    }
    public CardView GetSelectedLevelUpMaterial()
    {
        return selectedMaterialCard;
    }
    public void OnSelectLevelUpTarget(FieldSlot slot)
    {
        if (slot.currentCard == null || slot.currentCard.cardData != selectedLevelUpEX)
        {
            Debug.LogWarning("❌ このスロットには対象のEXユニットがいません");
            return;
        }

        // 実行
        TryLevelUp(slot, selectedLevelUpMaterial);

        // 後片付け
        selectedLevelUpMaterial = null;
        selectedLevelUpEX = null;
    }
    private void HighlightLevelUpTargets(CardData exCard)
    {
        foreach (var slot in fieldSlots)
        {
            if (slot.currentCard != null &&
                slot.currentCard.cardData == exCard)
            {
                slot.SetHighlight(true);
            }
            else
            {
                slot.SetHighlight(false);
            }
        }
    }


}

