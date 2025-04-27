using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventSelectionStarter : MonoBehaviour
{
    public void StartEventSelection()
    {
        Debug.Log(" �C�x���g�J�[�h�I�����[�h�J�n");

        foreach (Transform cardObj in HandManager.Instance.handZone)
        {
            CardView view = cardObj.GetComponent<CardView>();
            if (view != null && !view.cardData.isUnit)
            {
                view.clickMode = CardView.CardClickMode.UseEvent;
            }
        }
    }
}