using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardCostType
{
    無し,
    裏キャラを表に,           // 裏キャラを表に
    手札に戻して登場,           // 手札に戻して登場（乱入）
    手札を捨てて控えに送る,        // 手札を捨てて控えに送る
    キャラを捨てて登場,   // キャラを捨てて登場
    手札を枚捨てて発動       // 手札を◯枚捨てて発動
}