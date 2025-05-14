using System;
using UnityEngine;
using static CardData;

public class CostRequirement
{
    public CostType type;
    public int amount; // X�w��n�̂���
    public Func<CardView, bool> isPayable; // �ΏۃJ�[�h��n��
    public Action doPay;

    public CostRequirement(CostType type, int amount, Func<CardView, bool> isPayable, Action doPay)
    {
        this.type = type;
        this.amount = amount;
        this.isPayable = isPayable;
        this.doPay = doPay;
    }
}