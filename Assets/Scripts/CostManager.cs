using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardData;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public class CostManager : MonoBehaviour
{
    public static class CostFactory
    {
        public static CostRequirement Create(CostType type, int amount)
        {
            switch (type)
            {
                case CostType.DiscardXUnit:
                    return new CostRequirement(
                        type,
                        amount,
                        () => HandManager.Instance.CountUnitCardsInHand() >= amount,
                        () => HandManager.Instance.DiscardUnitFromHand(amount)
                    );

                case CostType.None:
                default:
                    return new CostRequirement(type, amount, () => true, () => { });
                
            }
        }
    }
}