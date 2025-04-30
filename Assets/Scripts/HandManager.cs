using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandManager : MonoBehaviour
{


    public static HandManager Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public Transform handZone; // ��D����ׂ�ꏊ
    public GameObject unitCardPrefab;
    public GameObject eventCardPrefab;

    public CardData[] cardPool; // �f�b�L or �J�[�h�ꗗ�������

    public int startHandCount = 5;

    private List<CardData> handCards = new List<CardData>(); // �� �ǉ��I

    public int HandCount => handCards.Count; // �� �ǉ��I

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
    /// �w�薇����D����̂Ă�i�擪����j
    /// </summary>
    public void DiscardFromHand(int amount)
    {
        for (int i = 0; i < amount && handCards.Count > 0; i++)
        {
            handCards.RemoveAt(0);

            // UI�����폜
            if (handZone.childCount > 0)
            {
                Destroy(handZone.GetChild(0).gameObject);
            }

            // �K�v�Ȃ�̂ĎD�G���A�i�T�����j�ɑ��鏈���������Œǉ�
        }
    }
    public void AddToHand(CardData data)
    {
        GameObject prefabToUse = data.isUnit ? unitCardPrefab : eventCardPrefab;

        GameObject cardGO = Instantiate(prefabToUse, handZone);
        CardView view = cardGO.GetComponent<CardView>();
        view.SetCard(data, true);

        handCards.Add(data);
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
    //EX�����̃T�|�[�g�֐�
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
                //  ��ɕێ����Ă���폜
                CardView result = view;
                result.transform.SetParent(null); // �O�̂��߈�x�O��
                Destroy(view.gameObject);
                return result;
            }
        }
        return null;
    }

    // ��D����w�肵��CardView���폜����
    public bool RemoveCard(CardView cardView)
    {
        if (cardView == null)
        {
            Debug.LogWarning(" RemoveCard��null���n����܂���");
            return false;
        }

        // �e��handZone���`�F�b�N�i��D�ɂ��邩�j
        if (cardView.transform.parent != handZone)
        {
            Debug.LogWarning(" ���̃J�[�h�͎�D�ɂ���܂���I");
            return false;
        }

        Destroy(cardView.gameObject);
        return true;
    }


}
