/*
===========================================================================
Copyright (C) 2019-2026 Project Crystal Dev Team

This file is part of Project Crystal Server.

Project Crystal Server is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Project Crystal Server is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with Project Crystal Server. If not, see <https://www.gnu.org/licenses/>.
===========================================================================
*/

using Crystal.Common;
using Crystal.Common.PolClient;
using Crystal.POLAuth.DataObjects;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Crystal.POLAuth
{
    public enum AdminControlResult
    {
        Success,
        GenericFailure,
        InvalidValue,
        CantFindPolId,
        CantFindContentId,
        MaxContentIds,
    }

    public class PolAuthAdminControl
    {
        private static string PolProIp = "";
        private static int PolProPort = 0;

        public static void SetProfileServer(string ip, int port)
        {
            PolProIp = ip;
            PolProPort = port;
        }

        public AdminControlResult AddAccount(string email, string password, out string newPolId, string forcedId = null)
        {
            newPolId = Database.CreateAccount(email, password, forcedId);
            if (newPolId == null)
            {
                return AdminControlResult.GenericFailure;
            }

            // Connect to auth server so we got authentication
            PolIrcClient authClient = new("POLA0001", "botbotbot");
            authClient.Connect("127.0.0.1", AuthServer.AUTHENTICATION_PORT);
            if (!authClient.WaitAuthentication(3000))
                return AdminControlResult.GenericFailure;

            // Create the mail file
            MailAccountData mailData = new()
            {
                Mode = 2,
                LastUpdate = Utils.UnixTimeStampUTC(),
                MainEmailName = email.Contains('@') ? email[..Math.Min(15, email.IndexOf('@'))] : email
            };
            byte[] mailDataBytes = new byte[MailAccountData.SIZE];
            MemoryMarshal.Write(mailDataBytes, mailData);

            // Profile - Write the File
            PolProfileClient polpro = new(PolProIp, PolProPort, authClient.GetMyIp(), authClient.GetMyPort(), authClient.CurrentCrypto.GetBlowKey(), authClient.CurrentCrypto.GetRKey1(), authClient.CurrentCrypto.GetRKey2());
            int result = polpro.WriteFile(SqCrypto.PolProDataToPolId(newPolId, 0, 0), "u/account", mailDataBytes, 0, 0, mailDataBytes.Length);
            authClient.Disconnect();

            // If failed roll back
            if (result != 0)
                Database.DeleteAccount(newPolId, out bool _);

            return result == 0 ? AdminControlResult.Success : AdminControlResult.GenericFailure;
        }

        public AdminControlResult DeleteAccount(string polId)
        {
            if (!Database.DeleteAccount(polId, out bool couldNotFindPolId))
            {
                if (couldNotFindPolId)
                    return AdminControlResult.CantFindPolId;
                else
                    return AdminControlResult.GenericFailure;
            }
            return AdminControlResult.Success;
        }

        public static AdminControlResult SetAccountPassword(string polId, string newPassword)
        {
            if (!Database.SetAccountPassword(polId, newPassword, out bool couldNotFindPolId))
            {
                if (couldNotFindPolId)
                    return AdminControlResult.CantFindPolId;
                else
                    return AdminControlResult.GenericFailure;
            }
            return AdminControlResult.Success;
        }

        public static AdminControlResult SetAdminData(string polId, ushort adminDataValue)
        {
            if (adminDataValue != 0 && (adminDataValue < 0xE0 || adminDataValue > 0xF0))
                return AdminControlResult.InvalidValue;

            if (!Database.SetAccountAdminData(polId, adminDataValue, out bool couldNotFindPolId))
            {
                if (couldNotFindPolId)
                    return AdminControlResult.CantFindPolId;
                else
                    return AdminControlResult.GenericFailure;
            }
            return AdminControlResult.Success;
        }

        public static AdminControlResult CreateContentId(string polId, ushort gameId, out ulong contentId)
        {
            contentId = Database.CreateContentId(polId, gameId, out bool couldNotFindPolId);
            if (contentId == 0)
            {
                contentId = 0;
                if (couldNotFindPolId)
                    return AdminControlResult.CantFindPolId;
                else
                    return AdminControlResult.GenericFailure;
            }
            return AdminControlResult.Success;
        }

        public static AdminControlResult DeleteContentId(string contentIdStr)
        {
            ulong contentId = 0;
            if (contentIdStr.StartsWith("0x") && ulong.TryParse(contentIdStr, out ulong parsedId))
                contentId = parsedId;
            else if (ulong.TryParse(contentIdStr, out ulong parsedId2))
                contentId = parsedId2;
            else
                return AdminControlResult.InvalidValue;

            if (!Database.DeleteContentId(contentId, out bool couldNotFindContentId))
            {
                if (couldNotFindContentId)
                    return AdminControlResult.CantFindContentId;
                else
                    return AdminControlResult.GenericFailure;
            }
            return AdminControlResult.Success;
        }

        public static AdminControlResult SetContentIdPaid(string contentIdStr, bool isPaid)
        {
            ulong contentId = 0;
            if (contentIdStr.StartsWith("0x") && ulong.TryParse(contentIdStr, out ulong parsedId))
                contentId = parsedId;
            else if (ulong.TryParse(contentIdStr, out ulong parsedId2))
                contentId = parsedId2;
            else
                return AdminControlResult.InvalidValue;
                
            if (!Database.SetContentIdPaid(contentId, isPaid, out bool couldNotFindContentId))
            {
                 if (couldNotFindContentId)
                    return AdminControlResult.CantFindContentId;
                else
                    return AdminControlResult.GenericFailure;
            }
            return AdminControlResult.Success;
        }

        public static AdminControlResult GetUserInfo(string polId, out string userOutStr)
        {
            userOutStr = Database.GetUserInfoAndBuildString(polId, out bool couldNotFindPolId);
            if (userOutStr == null)
            {
                if (couldNotFindPolId)
                    return AdminControlResult.CantFindPolId;
                else
                    return AdminControlResult.GenericFailure;
            }
            return AdminControlResult.Success;
        }

        public static List<string> GetUserList()
        {
            return Database.GetUserListForOutput();
        }

        public static bool IsGameIdValid(ushort gameId)
        {
            switch (gameId)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 10:
                case 1000:
                    return true;
                default:
                    return false;
            }
        }

        public static string GetGameIdName(ushort gameId)
        {
            switch (gameId)
            {
                case 1:
                    return "FINAL FANTASY XI";
                case 2:
                    return "Tetra Master";
                case 3:
                    return "Janhourou";
                case 4:
                    return "Front Mission Online";
                case 10:
                    return "Dirge of Cerberus: FINAL FANTASY VII";
                case 11:
                    return "Fantasy Earth: Ring of Dominion";
                case 13:
                    return "EverQuest II";
                case 15:
                    return "Dirge of Cerberus: FINAL FANTASY VII";
                case 1000:
                    return "PlayOnline Viewer";
                case 1001:
                    return "PlayOnline Chat";
                default:
                    return "Unknown";
            }
        }

        public static string GetOnlineStatusName(byte status)
        {
            switch (status)
            {
                case 0:
                    return "Offline";
                case 1:
                    return "Online";
                case 2:
                    return "AFK";
                case 5:
                    return "Invisible";
                default:
                    return "Unknown";
            }
        }
    }
}