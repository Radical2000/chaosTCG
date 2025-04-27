using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardPlayManager : MonoBehaviour
{
    public Transform fieldZone;
    public HandManager handManager;
    public GameObject cardPrefab;
    


    public bool CanPlayCard(CardData card)
    {
        switch (card.costType)
        {
            case CardCostType.���L������\��:
                return fieldZone.GetComponentsInChildren<CardView>().Any(c => !c.isFaceUp);
            case CardCostType.��D�ɖ߂��ēo��:
            case CardCostType.�L�������̂Ăēo��:
                return fieldZone.childCount > 0;
            case CardCostType.��D���̂ĂčT���ɑ���:
            case CardCostType.��D�𖇎̂ĂĔ���:
                return handManager.HandCount >= card.costAmount;
            case CardCostType.����:
            default:
                return true;
        }
    }

    public void PlayCard(CardView cardView)
    {
        CardData card = cardView.GetCardData();
        if (!CanPlayCard(cardView.GetCardData()))
        {
            Debug.Log("�R�X�g��������������Ă��܂���I");
            return;
        }
        if (cardView.transform.parent.name != "PlayerHand")
        {
            Debug.LogWarning(" ��D�ȊO�̃J�[�h�͏�ɏo���܂���I");
            return;
        }
        if (!ActionLimiter.Instance.CanSummon())
        {
            Debug.LogWarning("���̃^�[���̏����͂��ł�1��s���܂����I");
            return;
        }
        if (IsSameNameCardOnField(card))
        {
            Debug.Log(" �����J�[�h����ɂ��邽�ߏ����ł��܂���");
            return;
        }

        ActionLimiter.Instance.UseSummon(); // ? �����J�E���g

        PayCost(card);

        // �� �X���b�g�ɔz�u����̂� OnClickSlot ���ł��I
        FieldManager.Instance.selectedCardToSummon = cardView;
        Debug.Log("? PlayCard �������� �� �X���b�g���N���b�N���Ă��������I");
    }
    

    private void PayCost(CardData card)
    {
        switch (card.costType)
        {
            case CardCostType.��D���̂ĂčT���ɑ���:
            case CardCostType.��D�𖇎̂ĂĔ���:
                handManager.DiscardFromHand(card.costAmount);
                break;
            case CardCostType.���L������\��:
                FlipOneFacedownCard();
                break;
            case CardCostType.��D�ɖ߂��ēo��:
                ReturnCharacterToHand(); // ������
                break;
            case CardCostType.�L�������̂Ăēo��:
                SacrificeRandomFieldCharacter(); // ������
                break;
        }
    }

    private void FlipOneFacedownCard()
    {
        var facedown = fieldZone.GetComponentsInChildren<CardView>().FirstOrDefault(c => !c.isFaceUp);
        if (facedown != null) facedown.SetFaceUp(true);
    }

    private void SacrificeRandomFieldCharacter()
    {
        if (fieldZone.childCount > 0)
        {
            Transform target = fieldZone.GetChild(0);
            Destroy(target.gameObject);
        }
    }
    private void ReturnCharacterToHand()
    {
        if (fieldZone.childCount == 0) return;

        Transform target = fieldZone.GetChild(0); // ���ōŏ��̃J�[�h
        CardView cardView = target.GetComponent<CardView>();

        if (cardView != null)
        {
            handManager.AddToHand(cardView.GetCardData()); // ��D�ɖ߂�
            Destroy(target.gameObject); // �t�B�[���h����폜
        }
    }

    public void SpawnCardToField(CardData data)
    {
        GameObject cardGO = Instantiate(cardPrefab, fieldZone);
        CardView view = cardGO.GetComponent<CardView>();
        view.SetCard(data, true); // �\�����ŕ\��
        view.isNewlySummoned = true;//�����ł��̃^�[��������������bool��ύX
    }

    //EX�����i�������j
    public void OnClickEX()
    {
        if (!ActionLimiter.Instance.CanEX())
        {
            Debug.Log("EX���͂��̃^�[�����łɎg�p�ς݂ł��I");
            return;
        }

        // EX�������������ɏ���

        ActionLimiter.Instance.UseEX();
    }
    //���x���A�b�v�����i�������j
    public void OnClickLevelUp()
    {
        if (!ActionLimiter.Instance.CanLevelUp())
        {
            Debug.Log("���x���A�b�v�͂��̃^�[�����łɎg�p�ς݂ł��I");
            return;
        }

        // ���x���A�b�v�����������ɏ���

        ActionLimiter.Instance.UseLevelUp();
    }
    //�������j�b�g�Ɋւ��鏈��
    public bool IsSameNameCardOnField(CardData card)
    {
        foreach (Transform child in fieldZone)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.cardData != null)
            {
                // ������v�iA �� B ���܂ޖ��O���ǂ����j
                if (view.cardData.cardName.Contains(card.cardName) ||
                    card.cardName.Contains(view.cardData.cardName))
                {
                    Debug.LogWarning($" �����J�[�h�����łɏ�ɑ��݂��܂��F{view.cardData.cardName}");
                    return true;
                }
            }
        }
        return false;
    }

}
