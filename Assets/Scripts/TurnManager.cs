using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static BattleManager;
using UnityEngine.UI;

public enum TurnPhase { Draw, Main, Battle, End }

public class TurnManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI phaseButtonLabel;
    [SerializeField] private CardData partnerCardData;
    [SerializeField] private FieldSlot partnerSlot;
    [SerializeField] private GameObject unitCardPrefab;
    public Button advanceButton;
    public static TurnManager Instance;

    public TurnPhase currentPhase = TurnPhase.Draw;
    public bool isPlayerTurn = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    private void Start()
    {
        // ① パートナー配置
        GameObject partnerGO = Instantiate(unitCardPrefab, partnerSlot.cardAnchor);
        CardView view = partnerGO.GetComponent<CardView>();
        view.SetCard(partnerCardData, true);
        partnerSlot.currentCard = view;

        // ② 通常のターン開始処理
        StartTurn();
    }

    public void StartTurn()
    {
        Debug.Log(" ターン開始（自分）");
        currentPhase = TurnPhase.Draw;
        StartDrawPhase();
    }

    private void StartDrawPhase()
    {
        Debug.Log(" ドローフェイズ：1枚ドロー");

        HandManager.Instance.DrawCard(1); 

        NextPhase();
    }

    public void NextPhase()
    {
        Debug.Log($" フェーズ遷移: {currentPhase} → 次へ");
        switch (currentPhase)
        {
            case TurnPhase.Draw:
                currentPhase = TurnPhase.Main;
                StartMainPhase();
                break;
            case TurnPhase.Main:
                currentPhase = TurnPhase.Battle;
                BattleManager.Instance.StartBattlePhase(); // 既存のバトル処理を呼び出す
                break;
            case TurnPhase.Battle:
                currentPhase = TurnPhase.End;
                StartEndPhase();
                break;
            case TurnPhase.End:
                EndTurn();
                break;
        }
    }

    private void StartMainPhase()
    {
        Debug.Log(" メインフェイズ開始");

        // カウントリセット（召喚1回, EX化1回, LvUP1回）
        ActionLimiter.Instance.ResetMainPhaseLimits();

        // メインイベント解放など、必要ならここに追加！
    }

    private void StartEndPhase()
    {
        Debug.Log(" エンドフェイズ開始");

        // 完了時に EndTurn を呼ぶコールバック方式に変更
        FieldManager.Instance.ResolveEndPhase(() =>
        {
            EndTurn(); // 選択完了後にターン終了
        });
    }

    private void EndTurn()
    {
        Debug.Log(" ターンエンド → 次のプレイヤーへ");

        // ターン終了処理（裏→表や一時効果リセットなど）をユニットに通知
        FieldManager.Instance.EndTurnResetAll();

        // 次のターン開始（今は単一プレイヤー用、後に相手ターン追加可能）
        StartTurn();
    }
    //ターンエンドボタン
    public void OnClickEndMainPhase()
    {
        if (TurnManager.Instance.currentPhase == TurnPhase.Main)
        {
            TurnManager.Instance.NextPhase(); // → Battleフェイズに進む
        }
    }
    public void OnClickEndBattle()
    {
        if (TurnManager.Instance.currentPhase == TurnPhase.Battle)
        {
            TurnManager.Instance.NextPhase(); // → endフェイズに進む
        }
    }
    

    public void UpdatePhaseButtonLabel()
    {
        switch (currentPhase)
        {
            case TurnPhase.Main:
                phaseButtonLabel.text = "バトルフェイズへ";
                advanceButton.interactable = true;
                break;
            case TurnPhase.Battle:
                if (IsBattleBusy())
                {
                    phaseButtonLabel.text = "（処理中）";
                    advanceButton.interactable = false;
                }
                else
                {
                    phaseButtonLabel.text = "ターン終了";
                    advanceButton.interactable = true;
                }
                break;
            case TurnPhase.End:
                phaseButtonLabel.text = "待機中...";
                advanceButton.interactable = false;
                break;
        }
    }

    private bool IsBattleBusy()
    {
        var p = BattleManager.Instance.currentPhase;
        return p == BattlePhase.EffectPhase1 ||
               p == BattlePhase.EffectPhase2 ||
               p == BattlePhase.EffectPhase3 ||
               p == BattlePhase.GuardDeclaration ||
               p == BattlePhase.ResolveBattle;
    }

    public void OnClickAdvancePhase()
    {
        if (currentPhase == TurnPhase.Main)
        {
            OnClickEndMainPhase();
        }
        else if (currentPhase == TurnPhase.Battle)
        {
            //  バトル中でも「効果フェーズ」や「防御選択中」は無効にする
            var battlePhase = BattleManager.Instance.currentPhase;
            if (battlePhase == BattlePhase.EffectPhase1 ||
                battlePhase == BattlePhase.EffectPhase2 ||
                battlePhase == BattlePhase.EffectPhase3 ||
                battlePhase == BattlePhase.GuardDeclaration ||
                battlePhase == BattlePhase.ResolveBattle)
            {
                Debug.Log(" 今はフェーズ移行できません（バトル中の効果フェーズなど）");
                return;
            }

            //  攻撃 or 攻撃終了直後ならOK
            OnClickEndBattle();
        }
        else if (currentPhase == TurnPhase.End)
        {
            Debug.Log(" 既にエンドフェイズです");
        }
    }
}
