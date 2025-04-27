using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public Transform discardZone; // �T��UI��ScrollView�Ȃ�

    public List<CardData> deck = new List<CardData>(); // �R�D�̃J�[�h���X�g
    public DiscardManager discardManager; // �T�����i�̂ĎD�j�ɑ����

    public void TakeDamage(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
            {
                Debug.LogWarning("�f�b�L���s�����I�s�k����������ׂ�");
                return;
            }

            CardData damageCard = deck[0];
            deck.RemoveAt(0);
            Debug.Log($"{damageCard.cardName} ���T�����ɑ���܂�");

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

