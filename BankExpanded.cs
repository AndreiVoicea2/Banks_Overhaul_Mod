using UnityEngine;

public class BankExpanded
{
    public enum QualityType
    {

        UNDEFINED = 10,
        WORST = -2,
        POOR = -1,
        AVERAGE = 0,
        GOOD = 1,
        BEST = 2


    }

    #region Variables
    public long BankDepositDate;
    public long RemainedDays;
    public bool BonusRewarded;
    public QualityType Quality;
    #endregion

    public BankExpanded()
    {
        SetQuality((QualityType)Random.Range(-2, 3));
        SetBankDepositDate(0);
        SetRemainedDays(0);
        SetBonusRewarded(false);
       
    }

    #region Getters and Setters

    public long GetBankDepositDate()
    {

        return BankDepositDate;

    }

    public long GetRemainedDays()
    {
        return RemainedDays;
    }

    public bool IsBonusRewarded()
    {

        return BonusRewarded;

    }

    public QualityType GetQualityType()
    {

        return Quality;

    }

    public void SetBankDepositDate(long _BankDepositDate)
    {

        BankDepositDate = _BankDepositDate;

    }

    public void SetRemainedDays(long _RemainedDays)
    {

        RemainedDays = _RemainedDays;

    }

    public void SetBonusRewarded(bool _BonusRewarded)
    {

        BonusRewarded = _BonusRewarded;

    }

    public void SetQuality(QualityType _Quality)
    {

        Quality = _Quality;        

    }

    #endregion



}
