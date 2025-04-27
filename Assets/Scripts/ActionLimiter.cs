using UnityEngine;

public class ActionLimiter : MonoBehaviour
{
    public static ActionLimiter Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int summonCount = 0;
    public int exCount = 0;
    public int levelUpCount = 0;

    public void ResetMainPhaseLimits()
    {
        summonCount = 0;
        exCount = 0;
        levelUpCount = 0;
    }

    public bool CanSummon() => summonCount < 1;
    public bool CanEX() => exCount < 1;
    public bool CanLevelUp() => levelUpCount < 1;

    public void UseSummon() => summonCount++;
    public void UseEX() => exCount++;
    public void UseLevelUp() => levelUpCount++;
}
