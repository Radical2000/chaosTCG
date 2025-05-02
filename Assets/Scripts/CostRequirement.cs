using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardData;

public class CostRequirement
{
    public CostType type;
    public int amount; // X�w��n�̂���
    public Func<bool> isPayable; // �x�����邩�H
    public Action doPay;         // �x�������s

    public CostRequirement(CostType type, int amount, Func<bool> isPayable, Action doPay)
    {
        this.type = type;
        this.amount = amount;
        this.isPayable = isPayable;
        this.doPay = doPay;
    }
}
