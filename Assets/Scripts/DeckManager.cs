using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public Transform discardZone; // 控えUIのScrollViewなど

    public List<CardData> deck = new List<CardData>(); // 山札のカードリスト
    public DiscardManager discardManager; // 控え室（捨て札）に送る先

    public void TakeDamage(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
            {
                Debug.LogWarning("デッキが尽きた！敗北処理を入れるべき");
                return;
            }

            CardData damageCard = deck[0];
            deck.RemoveAt(0);
            Debug.Log($"{damageCard.cardName} を控え室に送ります");

            if (discardManager != null)
            {
                discardManager.AddToDiscard(damageCard);
            }
        }
    }

    public void AddToDiscard(CardView card)
    {
        card.transform.SetParent(discardZone);
        card.transform.localScale = Vector3.one;
        card.transform.rotation = Quaternion.identity;
    }
}

