using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardData;

public class CostRequirement
{
    public CostType type;
    public int amount; // XéwíËånÇÃÇΩÇﬂ
    public Func<bool> isPayable; // éxï•Ç¶ÇÈÇ©ÅH
    public Action doPay;         // éxï•Ç¢é¿çs

    public CostRequirement(CostType type, int amount, Func<bool> isPayable, Action doPay)
    {
        this.type = type;
        this.amount = amount;
        this.isPayable = isPayable;
        this.doPay = doPay;
    }
}
