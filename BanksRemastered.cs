using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;


public class BanksRemastered : MonoBehaviour
{
  
    #region Constants

    private const int ConversionTime = DaggerfallDateTime.DaysPerYear * DaggerfallDateTime.MinutesPerDay;
    private const float MessageDelay = 6f;

    #endregion

    #region Variables

    public static bool AutomaticDeposit { get; set; }
    public static float BonusRate { get; set; }

    public static int DepositDaysNumber { get; set; }

    static int BankDepositDateSize = DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount + 1;
    private int DepositDaysDue;



    #endregion

    #region Structs
    struct BankStruct
    {

        public uint BankDepositDate;
        public bool BonusRewarded;

    }

    private BankStruct[] bankstruct = new BankStruct[BankDepositDateSize];

    #endregion

    #region Mod Initialization
    static Mod mod;

    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        go.AddComponent<BanksRemastered>();
        mod.LoadSettingsCallback = LoadSettings;
        mod.IsReady = true;
    }


    #endregion

    #region Unity Methods
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }


    void Start()
    {
        
        mod.LoadSettings();
        DepositDaysDue = DaggerfallDateTime.MinutesPerDay * DepositDaysNumber;

        if (AutomaticDeposit == true)
        {
            DaggerfallBankManager.OnDepositGold += AUTOSetDepositTimer;
            DaggerfallBankManager.OnDepositLOC += AUTOSetDepositTimer;
            WorldTime.OnNewHour += AUTORewardBonusDeposit;

        }
        else
        {
            DaggerfallBankManager.OnDepositGold += SetDepositTimer;
            DaggerfallBankManager.OnDepositLOC += SetDepositTimer;
            WorldTime.OnNewHour += RewardBonusDeposit;
        }
    }

    private static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
    {
        AutomaticDeposit = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowAutomaticDepositing");
        BonusRate = mod.GetSettings().GetValue<float>("GeneralSettings", "BonusRate");
        DepositDaysNumber = mod.GetSettings().GetValue<int>("GeneralSettings", "DepositDays");

    }

    #endregion

    #region NOAUTO

    private void SetDepositTimer(TransactionType type, TransactionResult result, int amount)
    {

        if (result == TransactionResult.NONE)
        {
            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            uint date = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
            bankstruct[index].BankDepositDate = date;
            bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BonusRewarded = false;
            DaggerfallUI.AddHUDText("Bonus deposit gold in " + DepositDaysDue/DaggerfallDateTime.MinutesPerDay + " days", MessageDelay);


        }
        else
        {
            Debug.Log("Depozitarea a eșuat: " + result.ToString());
        }

    }


    private void RewardBonusDeposit()
    {

        int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
        uint CurrentDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
        if (DaggerfallBankManager.BankAccounts[index].accountGold != 0 && CurrentDate >= bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BankDepositDate + DepositDaysDue && bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BonusRewarded == false && bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BankDepositDate != 0)
        {
            DaggerfallBankManager.BankAccounts[index].accountGold += (int)((BonusRate / 100) * DaggerfallBankManager.BankAccounts[index].accountGold);
            DaggerfallUI.AddHUDText("Your Bonus Gold Has Been Added To The Bank", MessageDelay);
            bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BonusRewarded = true;
            
        }

    }

    #endregion

    #region AUTO
    private void AUTOSetDepositTimer(TransactionType type, TransactionResult result, int amount)
    {

        if (result == TransactionResult.NONE)
        {
            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            
            if (bankstruct[index].BankDepositDate == 0)
            {
                uint date = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
                bankstruct[index].BankDepositDate = date;
                DaggerfallUI.AddHUDText("Bonus deposit gold in " + DepositDaysDue / DaggerfallDateTime.MinutesPerDay + " days", MessageDelay);
            }

        }
        else
        {
            Debug.Log("Depozitarea a eșuat: " + result.ToString());
        }

    }

    private void AUTORewardBonusDeposit()
    {

        int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
        uint CurrentDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
        if (DaggerfallBankManager.BankAccounts[index].accountGold != 0 && CurrentDate >= bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BankDepositDate + DepositDaysDue &&  bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BankDepositDate != 0)
        {
            uint DaysPassedWithDeposit = (CurrentDate - bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BankDepositDate) / DaggerfallDateTime.MinutesPerDay;
            Debug.Log(DaysPassedWithDeposit);

            for (int i=1; i<= DaysPassedWithDeposit; i++)
            DaggerfallBankManager.BankAccounts[index].accountGold += (int)((BonusRate / 100) * DaggerfallBankManager.BankAccounts[index].accountGold);

            DaggerfallUI.AddHUDText("Your Bonus Gold Has Been Added To The Bank", MessageDelay);
            bankstruct[GameManager.Instance.PlayerGPS.CurrentRegionIndex].BankDepositDate = CurrentDate;

        }

    }

    #endregion
}
