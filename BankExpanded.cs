using UnityEngine;

public class BankExpanded : MonoBehaviour
{
    public enum QualityType
    {

        UNDEFINED = 0,
        WORST = 1,
        POOR = 2,
        AVERAGE = 3,
        GOOD = 4,
        BEST = 5


    }

    #region Variables
    public float bonusRate;
    public long BankDepositDate;
    public long RemainedDays;
    public bool BonusRewarded;
    public QualityType Quality;
    public float QualityFactor;
    #endregion

    public BankExpanded()
    {
        SetQuality((QualityType)Random.Range(1, 6));
        SetbonusRate(BanksRemastered.BonusRate);
        SetBankDepositDate(0);
        SetRemainedDays(0);
        SetBonusRewarded(false);
       
    }

    #region Getters and Setters
    public float GetbonusRate()
    {

        return bonusRate;

    }

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

    public void SetbonusRate(float _bonusRate)
    {

        bonusRate = _bonusRate;
        if (BanksRemastered.BankQuality == true && _bonusRate + QualityFactor >= 1)
            bonusRate += QualityFactor;


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
        switch (Quality)
        {

            case QualityType.WORST:

                QualityFactor = -2;

             break;

            case QualityType.POOR:

                QualityFactor = -1;

                break;

            case QualityType.AVERAGE:

                QualityFactor = 0;

            break;

            case QualityType.GOOD:

                QualityFactor = 1;

             break;

            case QualityType.BEST:


                QualityFactor = 2;

             break;



        }

        

    }

    #endregion

    public string PrintBankQualityMessage()
    {
        switch (GetQualityType())
        {

            case QualityType.WORST:

                return "The walls are faded, and a few shelves with records look like they might collapse at any moment. The clerks seem bored, performing their tasks mechanically without much attention. One leans against the counter while another wears down his quill on a scrap of paper. An old guard dozes off in a corner.";

            break;

            case QualityType.POOR:
                return "The bank has a cold atmosphere, with old but still sturdy furniture. The clerks work without haste, double-checking documents before approving transactions. A young, somewhat inattentive guard scans the room but seems more concerned about his night shift than security.";
                

            break;

            case QualityType.AVERAGE:

                return "The interior is simple but well-organized. The furniture is clean, without unnecessary decorations. The clerks work efficiently but don’t seem interested in small talk. A guard patrols with an air of routine, while a discreet metal door leads to a storage area.";
               

             break;

            case QualityType.GOOD:

                return "The floor is spotless, and the furniture looks new, though without any extravagance. The clerks serve customers quickly and efficiently, without excessive formalities. A well-equipped guard stands near the entrance, while the bank’s vault is visibly secured behind a metal grate.";
                
            break;

            case QualityType.BEST:

                return "The room is well-lit, and documents are meticulously organized. The clerks are quick and respectful, wasting no time on unnecessary pleasantries. The guards are well-equipped and switch shifts with precision. A heavy door reinforced with metal bars protects the bank’s vault.";
                
             break;



        }

        return "Undefined Bank Quality";

    }

}
