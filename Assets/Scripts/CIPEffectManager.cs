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
            Debug.Log($"CIP���ʁF{cardView.cardData.cardName} �̌��ʂ�1���̂Ă܂�");

            HandSelectionUI.Instance.StartSelection(
                1,
                view => view != null && view != cardView, // ���g�ȊO�̎�D
                selected =>
                {
                    foreach (var discard in selected)
                    {
                        HandManager.Instance.RemoveCard(discard);
                        DiscardManager.Instance.AddToDiscard(discard.cardData);
                        Debug.Log($"CIP���ʂ� {discard.cardData.cardName} ���̂Ă܂���");
                    }
                },
                null
            );
        }
    }
}