using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public Transform discardZone; // T‚¦UI‚ÌScrollView‚È‚Ç

    public List<CardData> deck = new List<CardData>(); // RD‚ÌƒJ[ƒhƒŠƒXƒg
    public DiscardManager discardManager; // T‚¦ºiÌ‚ÄDj‚É‘—‚éæ

    public void TakeDamage(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
            {
                Debug.LogWarning("ƒfƒbƒL‚ªs‚«‚½I”s–kˆ—‚ğ“ü‚ê‚é‚×‚«");
                return;
            }

            CardData damageCard = deck[0];
            deck.RemoveAt(0);
            Debug.Log($"{damageCard.cardName} ‚ğT‚¦º‚É‘—‚è‚Ü‚·");

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

