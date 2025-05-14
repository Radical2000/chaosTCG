using UnityEngine;
using static CardView;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public DeckManager opponentDeck;

    private enum EffectTurn { Attacker, Defender }
    private EffectTurn currentEffectTurn = EffectTurn.Attacker;

    private bool attackerPassed = false;
    private bool defenderPassed = false;

    private CardView attackingCard;
    public GameObject defenseChoicePanel;
    private CardView currentAttacker;
    private CardView currentDefender;

    public Transform playerFieldZone;
    public Transform enemyFieldZone;
    private bool isWaitingForGuardSelection = false;


    private bool effect1AttackerPassed = false;
    private bool effect1DefenderPassed = false;
    private bool effect2AttackerPassed = false;
    private bool effect2DefenderPassed = false;
    private bool effect3AttackerPassed = false;
    private bool effect3DefenderPassed = false;

    public GameObject effectChoicePanel;

    public BattlePhase currentPhase = BattlePhase.None;
    public enum BattlePhase
    {
        None,
        BattleStart,
        EffectPhase1,
        AttackOrEnd,
        EffectPhase2,
        GuardDeclaration,
        EffectPhase3,
        ResolveBattle,
        TurnEnd
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartBattlePhase();
        }
    }

    public void StartBattlePhase()
    {
        currentPhase = BattlePhase.BattleStart;
        Debug.Log("バトルフェーズ開始");
        StartEffectPhase1();
    }

    public void StartEffectPhase1()
    {
        currentPhase = BattlePhase.EffectPhase1;
        effect1AttackerPassed = false;
        effect1DefenderPassed = false;
        effectChoicePanel.SetActive(true);
        Debug.Log("効果フェーズ1開始");
    }

    public void StartEffectPhase2()
    {
        currentPhase = BattlePhase.EffectPhase2;
        effect2AttackerPassed = false;
        effect2DefenderPassed = false;
        effectChoicePanel.SetActive(true);
        Debug.Log("効果フェーズ2開始（攻撃宣言後）");
    }

    public void StartEffectPhase3()
    {
        Debug.Log("効果フェーズ3開始");
        currentPhase = BattlePhase.EffectPhase3;
        effect3AttackerPassed = false;
        effect3DefenderPassed = false;
        effectChoicePanel.SetActive(true);
        Debug.Log("効果フェーズ3開始（防御宣言後）");
    }

    public void RegisterAttacker(CardView attacker)
    {
        Debug.Log($"RegisterAttacker 呼ばれた (BM: {this})");

        if (attacker == null)
        {
            Debug.LogError(" attacker に null が渡されてます！");
            return;
        }

        currentAttacker = attacker;

        var cardName = attacker.GetCardData()?.cardName ?? "null";
        Debug.Log($" 攻撃ユニット登録: {cardName}");

        //  効果フェーズ2開始（防御処理はここではやらない）
        StartEffectPhase2();
    }


    private void ShowDefenseChoice(CardView attacker)
    {
        Debug.Log("🛡️ 防御側の選択を開始します");
        Debug.Log($"enemyFieldZone のスロット数: {enemyFieldZone.childCount}");

        bool hasValidGuard = false;
        int totalSlotChecked = 0;

        foreach (Transform slotObj in enemyFieldZone)
        {
            totalSlotChecked++;

            FieldSlot slot = slotObj.GetComponent<FieldSlot>();
            if (slot == null)
            {
                Debug.LogWarning($"⚠️ Slot {slotObj.name} に FieldSlot がついていません！");
                continue;
            }

            if (slot.currentCard == null)
            {
                Debug.Log($"⬜ Slot {slotObj.name} にカードが入っていません");
                continue;
            }

            CardView view = slot.currentCard;

            Debug.Log($"🧪 ガード候補: {view.cardData.cardName}, isFaceUp={view.isFaceUp}, isRested={view.isRested}");

            if (view.isFaceUp && !view.isRested)
            {
                hasValidGuard = true;
                break;
            }
        }

        Debug.Log($"🔍 チェックしたスロット数: {totalSlotChecked}");

        if (!hasValidGuard)
        {
            Debug.Log("⚠️ 表向きガード可能なユニットがいない → 自動的にライフで受けます");
            OnClickTakeLifeDamage();
            return;
        }

        Debug.Log("✅ ガード可能なユニットあり → 選択を待ちます");
        defenseChoicePanel.SetActive(true);
    }





    public void OnClickGuard()
    {
        Debug.Log("ガード選択モード：有効ユニットをクリックしてください");
        isWaitingForGuardSelection = true;
        defenseChoicePanel.SetActive(false);
    }

    public void OnClickTakeLifeDamage()
    {
        Debug.Log("OnClickTakeLifeDamage 実行 (BM: " + this + ")");

        if (currentAttacker == null || currentAttacker.GetCardData() == null)
        {
            Debug.LogError("currentAttacker が未設定、または cardData がありません！");
            return;
        }

        Debug.Log("防御側：ライフで受ける選択 → ガードユニットなしとして処理継続");

        currentDefender = null; //  ガードユニットはなしと明示！

        defenseChoicePanel.SetActive(false);

        //  ライフ受けのあとでも効果③のイベント発動が可能
        StartEffectPhase3();
    }



    public void TrySelectGuard(CardView candidate)
    {
        if (!isWaitingForGuardSelection) return;

        // フィールドスロットに正しく存在しているかを確認
        bool isValid = false;

        foreach (Transform slot in enemyFieldZone)
        {
            FieldSlot fieldSlot = slot.GetComponent<FieldSlot>();
            if (fieldSlot != null && fieldSlot.currentCard == candidate)
            {
                if (candidate.isFaceUp && !candidate.isRested)
                {
                    currentDefender = candidate;
                    isWaitingForGuardSelection = false;

                    Debug.Log($" ガードユニットに {candidate.GetCardData().cardName} を指定しました");
                    StartEffectPhase3();
                    return;
                }
                else
                {
                    Debug.Log(" 選択されたユニットはガードできない状態です（裏 or レスト）");
                    return;
                }
            }
        }

        Debug.LogWarning(" ガード選択ユニットが敵フィールド上に存在しません！");
    }


    public void OnClickAttackerPass()
    {
        switch (currentPhase)
        {
            case BattlePhase.EffectPhase1:
                OnAttackerEffectPhase1Pass();
                break;
            case BattlePhase.EffectPhase2:
                OnAttackerEffectPhase2Pass();
                break;
            case BattlePhase.EffectPhase3:
                OnAttackerEffectPhase3Pass();
                break;
        }
    }

    public void OnClickDefenderPass()
    {
        switch (currentPhase)
        {
            case BattlePhase.EffectPhase1:
                OnDefenderEffectPhase1Pass();
                break;
            case BattlePhase.EffectPhase2:
                OnDefenderEffectPhase2Pass();
                break;
            case BattlePhase.EffectPhase3:
                OnDefenderEffectPhase3Pass();
                break;
        }
    }

    public void OnAttackerEffectPhase1Pass()
    {
        effect1AttackerPassed = true;
        Debug.Log("EffectPhase1：攻撃側がパスしました");
        CheckEffectPhase1Pass();
    }

    public void OnDefenderEffectPhase1Pass()
    {
        effect1DefenderPassed = true;
        Debug.Log("EffectPhase1：防御側がパスしました");
        CheckEffectPhase1Pass();
    }

    private void CheckEffectPhase1Pass()
    {
        if (effect1AttackerPassed && effect1DefenderPassed)
        {
            Debug.Log("EffectPhase1：両者がパス → 攻撃宣言フェーズへ");
            effectChoicePanel.SetActive(false);
            StartAttackPhase();
        }
    }

    public void OnAttackerEffectPhase2Pass()
    {
        effect2AttackerPassed = true;
        Debug.Log("EffectPhase2：攻撃側がパスしました");
        CheckEffectPhase2Pass();
    }

    public void OnDefenderEffectPhase2Pass()
    {
        effect2DefenderPassed = true;
        Debug.Log("EffectPhase2：防御側がパスしました");
        CheckEffectPhase2Pass();
    }

    private void CheckEffectPhase2Pass()
    {
        if (effect2AttackerPassed && effect2DefenderPassed)
        {
            Debug.Log("EffectPhase2：両者がパス → ガード宣言フェーズへ");

            effectChoicePanel.SetActive(false);
            currentPhase = BattlePhase.GuardDeclaration;

            //  ガード選択モードON
            SetAllCardModes(CardClickMode.GuardSelect, enemyFieldZone);

            // ここで防御処理を呼ぶ！（重要）
            ShowDefenseChoice(currentAttacker);
        }
    }


    public void OnAttackerEffectPhase3Pass()
    {
        effect3AttackerPassed = true;
        Debug.Log("EffectPhase3：攻撃側がパスしました");
        CheckEffectPhase3Pass();
    }

    public void OnDefenderEffectPhase3Pass()
    {
        effect3DefenderPassed = true;
        Debug.Log("EffectPhase3：防御側がパスしました");
        CheckEffectPhase3Pass();
    }

    private void CheckEffectPhase3Pass()
    {
        if (effect3AttackerPassed && effect3DefenderPassed)
        {
            Debug.Log("EffectPhase3：両者がパス → バトル解決へ");
            effectChoicePanel.SetActive(false);
            ResolveBattle();
        }
    }

    public void ResolveBattle()
    {
        Debug.Log("▶ バトル解決開始");

        CardData attacker = currentAttacker.GetCardData();
        CardData defender = currentDefender?.GetCardData();

        bool defenderExists = currentDefender != null;
        bool attackerHasPenetrate = currentAttacker.GetEffectivePenetrate();
        int atkPower = attacker.power;
        int atkSupport = attacker.support;
        int defPower = defender?.power ?? 0;
        int defSupport = defender?.support ?? 0;

        Debug.Log($"[DEBUG] ResolveBattle 開始");
        Debug.Log($"[DEBUG] 攻撃側カード: {attacker.cardName}");
        Debug.Log($"[DEBUG] hasPenetrate={attacker.hasPenetrate}, tempPenetrate={currentAttacker.tempHasPenetrate}, effective={attackerHasPenetrate}");

        //  ライフで受ける場合
        if (!defenderExists)
        {
            Debug.Log("🩸 防御ユニットがいない → ライフで直接ダメージ");
            opponentDeck.TakeDamage(atkPower);
            Debug.Log("✅ バトル解決完了（ライフ受け）");
            return;
        }

        bool defenderIsFaceUp = currentDefender.isFaceUp;

        //  貫通チェックはユニット裏返す前にやる！
        if (!defenderIsFaceUp)
        {
            if (attackerHasPenetrate)
            {
                Debug.Log("🗡️ 防御が裏 → 貫通持ちなので本体にダメージ！");
                opponentDeck.TakeDamage(atkPower);
            }
            else
            {
                Debug.Log("🛡️ 防御が裏 → 攻撃は空振り！");
            }
            return;
        }

        //  通常戦闘処理（先にダメージ処理）
        currentDefender.TakeDamage(atkPower);
        currentAttacker.TakeDamage(defPower);

        // ✅ ダメージ後に貫通判定
        bool defenderDied = !currentDefender.isFaceUp;
        if (defenderDied && attackerHasPenetrate)
        {
            int pierceDamage = Mathf.Max(0,  - currentDefender.currentHP);
            Debug.Log($"🩸 防御が戦闘で裏 → 本体に {pierceDamage} 貫通ダメージ！（ DEF HP: {currentDefender.currentHP}）");
            opponentDeck.TakeDamage(pierceDamage);
        }


        //  攻撃側は攻撃宣言時に必ずレスト（戦闘後）
        currentAttacker.SetRest(true);
        //  攻撃ユニットが裏になった場合 → HPを0に
        if (!currentAttacker.isFaceUp)
        {
            currentAttacker.currentHP = 0;
            currentAttacker.UpdateHPText(); // 表示更新
        }
        //  防御ユニットが裏になった場合 → HPを0に
        if (!currentDefender.isFaceUp)
        {
            currentDefender.currentHP = 0;
            currentDefender.UpdateHPText(); // 表示更新
        }


        Debug.Log("✅ バトル解決完了（通常戦闘）");
    }




    public void StartAttackPhase()
    {
        currentPhase = BattlePhase.AttackOrEnd;
        SetAllCardModes(CardClickMode.Attack, playerFieldZone);
        Debug.Log("攻撃フェーズ開始：clickMode = Attack に設定しました");
    }


    public void SetAllCardModes(CardView.CardClickMode mode)
    {
        SetAllCardModes(mode, playerFieldZone);
        SetAllCardModes(mode, enemyFieldZone);
    }

    public void SetAllCardModes(CardView.CardClickMode mode, Transform fieldZone)
    {
        foreach (Transform slot in fieldZone)
        {
            FieldSlot fieldSlot = slot.GetComponent<FieldSlot>();
            if (fieldSlot != null && fieldSlot.currentCard != null)
            {
                fieldSlot.currentCard.clickMode = mode;
            }
        }
    }

    //エフェクトフェイズ交互パスの実装
    private void SwitchEffectTurn()
    {
        if (currentEffectTurn == EffectTurn.Attacker)
        {
            currentEffectTurn = EffectTurn.Defender;
            Debug.Log("🟦 防御側の効果使用ターンに切り替え");
        }
        else
        {
            currentEffectTurn = EffectTurn.Attacker;
            Debug.Log("🟥 攻撃側の効果使用ターンに切り替え");
        }

        // モードを再設定（UseEvent / None 切り替えなどもここに入れる）
        UpdateEffectClickModes();
    }

    public void OnClickPass()
    {
        if (currentPhase == BattlePhase.EffectPhase1)
        {
            if (currentEffectTurn == EffectTurn.Attacker)
            {
                attackerPassed = true;
            }
            else
            {
                defenderPassed = true;
            }

            if (attackerPassed && defenderPassed)
            {
                Debug.Log("両者パス → 攻撃宣言フェーズへ");
                currentPhase = BattlePhase.AttackOrEnd;
                effectChoicePanel.SetActive(false);
                SetAllCardModes(CardView.CardClickMode.Attack, playerFieldZone);
            }
            else
            {
                SwitchEffectTurn(); // パスしても相手へターン交代
            }
        }
    }
    //イベント発動完了時に行動権を交代
    public void OnEffectUsed()
    {
        if (currentEffectTurn == EffectTurn.Attacker)
            attackerPassed = false;
        else
            defenderPassed = false;

        SwitchEffectTurn();
    }

    private void UpdateEffectClickModes()
    {
        // 一旦すべてのカードのクリックモードをNoneにする
        SetAllCardModes(CardView.CardClickMode.None, playerFieldZone);
        SetAllCardModes(CardView.CardClickMode.None, enemyFieldZone);

        // 攻撃側のターンなら → プレイヤーのイベントカードをクリック可能に
        if (currentEffectTurn == EffectTurn.Attacker)
        {
            foreach (Transform cardObj in HandManager.Instance.handZone)
            {
                CardView view = cardObj.GetComponent<CardView>();
                if (view != null && !view.cardData.isUnit)
                {
                    view.clickMode = CardView.CardClickMode.UseEvent;
                }
            }
        }
        // 防御側のターン（今は何もしない or 防御イベントがあるなら同様に設定）
    }
    //ターン終了時にステータスリセット用
    public void ResetAllTempPower()
    {
        foreach (Transform card in playerFieldZone)
        {
            CardView view = card.GetComponent<CardView>();
            if (view != null) view.tempPowerBoost = 0;
        }
    }
    public void OnClickEndBattle()
    {
        Debug.Log("🛑 攻撃せずにバトルを終了 → エンドフェイズへ");
        TurnManager.Instance.NextPhase(); // → End へ
    }
}
