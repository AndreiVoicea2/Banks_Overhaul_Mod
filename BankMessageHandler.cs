using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BankExpanded;

public class BankMessageHandler
{
    public static string PrintBankQualityMessage(QualityType quality)
    {
        switch (quality)
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


    public enum MessageState
    {
        DEPOSIT_ONE_DAY = 0,
        DEPOSIT = 1,
        REWARD = 2,
        FAILED_DEPOSIT = 3,
        FAILED_LOAD_SETTINGS = 4,
        MISSED_DEPOSIT = 5,
        GET_SAVE_ERROR = 6,
        LOAD_SAVE_ERROR = 7
    }
    public static string GeneralMessageHandler(MessageState MessageCode, int MessageNumber = 0)
    {
        switch (MessageCode)
        {
            case MessageState.DEPOSIT_ONE_DAY:
                return "Bonus deposit gold in " + MessageNumber + " day";
                break;

            case MessageState.DEPOSIT:
                return "Bonus deposit gold in " + MessageNumber + " days";
                break;

            case MessageState.REWARD:
                return "Your Deposit Generated " + MessageNumber.ToString() + " Gold";
                break;

            case MessageState.FAILED_DEPOSIT:
                return "Failed Deposit";
                break;

            case MessageState.FAILED_LOAD_SETTINGS:
                return "Failed To Load Settings";
                break;

            case MessageState.MISSED_DEPOSIT:
                return "You Missed The Bonus Gold";
                break;

            case MessageState.GET_SAVE_ERROR:
                return "Failed To Get Saved Data";
                break;

            case MessageState.LOAD_SAVE_ERROR:
                return "Failed To Load Saved Data";
                break;

        }
        return "Wrong Message Code";
    }
}
