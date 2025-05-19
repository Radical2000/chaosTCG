using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CIPEffectManager : MonoBehaviour
{
    public static CIPEffectManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TryTriggerCIPEffect(CardView cardView)
    {
        if (cardView == null || cardView.cardData == null) return;

        if (cardView.cardData.hasCIPDiscardEffect)
        {
            Debug.Log($"CIPŒø‰ÊF{cardView.cardData.cardName} ‚ÌŒø‰Ê‚Å1–‡ŽÌ‚Ä‚Ü‚·");

            HandSelectionUI.Instance.StartSelection(
                1,
                view => view != null && view != cardView, // Ž©gˆÈŠO‚ÌŽèŽD
                selected =>
                {
                    foreach (var discard in selected)
                    {
                        HandManager.Instance.RemoveCard(discard);
                        DiscardManager.Instance.AddToDiscard(discard.cardData);
                        Debug.Log($"CIPŒø‰Ê‚Å {discard.cardData.cardName} ‚ðŽÌ‚Ä‚Ü‚µ‚½");
                    }
                },
                null
            );
        }
    }
}