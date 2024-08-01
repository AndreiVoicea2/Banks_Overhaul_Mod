using UnityEngine;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System;


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

        public static bool AutomaticDeposit { get; set; }
        public static float BonusRate { get; set; }

        public static int DepositDaysNumber { get; set; }

        public static int BankStructSize = DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount + 1;


        public BankStruct[] bankstruct = new BankStruct[BankStructSize];

        private long DepositDaysDue;



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
            mod.IsReady = true;
        }

    private static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
    {
        try
        {
            AutomaticDeposit = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowAutomaticDepositing");
            BonusRate = mod.GetSettings().GetValue<float>("GeneralSettings", "BonusRate");
            DepositDaysNumber = mod.GetSettings().GetValue<int>("GeneralSettings", "DepositDays");
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

        private void Update()
        {

             if (AutomaticDeposit == true)
             {

                AUTORewardBonusDeposit();

             }
             else
             {

                RewardBonusDeposit();

             }

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
                DaggerfallBankManager.BankAccounts[index].accountGold += (int)((BonusRate / 100) * DaggerfallBankManager.BankAccounts[index].accountGold);
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
                    DaggerfallBankManager.BankAccounts[index].accountGold += (int)((BonusRate / 100) * DaggerfallBankManager.BankAccounts[index].accountGold);

                DaggerfallUI.AddHUDText("Your Bonus Gold Has Been Added To The Bank", MessageDelay);
                bankstruct[index].BankDepositDate = CurrentDate;

            if (DaysPassedFromLastPayout >= DepositDaysDue)
                bankstruct[index].RemainedDays = (DaysPassedFromLastPayout % DepositDaysDue);
            else bankstruct[index].RemainedDays = 0;

            }

        }

    #endregion

        #region SaveMethods

    
    public Type SaveDataType
        {
            get { return typeof(BanksRemasteredSaveData); }
        }

        public object NewSaveData()
        {
            return new BanksRemasteredSaveData
            {

                bankstruct = new BankStruct[BankStructSize]

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
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in RestoreSaveData: {ex.Message}");
        }


    }

    #endregion



}


