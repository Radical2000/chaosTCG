using UnityEngine;

public class DiscardUIController : MonoBehaviour
{
    public DiscardManager discardManager;

    public void OnClickReturnToHand()
    {
        discardManager.ReturnToHand();
    }

    public void OnClickReturnToField()
    {
        discardManager.ReturnToField();
    }

    public void OnClickBanish()
    {
        discardManager.BanishCard();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
}
