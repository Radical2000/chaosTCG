using UnityEngine;

public class DiscardManager : MonoBehaviour
{
    public static DiscardManager Instance;
    private void Awake()
    {
        Instance = this;
    }



    public HandManager handManager;
    public CardPlayManager fieldManager;
    public DiscardManager banishManager; // �܂��� BanishManager �ɕ����Ă�OK
    public DiscardUIController uiController; // Inspector�ɐݒ�


    private CardView selectedCardView;
    public Transform discardZone; // Content �������ɃA�^�b�`
    public Transform banishZone;
    public GameObject cardPrefab;

    public void AddToDiscard(CardData cardData)
    {
        GameObject card = Instantiate(cardPrefab, discardZone);
        CardView view = card.GetComponent<CardView>();
        view.SetCard(cardData, true); // ���\���Ȃ� false
    }

    public void MoveCardViewToDiscard(CardView view)
    {
        view.transform.SetParent(discardZone);
        view.transform.localScale = Vector3.one;
        view.transform.rotation = Quaternion.identity;

        Debug.Log($" �g�p�ς݃C�x���g {view.GetCardData().cardName} ���T�����Ɉړ����܂���");
    }

    public void SelectCard(CardView card)
    {
        selectedCardView = card;
        HighlightCard(card); // �I�𒆂̌����ډ��o
        ShowOptionsUI();     // �u�߂�/�o��/���O�v�{�^����\��
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
        banishManager.AddToBanish(selectedCardView.GetCardData()); // ���O�]�[���ɒǉ�
        Destroy(selectedCardView.gameObject);
        selectedCardView = null;
    }
    private void HighlightCard(CardView card)
    {
        // TODO: �g�����点����A�F��ς�����i�������Ȃ���OK�j
        Debug.Log($"�J�[�h�I��: {card.GetCardData().cardName}");
    }

    private void ShowOptionsUI()
    {
        // TODO: �{�^���̕\���Ȃǁi���Ƃ�UI�Ɛڑ��j
        Debug.Log("����UI�\��");
        uiController.Show();
    }

    public void AddToBanish(CardData cardData)
    {
        GameObject card = Instantiate(cardPrefab, discardZone); // discardZone �����O�]�[���ɐݒ�
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


}
