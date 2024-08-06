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


#region Containers
public struct BankStruct
    {

        public long BankDepositDate;
        public long RemainedDays;
        public bool BonusRewarded;

    }

    [FullSerializer.fsObject("v1")]
    public class BanksRemasteredSaveData
    {
        
        public BankStruct[] bankstruct = new BankStruct[BanksRemastered.BankStructSize];
        public bool HasLoan;

    }

    #endregion

public class BanksRemastered : MonoBehaviour, IHasModSaveData
 {

        public static BanksRemastered instance;

        #region Constants
    
        private const long ConversionTime = DaggerfallDateTime.DaysPerYear * DaggerfallDateTime.MinutesPerDay;
        private const float MessageDelay = 6f;

    #endregion

    #region Variables

        static readonly int[] loanVals = { 10000, 15000, 20000, 25000, 30000, 35000, 40000, 45000, 50000 };
        static readonly float[] BonusRateOffsetVals = { 1f, 1.05f, 1.095f, 1.15f, 1.2f, 1.25f, 1.3f, 1.35f, 1.4f, 1.45f, 1.5f };
    
         public static bool AutomaticDeposit { get; set; }
        public static bool RestrictLoanOption { get; set; }
        public static bool BonusRateWithStats { get; set; }
        private bool HasLoadedData = false;
        private bool HasLoan = false;

        public static int LoanAmount { get; set; }
        public static int DepositDaysNumber { get; set; }
        public static int BankStructSize = DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount + 1;
        private long DepositDaysDue;
        private int initialLoanAmount;
        public static float BonusRate { get; set; }
        public static float BonusRateOffset { get; set; }

    public BankStruct[] bankstruct = new BankStruct[BankStructSize];

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
            mod.SaveDataInterface = instance;
            FormulaHelper.RegisterOverride(mod, "CalculateMaxBankLoan", (Func<int>)CalculateMaxBankLoan);
            mod.IsReady = true;
        }

    private static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
    {
        try
        {
            AutomaticDeposit = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowAutomaticDepositing");
            RestrictLoanOption = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowLoanRestriction");
            LoanAmount = loanVals[mod.GetSettings().GetInt("GeneralSettings", "LoanMaxPerLevel")];
            BonusRate = mod.GetSettings().GetValue<float>("GeneralSettings", "BonusRate");
            DepositDaysNumber = mod.GetSettings().GetValue<int>("GeneralSettings", "DepositDays");
            BonusRateWithStats = mod.GetSettings().GetValue<bool>("MiscSettings", "AllowStatsToCalculateBonusRate");
            BonusRateOffset = BonusRateOffsetVals[mod.GetSettings().GetInt("MiscSettings", "BonusRateOffset")];

        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load settings: {ex.Message}");
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
        
             mod.LoadSettings();
             DepositDaysDue = DaggerfallDateTime.MinutesPerDay * DepositDaysNumber;
             initialLoanAmount = LoanAmount;

             DaggerfallWorkshop.Game.Serialization.SaveLoadManager.OnStartLoad += (SaveData_v1 saveData) =>
             {
                 HasLoadedData = false;
             };

             if (RestrictLoanOption == true)
             {
             
                  DaggerfallBankManager.OnBorrowLoan += RestrictLoaning;
                  DaggerfallBankManager.OnRepayLoan += EnableLoaning;
             
             }
                
            if (AutomaticDeposit == true)
            {
                DaggerfallBankManager.OnDepositGold += AUTOSetDepositTimer;
                DaggerfallBankManager.OnDepositLOC += AUTOSetDepositTimer;

            }
            else
            {
                DaggerfallBankManager.OnDepositGold += SetDepositTimer;
                DaggerfallBankManager.OnDepositLOC += SetDepositTimer;

            }
        }

        private void Update()
        {

             if (HasLoadedData == true)
             {

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

        if (result == TransactionResult.NONE)
            HasLoan = false;

    }

    public static int CalculateMaxBankLoan()
    {
        return GameManager.Instance.PlayerEntity.Level * LoanAmount;
    }

    #endregion

        #region NOAUTO

    private void SetDepositTimer(TransactionType type, TransactionResult result, int amount)
        {

            if (result == TransactionResult.NONE)
            {
                int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
                long date = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
                bankstruct[index].BankDepositDate = date;
                bankstruct[index].BonusRewarded = false;
                DaggerfallUI.AddHUDText("Bonus deposit gold in " + DepositDaysNumber + " days", MessageDelay);


            }
            else
            {
                Debug.Log("Failed Deposit: " + result.ToString());
            }

        }


        private void RewardBonusDeposit()
        {

            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            long CurrentDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
            if (DaggerfallBankManager.BankAccounts[index].accountGold != 0 && CurrentDate >= bankstruct[index].BankDepositDate + DepositDaysDue && bankstruct[index].BonusRewarded == false && bankstruct[index].BankDepositDate != 0)
            {
                DaggerfallBankManager.BankAccounts[index].accountGold += CalculateBonusRate(index);
                DaggerfallUI.AddHUDText("Your Bonus Gold Has Been Added To The Bank", MessageDelay);
                bankstruct[index].BonusRewarded = true;

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
                    long date = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;
                    bankstruct[index].BankDepositDate = date;
                    DaggerfallUI.AddHUDText("Bonus deposit gold in " + DepositDaysNumber + " days", MessageDelay);
                }

            }
            else
            {
                Debug.Log("Failed Deposit: " + result.ToString());
            }

        }

        private void AUTORewardBonusDeposit()
        {

            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            long CurrentDate = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime() + ConversionTime;  
            long DateRewardDeposit = bankstruct[index].BankDepositDate + DepositDaysDue - bankstruct[index].RemainedDays;
        if (DaggerfallBankManager.BankAccounts[index].accountGold != 0 && CurrentDate >= DateRewardDeposit && bankstruct[index].BankDepositDate != 0)
            {

            long DaysPassedFromLastPayout = ((CurrentDate - bankstruct[index].BankDepositDate) + bankstruct[index].RemainedDays);

            for (int i = 1; i <= DaysPassedFromLastPayout / DepositDaysDue; i++)
                    DaggerfallBankManager.BankAccounts[index].accountGold += CalculateBonusRate(index);

                DaggerfallUI.AddHUDText("Your Bonus Gold Has Been Added To The Bank", MessageDelay);
                bankstruct[index].BankDepositDate = CurrentDate;

            if (DaysPassedFromLastPayout >= DepositDaysDue)
                bankstruct[index].RemainedDays = (DaysPassedFromLastPayout % DepositDaysDue);
            else bankstruct[index].RemainedDays = 0;

            }

        }

    #endregion

    private int CalculateBonusRate(int index)
    {

        if (BonusRateWithStats == false)
            return (int)((BonusRate / 100) * DaggerfallBankManager.BankAccounts[index].accountGold);
        else
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            float BonusRateWithStats = BonusRate * (BonusRateOffset + ((float)(playerEntity.Stats.PermanentPersonality * playerEntity.Stats.PermanentLuck * playerEntity.Skills.GetPermanentSkillValue(DaggerfallConnect.DFCareer.Skills.Mercantile))/1000000));
            Debug.Log(BonusRateWithStats);
            return (int)((BonusRateWithStats / 100) * DaggerfallBankManager.BankAccounts[index].accountGold);
        }
    }

        #region SaveMethods

    
        public Type SaveDataType
        {
            get { return typeof(BanksRemasteredSaveData); }
        }

        public object NewSaveData()
        {
            return new BanksRemasteredSaveData
            {

                bankstruct = new BankStruct[BankStructSize],
                HasLoan = false
            };
        }

    public object GetSaveData()
    {
        try
        {
            BanksRemasteredSaveData saveData = new BanksRemasteredSaveData
            {
                bankstruct = new BankStruct[BankStructSize]
            };

            for (int i = 0; i < bankstruct.Length; i++)
            {
                saveData.bankstruct[i] = bankstruct[i];
            }

            saveData.HasLoan = HasLoan;

            return saveData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in GetSaveData: {ex.Message}");
            return null;
        }
    }


    public void RestoreSaveData(object saveData)
    {
        try
        {
            BanksRemasteredSaveData bankSaveData = (BanksRemasteredSaveData)saveData;
            bankstruct = new BankStruct[BankStructSize];

            for (int i = 0; i < bankSaveData.bankstruct.Length; i++)
            {
                bankstruct[i] = bankSaveData.bankstruct[i];
            }

            HasLoan = bankSaveData.HasLoan;

            HasLoadedData = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in RestoreSaveData: {ex.Message}");
        }


    }

    #endregion



}


