using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class HandManager : MonoBehaviour
{


    public static HandManager Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public Transform handZone; // 手札を並べる場所
    public GameObject unitCardPrefab;
    public GameObject eventCardPrefab;

    public CardData[] cardPool; // デッキ or カード一覧から引く

    public int startHandCount = 5;

    private List<CardData> handCards = new List<CardData>(); // ← 追加！

    public int HandCount => handCards.Count; // ← 追加！

    private void Start()
    {
        DrawStartingHand();
    }

    void DrawStartingHand()
    {
        for (int i = 0; i < startHandCount; i++)
        {
            int index = Random.Range(0, cardPool.Length);
            CardData drawnCard = cardPool[index];
            SpawnCardToHand(drawnCard);
        }
    }

    public void SpawnCardToHand(CardData data)
    {
        GameObject prefabToUse = data.isUnit ? unitCardPrefab : eventCardPrefab;

        GameObject cardGO = Instantiate(prefabToUse, handZone);
        CardView view = cardGO.GetComponent<CardView>();
        view.SetCard(data, true);
    }

    /// <summary>
    /// 指定枚数手札から捨てる（先頭から）
    /// </summary>
    public void DiscardUnitFromHand(int x)
    {
        List<CardView> unitCards = new List<CardView>();

        foreach (Transform child in handZone)
        {
            if (child == null) continue;

            var view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null && view.cardData.isUnit)
            {
                unitCards.Add(view);
            }
        }

        if (unitCards.Count < x)
        {
            Debug.LogWarning("手札にユニットカードが足りません！");
            return;
        }

        for (int i = 0; i < x; i++)
        {
            int index = Random.Range(0, unitCards.Count);
            var toDiscard = unitCards[index];
            unitCards.RemoveAt(index);

            DiscardManager.Instance.AddToDiscard(toDiscard.cardData);
            Destroy(toDiscard.gameObject);

            Debug.Log($" ユニット {toDiscard.cardData.cardName} を手札から捨てました");
        }
    }


    public int CountUnitCardsInHand()
    {
        if (handZone == null)
        {
            Debug.LogWarning(" handZone が設定されていません！");
            return 0;
        }

        int count = 0;
        foreach (Transform child in handZone)
        {
            var view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null && view.cardData.isUnit)
            {
                count++;
            }
        }
        return count;
    }

    public void AddToHand(CardData data)
    {
        GameObject prefabToUse = data.isUnit ? unitCardPrefab : eventCardPrefab;

        GameObject cardGO = Instantiate(prefabToUse, handZone);
        CardView view = cardGO.GetComponent<CardView>();
        view.SetCard(data, true);

    }
    public void DrawCard(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (cardPool.Length == 0) return;

            int index = Random.Range(0, cardPool.Length);
            CardData drawnCard = cardPool[index];
            SpawnCardToHand(drawnCard);
        }
    }
    //EX処理のサポート関数
    public bool HasCardWithName(string name)
    {
        foreach (Transform child in handZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.GetCardData() != null)
            {
                if (view.GetCardData().cardName.Contains(name))
                {
                    return true;
                }
            }
        }
        return false;
    }


    public CardView RemoveCardByName(string name)
    {
        foreach (Transform child in handZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null &&
                view.cardData.cardName.Contains(name))
            {
                //  先に保持してから削除
                CardView result = view;
                result.transform.SetParent(null); // 念のため一度外す
                Destroy(view.gameObject);
                return result;
            }
        }
        return null;
    }

    // 手札から指定したCardViewを削除する
    public bool RemoveCard(CardView cardView)
    {
        if (cardView == null)
        {
            Debug.LogWarning(" RemoveCardにnullが渡されました");
            return false;
        }

        // 親がhandZoneかチェック（手札にあるか）
        if (cardView.transform.parent != handZone)
        {
            Debug.LogWarning(" このカードは手札にありません！");
            return false;
        }

        Destroy(cardView.gameObject);
        return true;
    }


}
