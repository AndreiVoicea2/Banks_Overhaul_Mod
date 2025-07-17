////////////////////////////////////////////////////////////////////
//Project Purpose: Enhancing the Daggerfall Unity Bank System     //
////////////////////////////////////////////////////////////////////
//Class Purpose: Main Bank Manager Which Handles All Functionality//
////////////////////////////////////////////////////////////////////
//Made by: Andrei Voicea                                          //
////////////////////////////////////////////////////////////////////


using UnityEngine;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallConnect;
using static DaggerfallWorkshop.Game.PlayerEnterExit;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using static BankMessageHandler;




#region Containers


[FullSerializer.fsObject("v1")]
public class BanksRemasteredSaveData
{

    public BankExpanded[] bankstruct = new BankExpanded[BanksRemastered.BankStructSize];
    public bool HasLoan;
    public bool LoadedFirstTime;
    public long RegionEventsRemainedDays;
    public long LastCheckoutDate;
}

#endregion

public class BanksRemastered : MonoBehaviour, IHasModSaveData
{

    public static BanksRemastered instance;

    #region Constants

    private const long ConversionTime = DaggerfallDateTime.DaysPerYear * DaggerfallDateTime.MinutesPerDay;
    private const float MessageDelay = 6f;

    private const long DaysForRegionFlagRefresh = 55500;

    #endregion

    #region Variables

    static readonly int[] loanVals = { 10000, 15000, 20000, 25000, 30000, 35000, 40000, 45000, 50000 };
    static readonly float[] BonusRateOffsetVals = { 1f, 1.05f, 1.095f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f, 1.5f };

    public static bool AutomaticDeposit { get; set; }
    public static bool RestrictLoanOption { get; set; }
    public static bool BonusRateWithStats { get; set; }

    public static bool BankQuality { get; set; }

    public static bool RegionFactors { get; set; }

    private bool HasLoadedData = false;
    private bool HasLoan = false;
    private bool LoadedFirstTime = false;
    private bool LoadedEventsFirstTime = false;



    public static int LoanAmount { get; set; }
    public static int DepositDaysNumber { get; set; }
    public static int BankStructSize = DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount + 1;

    public static int PercentageLost { get; set; }

    public static int SpoilsOfWar { get; set; }
    private long DepositDaysDue;
    private long RegionEventsRemainedDays;
    private int initialLoanAmount;

    private long LastCheckoutDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;

    public static float BonusRate { get; set; }
    public static float BonusRateOffset { get; set; }

    public BankExpanded[] bankstruct = new BankExpanded[BankStructSize];

    private static PlayerEntity playerentity = GameManager.Instance.PlayerEntity;


    #endregion

    #region Mod Initialization
    static Mod mod;

    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        go.AddComponent<BanksRemastered>();
        mod.SaveDataInterface = instance;
        FormulaHelper.RegisterOverride(mod, "CalculateMaxBankLoan", (Func<int>)CalculateMaxBankLoan);
        mod.IsReady = true;
    }

    private void LoadSettings(ModSettings modSettings, ModSettingsChange change)
    {
        try
        {

            if (change.HasChanged("DepositSettings"))
            {
                bool PreviousAutomaticDepositSetting = AutomaticDeposit;
                bool PreviousBankQuality = BankQuality;

                AutomaticDeposit = mod.GetSettings().GetValue<bool>("DepositSettings", "AllowAutomaticDepositing");
                BonusRate = mod.GetSettings().GetValue<float>("DepositSettings", "BonusRate");
                DepositDaysNumber = mod.GetSettings().GetValue<int>("DepositSettings", "DepositDays");
                BankQuality = mod.GetSettings().GetValue<bool>("DepositSettings", "AllowBankQuality");
                BonusRateWithStats = mod.GetSettings().GetValue<bool>("DepositSettings", "AllowStatsForBonus");
                BonusRateOffset = BonusRateOffsetVals[mod.GetSettings().GetInt("DepositSettings", "BonusRateOffset")];
                DepositDaysDue = DaggerfallDateTime.MinutesPerDay * DepositDaysNumber;


                if (AutomaticDeposit != PreviousAutomaticDepositSetting || LoadedEventsFirstTime == false)
                {

                    if (AutomaticDeposit == true)
                    {
                        if (LoadedEventsFirstTime == true)
                        {
                            DaggerfallBankManager.OnDepositGold -= SetDepositTimer;
                            DaggerfallBankManager.OnDepositLOC -= SetDepositTimer;
                        }

                        DaggerfallBankManager.OnDepositGold += AUTOSetDepositTimer;
                        DaggerfallBankManager.OnDepositLOC += AUTOSetDepositTimer;


                    }
                    else
                    {
                        if (LoadedEventsFirstTime == true)
                        {
                            DaggerfallBankManager.OnDepositGold -= AUTOSetDepositTimer;
                            DaggerfallBankManager.OnDepositLOC -= AUTOSetDepositTimer;
                        }


                        DaggerfallBankManager.OnDepositGold += SetDepositTimer;
                        DaggerfallBankManager.OnDepositLOC += SetDepositTimer;


                    }
                }

                if (BankQuality != PreviousBankQuality || LoadedEventsFirstTime == false)
                {

                    if (BankQuality == true)
                        OnTransitionInterior += HandleTransitionToInterior;
                    else
                    {
                        if (LoadedEventsFirstTime == true)
                            OnTransitionInterior -= HandleTransitionToInterior;
                    }
                }



            }

            if (change.HasChanged("LoanSettings"))
            {
                bool PreviousRestrictLoanOption = RestrictLoanOption;
                RestrictLoanOption = mod.GetSettings().GetValue<bool>("LoanSettings", "AllowLoanRestriction");
                LoanAmount = loanVals[mod.GetSettings().GetInt("LoanSettings", "LoanMaxPerLevel")];
                initialLoanAmount = LoanAmount;


                if (RestrictLoanOption != PreviousRestrictLoanOption || LoadedEventsFirstTime == false)
                {

                    if (RestrictLoanOption == true)
                    {
                        DaggerfallBankManager.OnBorrowLoan += RestrictLoaning;
                        DaggerfallBankManager.OnRepayLoan += EnableLoaning;

                    }
                    else
                    {
                        if (LoadedEventsFirstTime == true)
                        {
                            DaggerfallBankManager.OnBorrowLoan -= RestrictLoaning;
                            DaggerfallBankManager.OnRepayLoan -= EnableLoaning;
                        }

                    }

                }


            }

            if (change.HasChanged("RegionEventsSettings"))
            {

                RegionFactors = mod.GetSettings().GetValue<bool>("RegionEventsSettings", "AllowRegionFactors");
                PercentageLost = mod.GetSettings().GetValue<int>("RegionEventsSettings", "GoldPercentageLost");
                SpoilsOfWar = mod.GetSettings().GetValue<int>("RegionEventsSettings", "SpoilsOfWarPercentage");

            }


            LoadedEventsFirstTime = true;

        }
        catch
        {

            Debug.LogError(GeneralMessageHandler(MessageState.FAILED_LOAD_SETTINGS));
        }

    }


    #endregion

    #region Unity Methods
    private void Awake()
    {
        instance = this;

    }


    private void Start()
    {
        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();


        DaggerfallWorkshop.Game.Serialization.SaveLoadManager.OnStartLoad += (SaveData_v1 saveData) =>
        {
            HasLoadedData = false;
        };

    }

    private void Update()
    {

        if (HasLoadedData == true)
        {

            if (LoadedFirstTime == false)
            {
                
                for (int i = 0; i < BankStructSize; i++)
                    bankstruct[i] = new BankExpanded();
                LoadedFirstTime = true;

            }

            if (RegionFactors == true)
                WriteRegionFlags();

            if (AutomaticDeposit == true)
            {

                AUTORewardBonusDeposit();

            }
            else
            {

                RewardBonusDeposit();

            }

            if (RestrictLoanOption == true)
            {
                if (HasLoan == true)
                    LoanAmount = 0;
                else LoanAmount = initialLoanAmount;

            }

        }

    }

    #endregion

    #region LoanRestrictionMethods

    private void RestrictLoaning(TransactionType type, TransactionResult result, int amount)
    {

        if (result == TransactionResult.NONE)
            HasLoan = true;

    }

    private void EnableLoaning(TransactionType type, TransactionResult result, int amount)
    {
        int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
        if ((result == TransactionResult.NONE && DaggerfallBankManager.HasLoan(index) == false) || result == TransactionResult.OVERPAID_LOAN)
            HasLoan = false;


    }

    public static int CalculateMaxBankLoan()
    {
        return playerentity.Level * LoanAmount;
    }

    #endregion


    private void HandleTransitionToInterior(TransitionEventArgs args)
    {
        if (GameManager.Instance.PlayerEnterExit.BuildingType == DFLocation.BuildingTypes.Bank && PlayerActivate.IsBuildingOpen(DFLocation.BuildingTypes.Bank))
        {

            DaggerfallMessageBox mb = null;
            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            mb = DaggerfallUI.MessageBox(PrintBankQualityMessage(bankstruct[index].GetQualityType()), true);
        }
    }



    #region NOAUTO

    private void SetDepositTimer(TransactionType type, TransactionResult result, int amount)
    {

        if (result == TransactionResult.NONE)
        {
            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            long date = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;

            bankstruct[index].SetBankDepositDate(date);
            bankstruct[index].SetBonusRewarded(false);



            DaggerfallUI.AddHUDText(GeneralMessageHandler(DepositDaysNumber == 1 ? MessageState.DEPOSIT_ONE_DAY : MessageState.DEPOSIT, DepositDaysNumber), MessageDelay);


        }
        else
        {
            Debug.Log(GeneralMessageHandler(MessageState.FAILED_DEPOSIT));
        }


    }


    private void RewardBonusDeposit()
    {

        int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
        long CurrentDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
        if (DaggerfallBankManager.BankAccounts[index].accountGold != 0 && CurrentDate >= bankstruct[index].GetBankDepositDate() + DepositDaysDue && bankstruct[index].IsBonusRewarded() == false && bankstruct[index].GetBankDepositDate() != 0)
        {
            float bonusrate = CalculateBonusRate(index);
            int BonusGold = CalculateBonusGold(index, bonusrate);
            DaggerfallBankManager.BankAccounts[index].accountGold += BonusGold;
            DaggerfallUI.AddHUDText(GeneralMessageHandler(MessageState.REWARD, BonusGold, (int)bonusrate), MessageDelay);
            bankstruct[index].SetBonusRewarded(true);

        }

    }

    #endregion

    #region AUTO
    private void AUTOSetDepositTimer(TransactionType type, TransactionResult result, int amount)
    {

        if (result == TransactionResult.NONE)
        {
            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;

            if (bankstruct[index].GetBankDepositDate() == 0)
            {
                long date = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
                bankstruct[index].SetBankDepositDate(date);
                DaggerfallUI.AddHUDText(GeneralMessageHandler(DepositDaysNumber == 1 ? MessageState.DEPOSIT_ONE_DAY : MessageState.DEPOSIT, DepositDaysNumber), MessageDelay);


            }

        }
        else
        {
            Debug.Log(GeneralMessageHandler(MessageState.FAILED_DEPOSIT));
        }

    }

    private void AUTORewardBonusDeposit()
    {

        int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;

        long CurrentDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
        long DateRewardDeposit = bankstruct[index].GetBankDepositDate() + DepositDaysDue - bankstruct[index].GetRemainedDays();

        if (CurrentDate >= DateRewardDeposit && bankstruct[index].GetBankDepositDate() != 0)
        {

            long DaysPassedFromLastPayout = ((CurrentDate - bankstruct[index].GetBankDepositDate()) + bankstruct[index].GetRemainedDays());
            int initialGold = DaggerfallBankManager.BankAccounts[index].accountGold;

            if (initialGold == 0)
            {

                DaggerfallUI.AddHUDText(GeneralMessageHandler(MessageState.MISSED_DEPOSIT), MessageDelay);

            }
            else
            {

                float bonusrate = CalculateBonusRate(index);

                for (int i = 1; i <= DaysPassedFromLastPayout / DepositDaysDue; i++)
                    DaggerfallBankManager.BankAccounts[index].accountGold += CalculateBonusGold(index, bonusrate);
                DaggerfallUI.AddHUDText(GeneralMessageHandler(MessageState.REWARD, DaggerfallBankManager.BankAccounts[index].accountGold - initialGold, (int)bonusrate), MessageDelay);

            }

            bankstruct[index].SetBankDepositDate(CurrentDate);

            if (DaysPassedFromLastPayout >= DepositDaysDue)
                bankstruct[index].SetRemainedDays((DaysPassedFromLastPayout % DepositDaysDue));
            else bankstruct[index].SetBankDepositDate(0);

        }

    }

    private float CalculateBonusRate(int index)
    {
        float bonusRate = BonusRate;

        if (BankQuality == true)
        {
            if (bonusRate + (float)bankstruct[index].GetQualityType() >= 1)
                bonusRate = bonusRate + (float)bankstruct[index].GetQualityType();
            else
                bonusRate = 1;
        }

        if (BonusRateWithStats == true)

            bonusRate = bonusRate * (BonusRateOffset + ((float)(playerentity.Stats.PermanentPersonality * playerentity.Stats.PermanentLuck * playerentity.Skills.GetPermanentSkillValue(DaggerfallConnect.DFCareer.Skills.Mercantile)) / 1000000));

        return bonusRate;

    }

    private int CalculateBonusGold(int index, float bonusRate)
    {
        return (int)((bonusRate / 100) * DaggerfallBankManager.BankAccounts[index].accountGold);
    }

    #endregion


    private void WriteRegionFlags()
    {

        long CurrentDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
        long DaysPassed = CurrentDate - LastCheckoutDate + RegionEventsRemainedDays;

        if (DaysPassed >= DaysForRegionFlagRefresh)
        {

            for (byte i = 0; i < BankStructSize; i++)
            {

                if (bankstruct[i].GetBankDepositDate() > 0)
                {
                    int bankgold = DaggerfallBankManager.BankAccounts[i].accountGold;

                    if (bankgold > 0)
                    {
                        bool[] record_flags = playerentity.RegionData[i].Flags;
                        string RegionName = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegionName(i);
                        
                        if (record_flags[3] || record_flags[5] || record_flags[8] || record_flags[11])
                        {
                            byte flagsPacked = (byte)(
                              ((record_flags[3] ? 1 : 0) << 0) |    //War Lost
                              ((record_flags[5] ? 1 : 0) << 1) |    //Plague
                              ((record_flags[8] ? 1 : 0) << 2) |    //Famine
                              ((record_flags[11] ? 1 : 0) << 3)    //Crime Wave
                            );

                            int goldlost = bankgold * PercentageLost / 100;
                            DaggerfallBankManager.BankAccounts[i].accountGold -= goldlost;
                            DaggerfallUI.AddHUDText(RegionEventsMessageBad(RegionName, flagsPacked, goldlost), MessageDelay);
                        }

                        if (record_flags[2])
                        {
                            int goldwon = bankgold * SpoilsOfWar / 100;
                            DaggerfallBankManager.BankAccounts[i].accountGold += goldwon;
                            DaggerfallUI.AddHUDText(RegionEventsMessageGood(RegionName, goldwon), MessageDelay);
                        }


                    }




                }

            }


            RegionEventsRemainedDays = DaysPassed % DaysForRegionFlagRefresh;
            LastCheckoutDate = CurrentDate;


        }


    }


    #region SaveMethods


    public Type SaveDataType
    {
        get { return typeof(BanksRemasteredSaveData); }
    }

    public object NewSaveData()
    {
        BanksRemasteredSaveData saveData = new BanksRemasteredSaveData
        {
            bankstruct = new BankExpanded[BankStructSize],
            HasLoan = false,
            LoadedFirstTime = false,
            RegionEventsRemainedDays = 0,
            LastCheckoutDate = 0

        };

        return saveData;
    }


    public object GetSaveData()
    {
        try
        {
            BanksRemasteredSaveData saveData = new BanksRemasteredSaveData
            {
                bankstruct = new BankExpanded[BankStructSize]
            };

            for (int i = 0; i < bankstruct.Length; i++)
            {
                saveData.bankstruct[i] = bankstruct[i];
            }

            saveData.HasLoan = HasLoan;
            saveData.LoadedFirstTime = LoadedFirstTime;
            saveData.RegionEventsRemainedDays = RegionEventsRemainedDays;
            saveData.LastCheckoutDate = LastCheckoutDate;

            return saveData;
        }
        catch
        {
            Debug.LogError(MessageState.GET_SAVE_ERROR);
            return null;
        }
    }


    public void RestoreSaveData(object saveData)
    {
        try
        {
            BanksRemasteredSaveData bankSaveData = (BanksRemasteredSaveData)saveData;
            bankstruct = new BankExpanded[BankStructSize];

            for (int i = 0; i < bankSaveData.bankstruct.Length; i++)
            {
                bankstruct[i] = bankSaveData.bankstruct[i];

            }

            HasLoan = bankSaveData.HasLoan;
            LoadedFirstTime = bankSaveData.LoadedFirstTime;
            RegionEventsRemainedDays = bankSaveData.RegionEventsRemainedDays;
            LastCheckoutDate = bankSaveData.LastCheckoutDate;
            HasLoadedData = true;
 
        }
        catch
        {
            Debug.LogError(GeneralMessageHandler(MessageState.LOAD_SAVE_ERROR));
        }


    }

    #endregion


}


