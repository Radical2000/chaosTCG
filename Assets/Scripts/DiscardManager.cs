using System.Collections.Generic;
using UnityEngine;

public class DiscardManager : MonoBehaviour
{
    public static DiscardManager Instance;
    private void Awake()
    {
        Instance = this;
    }

    public List<CardData> playerDiscard = new List<CardData>();

    public HandManager handManager;
    public CardPlayManager fieldManager;
    public DiscardManager banishManager; // または BanishManager に分けてもOK
    public DiscardUIController uiController; // Inspectorに設定
    public Transform banishContent;

    private CardView selectedCardView;
    public Transform discardZone; // Content をここにアタッチ
    public Transform banishZone;
    public GameObject cardPrefab;
    public GameObject discardPanel;

    public void AddToDiscard(CardData cardData)
    {
        GameObject card = Instantiate(cardPrefab, discardZone);
        CardView view = card.GetComponent<CardView>();
        view.SetCard(cardData, true); // 裏表示なら false
    }

    public void MoveCardViewToDiscard(CardView view)
    {
        view.transform.SetParent(discardZone);
        view.transform.localScale = Vector3.one;
        view.transform.rotation = Quaternion.identity;

        Debug.Log($" 使用済みイベント {view.GetCardData().cardName} を控え室に移動しました");
    }

    public void SelectCard(CardView card)
    {
        selectedCardView = card;
        HighlightCard(card); // 選択中の見た目演出
        ShowOptionsUI();     // 「戻す/出す/除外」ボタンを表示
    }

    public void ReturnToHand()
    {
        if (selectedCardView == null) return;
        handManager.AddToHand(selectedCardView.GetCardData());
        Destroy(selectedCardView.gameObject);
        selectedCardView = null;
    }

    public void ReturnToField()
    {
        if (selectedCardView == null) return;
        fieldManager.SpawnCardToField(selectedCardView.GetCardData());
        Destroy(selectedCardView.gameObject);
        selectedCardView = null;
    }

    public void BanishCard()
    {
        if (selectedCardView == null) return;
        banishManager.AddToBanish(selectedCardView.GetCardData()); // 除外ゾーンに追加
        Destroy(selectedCardView.gameObject);
        selectedCardView = null;
    }
    private void HighlightCard(CardView card)
    {
        // TODO: 枠を光らせたり、色を変えたり（未実装なら空でOK）
        Debug.Log($"カード選択: {card.GetCardData().cardName}");
    }

    private void ShowOptionsUI()
    {
        // TODO: ボタンの表示など（あとでUIと接続）
        Debug.Log("操作UI表示");
        uiController.Show();
    }

    public void AddToBanish(CardData cardData)
    {
        GameObject card = Instantiate(cardPrefab, discardZone); // discardZone を除外ゾーンに設定
        CardView view = card.GetComponent<CardView>();
        view.SetCard(cardData, true);
    }
    public CardView FindCardByName(string name)
    {
        foreach (Transform child in discardZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.GetCardData() != null)
            {
                if (view.GetCardData().cardName.Contains(name))
                {
                    return view;
                }
            }
        }
        return null;
    }


    public void BanishCardData(CardData cardData)
    {
        GameObject card = Instantiate(cardPrefab, banishZone);
        CardView view = card.GetComponent<CardView>();
        view.SetCard(cardData, true);
    }

    public bool HasCardWithName(string name)
    {
        foreach (Transform child in discardZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.GetCardData().cardName.Contains(name))
            {
                return true;
            }
        }
        return false;
    }
    public void OpenDiscardPanel()
    {
        bool isActive = discardPanel.activeSelf;
        if (isActive)
        {
            // もし開いてたら閉じる
            discardPanel.SetActive(false);
            Debug.Log("墓地パネルを閉じました");
        }
        else
        {
            // もし閉じてたら開く
            discardPanel.SetActive(true);
            Debug.Log("墓地パネルを開きました");
            ClearAllBanishHighlights();
        }
    }

    public void CloseDiscardPanel()
    {
        if (discardPanel != null)
        {
            discardPanel.SetActive(false);
            Debug.Log(" 墓地パネルを閉じました");
        }
    }
    public void BanishCard(CardView view)
    {
        if (view == null) return;

        // 除外ゾーンへ移動
        view.transform.SetParent(banishZone);
        view.transform.localScale = Vector3.one;
        view.transform.rotation = Quaternion.identity;

        Debug.Log($" {view.cardData.cardName} を除外ゾーンに移動しました");
    }
    // 除外ゾーンのハイライトをすべて消す
    private void ClearAllBanishHighlights()
    {
        Debug.Log("ClearAllBanishHighlights()");
        foreach (Transform child in banishContent)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null)
            {
                view.SetHighlight(false);
            }
        }
    }
    public List<CardView> GetPlayerDiscardViews()
    {
        List<CardView> views = new List<CardView>();
        foreach (Transform child in discardZone)
        {
            var view = child.GetComponent<CardView>();
            if (view != null)
                views.Add(view);
        }
        return views;
    }

    public List<CardData> GetPlayerDiscard()
    {
        return new List<CardData>(playerDiscard); // playerDiscard は CardData の List として定義してある前提
    }
}
    
