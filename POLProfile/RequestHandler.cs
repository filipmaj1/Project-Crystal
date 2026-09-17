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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Crystal.POLProfile.DataObjects.Pol;
using Crystal.POLProfile.Packets;
using Crystal.POLProfile.DataObjects.Pol.Friend;
using Crystal.POLProfile.Packets.Receive;
using Crystal.POLProfile.DataObjects.Pol.Group;
using System.Runtime.InteropServices;
using Crystal.POLProfile.DataObjects.Pol.Handle;
using Crystal.POLProfile.DataObjects.Pol.Character;
using Crystal.POLProfile.DataObjects.Pol.Files;
using Crystal.POLProfile.DataObjects.Pol.Status;
using Crystal.POLProfile.DataObjects;
using Crystal.POLProfile.DataObjects.Pol.Database;
using System.Text;
using Crystal.Common.Notification;
using Crystal.POLProfile.PolDb;

namespace Crystal.POLProfile
{
    class RequestHandler
    {
        private readonly PolProServer Server;
        private readonly NotifierClient NotifierClient;
        private readonly Dictionary<ushort, Action<Client, byte[]>> RequestHandlers = [];

        public RequestHandler(PolProServer server, NotifierClient notifierClient)
        {
            Server = server;
            NotifierClient = notifierClient;
            RequestHandlers[0x008] = StoreHandleList;
            RequestHandlers[0x009] = LoadHandleList;
            RequestHandlers[0x103] = LoadCharacterList;
            RequestHandlers[0x10A] = StoreCharacterList;
            RequestHandlers[0x10B] = SearchPolId;               // Unknown Request
            RequestHandlers[0x203] = LoadFriendList;
            RequestHandlers[0x206] = StoreFriendList;
            RequestHandlers[0x300] = ReadFile;
            RequestHandlers[0x301] = WriteFile;
            RequestHandlers[0x302] = DeleteFile;
            RequestHandlers[0x303] = GetFileList;
            RequestHandlers[0x304] = WriteFileMulti;        
            RequestHandlers[0x400] = ChangeMyStatus;            // Unknown Request
            RequestHandlers[0x401] = LoadMyStatus;              // Unknown Request
            RequestHandlers[0x403] = UpdateComment;
            RequestHandlers[0x404] = UnknownStatusRequest;      // Unknown Request
            RequestHandlers[0x405] = UpdateStatus;
            RequestHandlers[0x406] = GetMyStatus;
            RequestHandlers[0x407] = VerifySELogin;
            RequestHandlers[0x501] = PolbesUpdateRecord;
            RequestHandlers[0x503] = PolbesSearchDataAll;
            RequestHandlers[0x504] = PolbesSearchData;
            RequestHandlers[0x701] = CreateGroup;
            RequestHandlers[0x702] = DisbandGroup;
            RequestHandlers[0x703] = ModifyGroupMembers;
            RequestHandlers[0x70B] = ChangeMyGroupSettings;
            RequestHandlers[0x70C] = LoadGroupList;

        }
        private void LoadHandleList(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} loading handle list.");
            // Get the handles
            List<HandleData> handleList = Database.LoadHandleList(client.GetPolProData());
            ReadOnlySpan<HandleData> handleListSpan = new([.. handleList]);
            int count = handleList.Count;

            // Build the response
            Span<byte> responseObj = new byte[handleList.Count * HandleData.SIZE + 8];
            MemoryMarshal.Write(responseObj, in count);                         // Count
            MemoryMarshal.AsBytes(handleListSpan).CopyTo(responseObj[0x8..]);   // HandleData List

            client.SendAnswer(responseObj.ToArray());
        }

        private void StoreHandleList(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} storing handle list.");
            if (recvObj.Length == 0x648)
            {
                Span<byte> handleOrderList = recvObj.AsSpan()[..0x40];
                ReadOnlySpan<HandleUpdate> updatedHandles = MemoryMarshal.Cast<byte, HandleUpdate>(recvObj.AsSpan().Slice(0x40, 0x600));
                ulong[] updatedPrimitives = new ulong[0x40];

                Database.UpdateHandlePositions(client.GetPolProData(), handleOrderList);

                //Look for Updates or Deletes
                for (int i = 0; i < 0x40; i++)
                {
                    // Skip
                    if (updatedHandles[i].Mode == 0)
                        continue;
                    // Update
                    else if (updatedHandles[i].Mode == 1)
                    {
                        ulong handleId = Database.StoreHandle(client.GetPolProData(), updatedHandles[i]);
                        updatedPrimitives[i] = 0x100000000000 | handleId;
                    }
                    // Delete
                    else if (updatedHandles[i].Mode == 2)
                    {
                        Database.DeleteHandle(client.GetPolProData(), updatedHandles[i].CreationPosition);
                    }
                }

                // Response is all the handle bitfields at once
                byte[] responseObj = new byte[0x204];
                MemoryMarshal.AsBytes(updatedPrimitives.AsSpan()).CopyTo(responseObj.AsSpan());
                client.SendAnswer(responseObj);
            }
        }

        private void LoadCharacterList(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} loading character list.");
            // Get the content ids (Characters)
            List<CharacterData> characterList = Database.LoadCharacterList(client.GetPolProData());
            ReadOnlySpan<CharacterData> CharacterDataSpan = new([.. characterList]);
            int count = characterList.Count;

            // Build the response
            Span<byte> responseObj = new byte[characterList.Count * CharacterData.SIZE + 8];
            MemoryMarshal.Write(responseObj, in count);                            // Count
            MemoryMarshal.AsBytes(CharacterDataSpan).CopyTo(responseObj[0x8..]);   // Character List

            client.SendAnswer(responseObj.ToArray());
        }

        private void StoreCharacterList(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} storing character list.");

            if (recvObj.Length == 0x248)
            {
                ReadOnlySpan<byte> characterOrderList = recvObj.AsSpan()[..0x40];
                ReadOnlySpan<CharacterUpdate> characterUpdateList = MemoryMarshal.Cast<byte, CharacterUpdate>(recvObj.AsSpan()[0x40..]);
                Database.UpdateCharacterOrder(client.GetPolProData(), characterOrderList);
                Database.UpdateCharacterLinks(client.GetPolProData(), characterUpdateList);
                client.SendAnswer();
            }
            else
                client.SendAnswer(errorCode: 0xFF);
        }

        private void SearchPolId(Client client, byte[] recvObj)
        {
            // Never seen
            throw new NotImplementedException();
        }

        private void LoadFriendList(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} loading friend list.");
            // Get the friend list
            List<FriendData> friendList = Database.LoadFriendList(client.GetPolProData());
            ReadOnlySpan<FriendData> friendListSpan = new([.. friendList]);
            int count = friendList.Count;

            // Build the response
            Span<byte> responseObj = new byte[friendList.Count * FriendData.SIZE + 8];
            MemoryMarshal.Write(responseObj, in count);                         // Count
            MemoryMarshal.AsBytes(friendListSpan).CopyTo(responseObj[0x8..]);   // Friend List

            client.SendAnswer(responseObj.ToArray());
        }

        private void StoreFriendList(Client client, byte[] recvObj)
        {
            Program.Log.Info(Utils.ByteArrayToHex(recvObj));
            Program.Log.Info($"{client.GetPolProData()} storing friend list.");
            if (recvObj.Length < FriendRequestHeader.SIZE)
            {
                Program.Log.Warn($"{client.GetPolProData()} sent a friend list store shorter than the header.");
                client.SendAnswer(errorCode: 0xFF);
                return;
            }

            // One header for the whole request, then the changed entries, then an 8 byte trailer
            ReadOnlySpan<FriendRequestHeader> header = MemoryMarshal.Cast<byte, FriendRequestHeader>(recvObj);
            int numUpdates = header[0].NumberOfFriendUpdates;
            Span<FriendData> requestedFLChange = MemoryMarshal.Cast<byte, FriendData>(recvObj.AsSpan()[FriendRequestHeader.SIZE..]);
            int numEntries = Math.Min(numUpdates, requestedFLChange.Length);
            List<FriendData> updatedFriends = [];

            // Go through each friend and update the DB
            for (int i = 0; i < numEntries; i++)
            {
                FriendData friend = requestedFLChange[i];

                // Are we adding/replacing or are we deleting?
                if (friend.IsValid)
                {             
                    bool success = Database.StoreFriend(client.GetPolProData(), friend);

                    // We need to get the friend again as the missing data gets filled in automatically.
                    FriendData updatedFriend = Database.LoadSpecificFriend(client.GetPolProData(), friend.CreationPosition);

                    if (success && updatedFriend.IsFriendList && !updatedFriend.Temporary)
                    {
                        NotifierClient.NotifyFriendLinkChange(client.GetPolProData(), updatedFriend.CreationPosition, updatedFriend.PolProData, true);
                    }

                    updatedFriends.Add(updatedFriend);
                }
                else
                {
                    // A delete carries only the slot and the state, so ask the DB who was sitting there
                    FriendData removedFriend = Database.LoadSpecificFriend(client.GetPolProData(), friend.CreationPosition);
                    NotifierClient.NotifyFriendLinkChange(client.GetPolProData(), friend.CreationPosition, removedFriend.PolProData, false, true);
                    Database.DeleteFriend(client.GetPolProData(), friend);
                    updatedFriends.Add(friend);
                }
            }

            // Send success response
            Span<byte> responseObj = new byte[0xC + (FriendData.SIZE * numEntries)];
            MemoryMarshal.Write(responseObj, in numEntries);                                        // Count
            MemoryMarshal.AsBytes(updatedFriends.ToArray().AsSpan()).CopyTo(responseObj[0x8..]);    // HandleData List
            client.SendAnswer(responseObj.ToArray());
        }

        private void ReadFile(Client client, byte[] recvObj)
        {
            FileOperation request = MemoryMarshal.Cast<byte, FileOperation>(recvObj)[0];
            string polId = SqCrypto.PolIdToPolProData(request.PolProId, out _, out byte domain);
            string filePath = Path.Combine(Server.GetPathFromDomain(domain), polId, request.FilePath);

            Program.Log.Info($"{client.GetPolProData()} reading file: [ID: {polId}, Domain: {domain}] \"{request.FilePath}\".");

            try
            {
                FileInfo fi = new(filePath);
                long fileSize = fi.Length;
                fileSize = fileSize < request.Length ? fileSize : request.Length;

                byte[] fileData = new byte[fileSize];
                using FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Read(fileData, (int)request.Offset, (int)fileSize);
                client.SendAnswer(fileData);
            }
            catch (IOException)
            {
                client.SendAnswer(null, 0x7E);
                //client.SendAnswer(null, 0xFF);
                return;
            }
        }

        private void WriteFile(Client client, byte[] recvObj)
        {
            Program.Log.Info(Utils.ByteArrayToHex(recvObj));
            FileOperation request = MemoryMarshal.Cast<byte, FileOperation>(recvObj.AsSpan()[0..FileOperation.SIZE_WRITE])[0];
            ReadOnlySpan<byte> fileData = recvObj.AsSpan()[FileOperation.SIZE_WRITE..];
            string polId = SqCrypto.PolIdToPolProData(request.PolProId, out _, out byte domain);

            // Messages carry their header base64'd in the last path segment: "O/m/<0x60 chars>"
            string b64Header = null;
            MessageHeader header = default;
            if (request.FilePath.StartsWith('O'))
            {
                string segment = request.FilePath[(request.FilePath.LastIndexOf('/') + 1)..];
                if (segment.Length >= 0x60)
                {
                    byte[] headerBytes = SqCrypto.DecodeBase64(segment, 0x60);
                    b64Header = segment;
                    header = MessageHeader.FromBytes(headerBytes);

                    Program.Log.Debug("===Notice Header===");
                    Program.Log.Debug("\n" + Utils.ByteArrayToHex(headerBytes));
                }
            }

            // Group message: PolProId is a groups.id, not a profile id
            if (b64Header != null && header.Type == MessageType.GroupMessage)
            {
                WriteGroupMessageFile(client, request, fileData, b64Header, domain);
                return;
            }
            // Friend accepted, remove temp flag
            else if (b64Header != null && header.Type == MessageType.FriendAccept)
            {
                ConfirmFriendAccept(client, header);
            }
            // A group invite and its acceptance are message-only - the client never sends a
            // ModifyGroupMembers for either, so this is where the roster actually changes.
            else if (b64Header != null && header.Type == MessageType.GroupInvite)
            {
                // Refused: don't file the message either, or they get an invitation they cannot accept
                byte inviteError = ConfirmGroupMemberChange(client, header, fileData, GroupMember.RANK_INVITED);
                if (inviteError != 0)
                {
                    client.SendAnswer(errorCode: inviteError);
                    return;
                }
            }
            else if (b64Header != null && header.Type == MessageType.GroupAccept)
            {
                ConfirmGroupMemberChange(client, header, fileData, GroupMember.RANK_NORMAL);
            }
            else if (b64Header != null && header.Type == MessageType.GroupDecline)
            {
                ConfirmGroupMemberChange(client, header, fileData, GroupMember.RANK_REMOVED);
            }


            string filePath = Path.Combine(Server.GetPathFromDomain(domain), polId, request.FilePath);

            Program.Log.Info($"{client.GetPolProData()} writing file: [ID: {polId}, Domain: {domain}] \"{request.FilePath}\".");

            // Write File
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, fileData.ToArray());

            // Notify Msg
            if (b64Header != null)
                NotifierClient.NotifyPolMessage(polId, b64Header);

            client.SendAnswer();
        }

        /*
         * Body a type 0x0D message has to carry, or the client crashes reading it:
         *
         *   "<title>\x07<text>\0" followed by a block of at least 0x54 bytes:
         *      +0x04  string A, max 0x32 chars + NUL - must be non-empty when mode is 0
         *      +0x37  string B, max 0x17 chars + NUL - must be non-empty when mode is 0
         *      +0x4F  mode: 0 = open (memcpys both strings), 2 = cancel (ignores them)
         *      +0x50  uint id
         *
         * sqPlayOnlineMessageGetBodyInfo splits the body on the \x07 and the NUL and hands the
         * remainder over as the block; it returns success even when there is no remainder, and
         * the caller reads block+0x4F without checking. Title caps at 0x7F, text at 0xFFF.
        */
        private static byte[] BuildType0DBody(string title, string body, string channel, string gmName, byte mode, uint id)
        {
            byte[] head = BuildTextBody(title, body);
            byte[] aBytes = Encoding.ASCII.GetBytes(channel);
            byte[] bBytes = Encoding.ASCII.GetBytes(gmName);

            byte[] bytes = new byte[head.Length + 0x54];
            head.CopyTo(bytes, 0);
            int block = head.Length;            // the text's NUL ends the head, the block follows

            aBytes.AsSpan(0, Math.Min(aBytes.Length, 0x32)).CopyTo(bytes.AsSpan(block + 0x04));
            bBytes.AsSpan(0, Math.Min(bBytes.Length, 0x17)).CopyTo(bytes.AsSpan(block + 0x37));
            bytes[block + 0x4F] = mode;
            BitConverter.GetBytes(id).CopyTo(bytes, block + 0x50);

            return bytes;
        }

        /*
         * "<title>\x07<text>\0" - the plain body every other type carries. The classifier drops
         * any message whose payload holds no \x07, or whose strlen is longer than the header's
         * PayloadStringSize, so assign the returned length to PayloadStringSize. The trailing NUL
         * counts towards it, the way the client itself sizes its bodies.
        */
        private static byte[] BuildTextBody(string title, string text)
        {
            byte[] titleBytes = Encoding.ASCII.GetBytes(title);
            byte[] textBytes = Encoding.ASCII.GetBytes(text);

            byte[] body = new byte[titleBytes.Length + 1 + textBytes.Length + 1];
            titleBytes.CopyTo(body, 0);
            body[titleBytes.Length] = 0x07;     // title / text separator
            textBytes.CopyTo(body, titleBytes.Length + 1);
            body[^1] = 0x00;

            return body;
        }

        /*
         * Both clients store their side of a friend request as a temporary entry, so neither
         * store confirms anything. The accept message the accepter writes to the requester is
         * the only point the server learns the link is real: promote both rows out of temp and
         * exchange status. The message itself still gets written by the caller.
        */
        private void ConfirmFriendAccept(Client client, MessageHeader header)
        {
            string sourcePolProData = SqCrypto.PolIdToPolProData(SqCrypto.ChangeCryptPolId(header.SourcePolProId, 0, SqCrypto.POL_CRYPTKEY_MESSAGE));
            string targetPolProData = SqCrypto.PolIdToPolProData(SqCrypto.ChangeCryptPolId(header.TargetPolProId, 0, SqCrypto.POL_CRYPTKEY_MESSAGE));

            // Only the sender gets to confirm their own link
            if (sourcePolProData != client.GetPolProData())
            {
                Program.Log.Warn($"{client.GetPolProData()} sent a friend accept claiming to be from {sourcePolProData}.");
                return;
            }

            Program.Log.Info($"{sourcePolProData} accepted a friend request from {targetPolProData}.");

            // Clear temp first - the notifier only reports a link both sides hold for real
            Database.RemoveTempFlag(targetPolProData, sourcePolProData);
            Database.RemoveTempFlag(sourcePolProData, targetPolProData);

            // The accepter's own entry for the requester. The accept message is itself the
            // proof of a mutual link, so this does not demand one - and a missing row now only
            // costs the accepter their own notice, not the requester's.
            List<DbFriendPolIdAndPosition> myEntry = Database.GetFriendPolIdAndNum(targetPolProData, sourcePolProData, false);
            byte? myFlIndex = myEntry != null && myEntry.Count > 0 ? myEntry[0].Position : null;

            NotifierClient.NotifyFriendLinkChange(sourcePolProData, myFlIndex, targetPolProData, true, false, false);
        }

        /*
         * A group invite (0x0E) and its acceptance (0x0F) never reach ModifyGroupMembers - the
         * client sends them purely as messages - so the roster change has to happen here. The
         * group is named in the message body, in the block that follows the text:
         * "<title>\x07<text>\0" + { ulong groupId; char groupName[0x14]; }, flagged by DataType
         * 0x0A. On an invite the subject is the recipient; on an accept it is the sender.
         *
         * Returns 0 when the request stands, otherwise the error byte the caller has to answer
         * with instead of filing the message. polerr codes are 5200 + byte.
        */
        private byte ConfirmGroupMemberChange(Client client, MessageHeader header, ReadOnlySpan<byte> fileData, byte rank)
        {
            ulong sourcePolProId = SqCrypto.ChangeCryptPolId(header.SourcePolProId, 0, SqCrypto.POL_CRYPTKEY_MESSAGE);
            ulong targetPolProId = SqCrypto.ChangeCryptPolId(header.TargetPolProId, 0, SqCrypto.POL_CRYPTKEY_MESSAGE);
            string sourcePolProData = SqCrypto.PolIdToPolProData(sourcePolProId);
            string targetPolProData = SqCrypto.PolIdToPolProData(targetPolProId);

            // Only the sender gets to change a roster on their own behalf
            if (sourcePolProData != client.GetPolProData())
            {
                Program.Log.Warn($"{client.GetPolProData()} sent a group message claiming to be from {sourcePolProData}.");
                return 0xFF;
            }

            if (!TryReadGroupId(fileData, out ulong groupId))
            {
                Program.Log.Warn($"{sourcePolProData} sent a group {(rank == GroupMember.RANK_INVITED ? "invite" : "accept")} with no group block in the body.");
                return 0xFF;
            }

            bool isInvite = rank == GroupMember.RANK_INVITED;

            string inviterPolProData = isInvite ? sourcePolProData : targetPolProData;
            ulong memberPolProId = isInvite ? targetPolProId : sourcePolProId;
            byte memberHandleNum = isInvite ? header.ReceiverHandleNumber : header.SenderHandleNumber;
            string memberPolProData = isInvite ? targetPolProData : sourcePolProData;

            string action = rank == GroupMember.RANK_INVITED ? "invited to" : rank == GroupMember.RANK_NORMAL ? "joined" : "declined";
            Program.Log.Info($"{sourcePolProData} {action} group {groupId} [Member: {memberPolProData}, Handle: {memberHandleNum}].");

            if (rank == GroupMember.RANK_INVITED)
            {
                List<DbGroupMemberStatusResult> roster = Database.GetAllGroupMemberStatuses(groupId);

                if (roster.Exists(row => row.PolIdData == memberPolProData))
                {
                    Program.Log.Info($"{sourcePolProData} invited {memberPolProData} to group {groupId} on handle {memberHandleNum}, but that account is already in the group.");
                    return 0x75;    // 5317 - already invited or awaiting confirmation
                }

                if (roster.Count >= GroupMember.MAX_MEMBERS)
                {
                    Program.Log.Info($"{sourcePolProData} invited {memberPolProData} to group {groupId}, which already holds {roster.Count} members.");
                    return 0x78;    // 5320 - a group can only have up to 64 members
                }

                // Their client only has four group slots for the whole account and silently drops
                // an invite past that, so refuse it where the inviter can still be told why.
                if (Database.GetGroupCount(memberPolProData) >= GroupSettings.MAX_GROUPS)
                {
                    Program.Log.Info($"{sourcePolProData} invited {memberPolProData} to group {groupId}, but that account already holds {GroupSettings.MAX_GROUPS} groups.");
                    return 0x73;    // 5315 - you can only join up to 4 groups
                }

                Database.UpdateGroupMember(groupId, memberPolProId, memberHandleNum, rank);
            }

            NotifierClient.NotifyGroupMemberModify(inviterPolProData, groupId, memberPolProId, memberHandleNum, rank);

            if (rank == GroupMember.RANK_INVITED)
                NotifierClient.NotifyGroupRoster(memberPolProData, groupId, true);

            if (rank != GroupMember.RANK_NORMAL)
                return 0;

            // Now a real member: push their status to the group, and pull the group's back to them
            NotifierClient.NotifyGroupStatusChange(memberPolProData, memberHandleNum, groupId, true);
            NotifierClient.NotifyGroupStatusLoad(memberPolProData);

            return 0;
        }

        /*
         * Reads the group id out of a message body: skip the title up to \x07, skip the text up
         * to its NUL, then the block starts with the id. Mirrors BuildGroupNoticeBody.
        */
        private static bool TryReadGroupId(ReadOnlySpan<byte> fileData, out ulong groupId)
        {
            groupId = 0;

            int separator = fileData.IndexOf((byte)0x07);
            if (separator < 0)
                return false;

            ReadOnlySpan<byte> afterTitle = fileData[(separator + 1)..];
            int terminator = afterTitle.IndexOf((byte)0x00);
            if (terminator < 0)
                return false;

            ReadOnlySpan<byte> block = afterTitle[(terminator + 1)..];
            if (block.Length < sizeof(ulong))
                return false;

            groupId = BitConverter.ToUInt64(block);
            return true;
        }


        private void WriteGroupMessageFile(Client client, FileOperation request, ReadOnlySpan<byte> fileData, string b64Header, byte domain)
        {
            ulong groupId = request.PolProId;
            List<DbGroupMemberStatusResult> members = Database.GetAllGroupMemberStatuses(groupId);

            Program.Log.Info($"{client.GetPolProData()} writing group message: [Group: {groupId}, Members: {members.Count}] \"{request.FilePath}\".");

            if (members.Count == 0)
            {
                Program.Log.Warn($"{client.GetPolProData()} sent a message to group {groupId}, which has no members.");
                client.SendAnswer(errorCode: 0xFF);
                return;
            }

            byte[] data = fileData.ToArray();

            foreach (DbGroupMemberStatusResult member in members)
            {
                string filePath = Path.Combine(Server.GetPathFromDomain(domain), member.PolIdData, request.FilePath);
                Program.Log.Info($"    -> [ID: {member.PolIdData}, Domain: {domain}]");

                // Write File
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, data);

                // Notify
                NotifierClient.NotifyPolMessage(member.PolIdData, b64Header);
            }

            client.SendAnswer();
        }

        private void DeleteFile(Client client, byte[] recvObj)
        {
            FileOperation request = MemoryMarshal.Cast<byte, FileOperation>(recvObj)[0];
            string polId = SqCrypto.PolIdToPolProData(request.PolProId, out _, out byte domain);
            string filePath = Path.Combine(Server.GetPathFromDomain(domain), polId, request.FilePath);

            Program.Log.Info($"{client.GetPolProData()} deleting file: [ID: {polId}, Domain: {domain}] \"{request.FilePath}\".");

            try
            {
                File.Delete(filePath);
            }
            catch (IOException)
            {
                Program.Log.Warn($"{client.GetPolProData()} tried to delete a non-existing file: {request.FilePath}");
            }
            finally
            {
                client.SendAnswer();
            }
        }

        private void GetFileList(Client client, byte[] recvObj)
        {
            FileOperation request = MemoryMarshal.Cast<byte, FileOperation>(recvObj)[0];
            string polId = SqCrypto.PolIdToPolProData(request.PolProId, out _, out byte domain);
            string filePath = Path.Combine(Server.GetPathFromDomain(domain), polId, request.FilePath);

            Program.Log.Info($"{client.GetPolProData()} getting file list: [ID: {polId}, Domain: {domain}] \"{request.FilePath}\".");

            DirectoryInfo dirInfo = new(filePath);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            FileInfo[] files = dirInfo.GetFiles();

            client.SendAnswer(ObjectBuilders.CreateGetFilesObject(files));
        }

        private void WriteFileMulti(Client client, byte[] recvObj)
        {
            FileWriteMultiOperation request = MemoryMarshal.Cast<byte, FileWriteMultiOperation>(recvObj.AsSpan()[0..FileWriteMultiOperation.SIZE])[0];
            ReadOnlySpan<byte> fileData = recvObj.AsSpan().Slice(FileWriteMultiOperation.SIZE, (int) request.Length);

            // Check if > 20, that is a hard max
            if (request.NumTargets > 20)
            {
                client.SendAnswer(null, 0xFF);
                return;
            }

            // Write the file for each and notify
            for (int i = 0; i < request.NumTargets; i++)
            {
                // Get data
                ulong polId = request.GetTargetPolId(i);
                string polProData = SqCrypto.PolIdToPolProData(polId, out _, out byte domain);
                string filePath = Path.Combine(Server.GetPathFromDomain(domain), polProData, request.FilePath);
                Program.Log.Info($"{client.GetPolProData()} writing multiple files: [{i}][ID: {polId}, Domain: {domain}] \"{request.FilePath}\".");

                // Write File
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, fileData.ToArray());

                // Notify
                if (request.FilePath.StartsWith('O'))
                {
                    string b64Header = request.FilePath[(request.FilePath.LastIndexOf('/') + 1)..];
                    NotifierClient.NotifyPolMessage(polProData, b64Header);
                }
            }

            client.SendAnswer();
        }

        private void UpdateComment(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} updating comment.");
            
            string comment = Encoding.Unicode.GetString(recvObj.AsSpan());
            int end = comment.IndexOf('\0');
            if (end >= 0)
                comment = comment[..end];

            Database.UpdateComment(client.GetPolProData(), comment);

            client.SendAnswer();

            byte activeHandleNum = Database.GetStatus(client.GetPolProData())?.FriendStatus.ActiveHandleNumber ?? 0;
            NotifierClient.NotifyFriendCommentChange(client.GetPolProData(), activeHandleNum, comment);
        }

        private void ChangeMyStatus(Client client, byte[] recvObj)
        {
            StatusInfo status = MemoryMarshal.Cast<byte, StatusInfo>(recvObj.AsSpan().Slice(0, StatusInfo.SIZE))[0];

            client.SendAnswer(null, 0xFF);
            throw new NotImplementedException();
        }

        private void LoadMyStatus(Client client, byte[] recvObj)
        {
            client.SendAnswer(null, 0xFF);
            throw new NotImplementedException();
        }

        private void UnknownStatusRequest(Client client, byte[] recvObj)
        {
            client.SendAnswer(null, 0xFF);
            throw new NotImplementedException();
        }

        private void UpdateStatus(Client client, byte[] recvObj)
        {
            // Unknown 0x10 struct
            StatusInfo status = MemoryMarshal.Cast<byte, StatusInfo>(recvObj.AsSpan().Slice(0x10, StatusInfo.SIZE))[0];

            // Check if we are coming online... need to send friend notices
            DbFriendStatusResult result = Database.GetStatus(client.GetPolProData());            

            // Swapping handles is a logout on the old one and a login on the new one as far as
            // friends and group members are concerned - both lists are keyed by handle.
            byte previousHandleNum = result?.FriendStatus.ActiveHandleNumber ?? status.ActiveHandleNumber;
            bool handleChanged = result != null && previousHandleNum != status.ActiveHandleNumber;

            // Update our status
            Database.UpdateStatus(client.GetPolProData(), status);

            // Get all online friend's status info and update your friends if we came online
            if (handleChanged)
            {
                // Friend links and group memberships name a handle, so everyone tied to the one we
                // just left would sit on our last status forever. Clear it, then come online on the
                // new handle exactly like a fresh login does.
                NotifierClient.NotifyFriendStatusChange(client.GetPolProData(), previousHandleNum, clearStatus: true);
                NotifierClient.NotifyAllGroupStatusChange(client.GetPolProData(), previousHandleNum, clearStatus: true);

                NotifierClient.NotifyFriendStatusInitLoadData(client.GetPolProData(), status.ActiveHandleNumber);
                NotifierClient.NotifyGroupStatusLoad(client.GetPolProData());
                NotifierClient.NotifyFriendStatusChange(client.GetPolProData(), status.ActiveHandleNumber);
                NotifierClient.NotifyAllGroupStatusChange(client.GetPolProData(), status.ActiveHandleNumber);
            }
            else if (result != null && result.FriendStatus.CurrentContentsClass == 0)
            {
                NotifierClient.NotifyFriendStatusInitLoadData(client.GetPolProData(), status.ActiveHandleNumber);
                NotifierClient.NotifyGroupStatusLoad(client.GetPolProData());
                NotifierClient.NotifyFriendStatusChange(client.GetPolProData(), status.ActiveHandleNumber);
                NotifierClient.NotifyAllGroupStatusChange(client.GetPolProData(), status.ActiveHandleNumber);
            }
            else
            {
                NotifierClient.NotifyFriendStatusChange(client.GetPolProData(), status.ActiveHandleNumber);
                NotifierClient.NotifyAllGroupStatusChange(client.GetPolProData(), status.ActiveHandleNumber);
            }

            // Random Value Binary returned; acts as a password for content
            byte[] randomValue = new byte[0x10];
            RandomNumberGenerator.Fill(randomValue);
            byte[] contentsHash = SqCrypto.PolGenerateContentsAuthHashCode(randomValue, client.GetLoginTime(), Utils.SwapEndian(client.GetCrypto().GetBlowKeyUInt64()));
            Program.Log.Info($"\nRandValue: {BitConverter.ToString(randomValue)}\n, ContentHash: {BitConverter.ToString(contentsHash)}\n");
            Database.SaveContentsAuth(client.GetPolProData(), randomValue, contentsHash);

            // Send Random Value Binary back to the client
            byte[] response = new byte[0x1C];
            Array.Copy(randomValue, response, 0x10);
            client.SendAnswer(response);

            Program.Log.Info($"{client.GetPolProData()} updating status - Content Class: {status.CurrentContentsClass}, Active Handle: {status.ActiveHandleNumber}, Online Status: {status.OpenStat}");
        }

        private void GetMyStatus(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} getting their status.");
            DbFriendStatusResult result = Database.GetStatus(client.GetPolProData());
            if (result != null)
            {
                StatusData status = result.FriendStatus;
                Span<byte> responseObj = new byte[0x7C];
                MemoryMarshal.Write(responseObj, in status);
                client.SendAnswer(responseObj.ToArray());
            }
        }

        private void VerifySELogin(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} performing SquareEnix login.");
            SELoginRequest _ = MemoryMarshal.Cast<byte, SELoginRequest>(recvObj.AsSpan())[0];
            client.SendAnswer(new byte[8]);
        }

        private void PolbesUpdateRecord(Client client, byte[] recvObj)
        {
            ReadOnlySpan<byte> obj = recvObj.AsSpan();

            // Read the update arguments
            DatabaseUpdateColsHeader updateHeader = MemoryMarshal.Cast<byte, DatabaseUpdateColsHeader>(obj)[0];
            List<PolBesColumnArgument> updateArgs = PolBesDb.ReadColumnArgument(updateHeader.ContentClass, updateHeader.NumColumns, obj[DatabaseUpdateColsHeader.SIZE..], out int bytesRead);

            // Read the where arguments
            DatabaseWhereColsHeader whereHeader = MemoryMarshal.Cast<byte, DatabaseWhereColsHeader>(obj[(DatabaseUpdateColsHeader.SIZE + bytesRead)..])[0];
            List<PolBesColumnArgument> whereArgs = PolBesDb.ReadColumnArgument(whereHeader.ContentClass, whereHeader.NumColumns, obj[(DatabaseUpdateColsHeader.SIZE + bytesRead + DatabaseWhereColsHeader.SIZE)..], out int _);

            // Update the DB
            Program.Log.Info($"{client.GetPolProData()} updating polbes record for content class: {updateHeader.ContentClass}.");
            PolBesDb.UpdatePolBES(client.GetPolProData(), updateHeader.Visibility, updateHeader.ContentClass, updateArgs, whereArgs);

            // Send display pic push
            if (updateHeader.ContentClass == 1000 && whereArgs.Exists(item => item.Column.PolBESName.Equals("z_pnum") && item.Column.Type == Datatype.BINARY))
            {
                byte handleNum = (byte) whereArgs.Where(item => item.Column.PolBESName.Equals("z_pnum")).First().Value;
                NotifierClient.NotifyFriendStatusChange(client.GetPolProData(), handleNum, true);
                NotifierClient.NotifyAllGroupStatusChange(client.GetPolProData(), handleNum, true);
            }

            client.SendAnswer();
        }

        private void PolbesSearchDataAll(Client client, byte[] recvObj)
        {
            // Parse request
            DatabaseSearchAllRequest searchRequest = MemoryMarshal.Cast<byte, DatabaseSearchAllRequest>(recvObj.AsSpan()[..DatabaseSearchAllRequest.SIZE])[0];
            List<PolBesColumnArgument> whereArgs = PolBesDb.ReadColumnArgument(searchRequest.WhereHeader.ContentClass, searchRequest.WhereHeader.NumColumns, recvObj.AsSpan()[DatabaseSearchAllRequest.SIZE..], out int _);

            Program.Log.Info($"{client.GetPolProData()} performing polbes search all for content class: {searchRequest.WhereHeader.ContentClass}.");
            Program.Log.Info("\n\n" + Utils.ByteArrayToHex(recvObj));

            // Get request data from the DB
            byte[] result = PolBesDb.SearchPolBES(client.GetPolProData(), searchRequest.WhereHeader.ContentClass, whereArgs, out int numResults);
            if (result == null)
            {
                client.SendAnswer(null, 0xFF);
                return;
            }

            // Write result to the search selector
            int index = client.GetNextSearchIndex();
            if (numResults != 0)
            {
                string searchResultPath = Path.Combine(".", "profile_data", "playonline", SqCrypto.PolIdToPolProData(client.GetPolProId()), "u", "s", $"select{index}");
                string directoryPath = Path.GetDirectoryName(searchResultPath);
                Directory.CreateDirectory(directoryPath);
                File.WriteAllBytes(searchResultPath, result);
            }

            // Send success response
            DatabaseSearchResult resultObj = new()
            {
                SelectFileIndex = index,
                NumResults = numResults,
                IsOverflow = 0
            };

            Span<byte> responseObj = new byte[DatabaseSearchResult.SIZE];
            MemoryMarshal.Write(responseObj, in resultObj);
            client.SendAnswer(responseObj.ToArray());
        }
        
        private void PolbesSearchData(Client client, byte[] recvObj)
        {
            // Parse request
            DatabaseSearchRequest searchRequest = MemoryMarshal.Cast<byte, DatabaseSearchRequest>(recvObj)[0];
            List<PolBesColumnArgument> whereArgs = PolBesDb.ReadColumnArgument(searchRequest.WhereHeader.ContentClass, searchRequest.WhereHeader.NumColumns, recvObj.AsSpan()[DatabaseSearchRequest.SIZE..], out int _);

            Program.Log.Info($"{client.GetPolProData()} performing polbes search for content class: {searchRequest.WhereHeader.ContentClass}.");
            Program.Log.Info("\n\n" + Utils.ByteArrayToHex(recvObj));

            // Get request data from the DB
            byte[] result = PolBesDb.SearchPolBES(client.GetPolProData(), searchRequest.WhereHeader.ContentClass, whereArgs, out int numResults);
            if (result == null || numResults == 0)
            {
                client.SendAnswer(null, 0x7C); // No profile found. Profiles are deleted if subid is deleted.
                return;
            }

            // Send the result data as a obj
            client.SendAnswer(result);
        }

        private void CreateGroup(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} requesting to create a group.");

            GroupCreateRequest request = GroupCreateRequest.FromBytes([.. recvObj]);
            long id = Database.CreateGroup(client.GetPolProData(), request.Name);

            // Success
            if (id > 0)
            {
                byte[] response = new byte[0xc];
                using (MemoryStream mstream = new(response))
                using (BinaryWriter writer = new(mstream))
                {
                    writer.Write((UInt64)id);
                }
                client.SendAnswer(response);
            }
            // Maximum groups
            else if (id == -3)
                client.SendAnswer(errorCode: 0x73);
            // Name already in use
            else if (id == -2)
                client.SendAnswer(errorCode: 0x74);
        }

        private void DisbandGroup(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} requesting to disband a group.");

            GroupDeleteRequest request = GroupDeleteRequest.FromBytes([.. recvObj]);

            if (Database.GetGroupMemberRank(request.GroupId, client.GetPolProData()) != GroupMember.RANK_MASTER)
            {
                client.SendAnswer(errorCode: 0xFF);
                return;
            }

            // Grab the roster and the name before the notifier deletes the group out from under us
            List<DbGroupMemberStatusResult> members = Database.GetAllGroupMemberStatuses(request.GroupId);
            string groupName = Database.GetGroups(client.GetPolProData(), out _).Find(group => group.Id == request.GroupId).Name;

            NotifierClient.NotifyGroupDisband(client.GetPolProData(), request.GroupId);

            SendGroupDisbandMessages(client.GetPolProData(), request.GroupId, groupName, members);

            client.SendAnswer();
        }

        private void SendGroupDisbandMessages(string requesterPolProData, ulong groupId, string groupName, List<DbGroupMemberStatusResult> members)
        {
            DbGroupMemberStatusResult sender = members.Find(member => member.PolIdData == requesterPolProData);
            byte[] body = BuildGroupNoticeBody(groupName, $" ", groupId, groupName);
            ulong senderPolId = SqCrypto.ChangeCryptPolId(SqCrypto.PolProDataToPolId(requesterPolProData, 0, 0), 0, SqCrypto.POL_CRYPTKEY_MESSAGE);

            foreach (DbGroupMemberStatusResult member in members)
            {
                // The requester's own client tears the group down off the disband response
                if (member.PolIdData == requesterPolProData)
                    continue;

                MessageHeader header = new()
                {
                    SourcePolProId = senderPolId,
                    TargetPolProId = SqCrypto.ChangeCryptPolId(SqCrypto.PolProDataToPolId(member.PolIdData, 0, 0), 0, SqCrypto.POL_CRYPTKEY_MESSAGE),
                    Sender = sender?.HandleName ?? "",
                    Subject = groupName,
                    SenderHandleNumber = sender?.HandlePosition ?? 0,
                    ReceiverHandleNumber = member.HandlePosition,
                    Type = MessageType.GroupDisband,
                    DataType = 0x0A,
                    Code = 0,
                    IsValid = true,
                    DoNotReply = true,
                    ContentId = 1000,
                    Timestamp = Utils.UnixTimeStampUTC(),
                    SequenceNumber = NotifierClient.GetNextSequenceNumber(),
                    PayloadStringSize = (uint)body.Length
                };

                string b64Header = MessageHeader.ToB64(header);
                string filePath = Path.Combine(Server.GetPathFromDomain(0), member.PolIdData, $"O/m/{b64Header}");

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, body);

                NotifierClient.NotifyPolMessage(member.PolIdData, b64Header);
            }
        }

        private void SendGroupRemovedMessage(string requesterPolProData, ulong groupId, string groupName, DbGroupMemberStatusResult sender,
                                             string targetPolProData, byte targetHandleNum)
        {
            byte[] body = BuildGroupNoticeBody(groupName, $" ", groupId, groupName);
            ulong senderPolId = SqCrypto.ChangeCryptPolId(SqCrypto.PolProDataToPolId(requesterPolProData, 0, 0), 0, SqCrypto.POL_CRYPTKEY_MESSAGE);

            MessageHeader header = new()
            {
                SourcePolProId = senderPolId,
                TargetPolProId = SqCrypto.ChangeCryptPolId(SqCrypto.PolProDataToPolId(targetPolProData, 0, 0), 0, SqCrypto.POL_CRYPTKEY_MESSAGE),
                Sender = sender?.HandleName ?? "",
                Subject = groupName,
                SenderHandleNumber = sender?.HandlePosition ?? 0,
                ReceiverHandleNumber = targetHandleNum,
                Type = MessageType.GroupRemoved,
                DataType = 0x0A,
                Code = 0,
                IsValid = true,
                DoNotReply = true,
                ContentId = 1000,
                Timestamp = Utils.UnixTimeStampUTC(),
                SequenceNumber = NotifierClient.GetNextSequenceNumber(),
                PayloadStringSize = (uint)body.Length
            };

            string b64Header = MessageHeader.ToB64(header);
            string filePath = Path.Combine(Server.GetPathFromDomain(0), targetPolProData, $"O/m/{b64Header}");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, body);

            NotifierClient.NotifyPolMessage(targetPolProData, b64Header);
        }

        /*
         * "<title>\x07<text>\0" + { ulong groupId; char groupName[0x14]; } - the body shape the
         * group notice types need. Requires DataType 0x0A or 0x0B on the header.
        */
        private static byte[] BuildGroupNoticeBody(string title, string text, ulong groupId, string groupName)
        {
            byte[] head = BuildTextBody(title, text);
            byte[] nameBytes = Encoding.UTF8.GetBytes(groupName);

            byte[] body = new byte[head.Length + 0x1C];
            head.CopyTo(body, 0);
            int block = head.Length;

            BitConverter.GetBytes(groupId).CopyTo(body, block);
            nameBytes.AsSpan(0, Math.Min(nameBytes.Length, 0x13)).CopyTo(body.AsSpan(block + 8));

            return body;
        }

        private void ModifyGroupMembers(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} requesting to modify group members.");

            GroupChangeMembersRequest request = GroupChangeMembersRequest.FromBytes([.. recvObj]);

            string requesterPolProData = client.GetPolProData();
            string targetPolProData = SqCrypto.PolIdToPolProData(request.MemberPolProId);
            bool targetIsSelf = targetPolProData == requesterPolProData;

            byte myRank = Database.GetGroupMemberRank(request.GroupId, requesterPolProData);
            if (myRank == 0)
            {
                client.SendAnswer(errorCode: 0xFF);     // requester not in group
                return;
            }

            bool allowed = request.Rank switch
            {
                GroupMember.RANK_REMOVED => targetIsSelf || myRank >= GroupMember.RANK_SUBMASTER,
                GroupMember.RANK_INVITED => !targetIsSelf && myRank >= GroupMember.RANK_SUBMASTER,
                GroupMember.RANK_NORMAL => !targetIsSelf && myRank >= GroupMember.RANK_SUBMASTER,
                GroupMember.RANK_SUBMASTER or GroupMember.RANK_MASTER => !targetIsSelf && myRank == GroupMember.RANK_MASTER,
                _ => false
            };

            if (!allowed)
            {
                client.SendAnswer(errorCode: 0xFF);     // permission denied
                return;
            }

            // Grab the requester's row and the name before the notifier applies the rank change
            DbGroupMemberStatusResult sender = Database.GetGroupMemberStatus(requesterPolProData, request.GroupId);
            string groupName = Database.GetGroups(requesterPolProData, out _).Find(group => group.Id == request.GroupId).Name;

            NotifierClient.NotifyGroupMemberModify(requesterPolProData, request.GroupId, request.MemberPolProId, request.HandlePosition, request.Rank);

            // Leaving under your own steam doesn't need one, that client cleans up off the response
            if (request.Rank == GroupMember.RANK_REMOVED && !targetIsSelf)
                SendGroupRemovedMessage(requesterPolProData, request.GroupId, groupName, sender, targetPolProData, request.HandlePosition);

            client.SendAnswer();
        }

        private void ChangeMyGroupSettings(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} requesting to modify group settings.");

            GroupChangeSettingsRequest request = GroupChangeSettingsRequest.FromBytes([.. recvObj]);

            bool success = Database.SetGroupSettings(client.GetPolProData(), request.Id, request.Comment, request.OnlineStatus, request.HandlePosition);

            NotifierClient.NotifyGroupStatusChange(client.GetPolProData(), request.HandlePosition, request.Id, true);

            client.SendAnswer();
        }

        private void LoadGroupList(Client client, byte[] recvObj)
        {
            Program.Log.Info($"{client.GetPolProData()} loading group list.");

            // Get Data + Structs
            List<GroupSettings> groupData = Database.GetGroups(client.GetPolProData(), out byte[] memberCounts);
            List<GroupMember> groupMembers = Database.GetGroupMembers(groupData.Select(item => item.Id).ToArray());
            GroupCountHeader header = new((byte)groupData.Count, memberCounts);

            // Build the response
            Span<byte> responseObj = new byte[8 + (GroupSettings.SIZE * groupData.Count) + (GroupMember.SIZE * groupMembers.Count)];
            MemoryMarshal.Write(responseObj, in header);                                                                                                        // Header
            MemoryMarshal.AsBytes(groupData.ToArray().AsSpan()).CopyTo(responseObj[GroupCountHeader.SIZE..]);                                                   // Settings
            MemoryMarshal.AsBytes(groupMembers.ToArray().AsSpan()).CopyTo(responseObj[(GroupCountHeader.SIZE + (GroupSettings.SIZE * groupData.Count))..]);     // Members

            client.SendAnswer(responseObj.ToArray());
        }

        public void HandleRequest(Client client, ushort opcode, byte[] obj = null)
        {
            if (obj != null)
                Program.Log.Debug($"Got Request:\n Opcode: {opcode:X}, Data:\n{Utils.ByteArrayToHex(obj)}");
            else
                Program.Log.Debug($"Got Request:\n Opcode: {opcode:X}", opcode);

            if (RequestHandlers.TryGetValue(opcode, out Action<Client, byte[]> value))
                value(client, obj);
        }
    }
}

