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
using Crystal.Common.Notification;
using Crystal.Common.Notification.Payload;
using Crystal.Common.PolClient;
using Crystal.POLProfile.DataObjects;
using Crystal.POLProfile.DataObjects.Pol.Group;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crystal.POLProfile
{
    class NotifierClient
    {
        private readonly string AuthServerIp;
        private readonly int AuthServerPort;
        private readonly string NotifierId;
        private readonly string NotifierPassword;

        private readonly PolIrcClient IrcClient;
        private readonly Thread SenderThread;
        private bool LoopIsAlive = false;
        private bool LoopEnded = false;
        private readonly object SyncObj = new();
        private readonly Queue<NotificationObj> QueuedNotifications = new();
        private uint SequenceNumber = 0;

        public NotifierClient(string authIp, int authPort, string polId, string polPassword)
        {
            AuthServerIp = authIp;
            AuthServerPort = authPort;
            NotifierId = polId;
            NotifierPassword = polPassword;

            IrcClient = new PolIrcClient(NotifierId, NotifierPassword, IncomingMsgCallback);
            SenderThread = new Thread(new ThreadStart(SenderLoop));
        }

        public PolIrcClient GetIrcClient()
        {
            return IrcClient;
        }

        public void NotifyPolMessage(string polProData, string polMessage)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    MessageHeader header = MessageHeader.FromB64(polMessage);
                    header.IsPolProRequest = true;
                    QueuedNotifications.Enqueue(new NotificationObj(polProData, header, null, true));
                }
            });
        }

        public void NotifyPolMail(string polProData, string fromName, string subject)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    // Get Status info
                    DbFriendStatusResult statusResult = Database.GetStatus(polProData);
                    if (statusResult == null)
                        return;

                    // Build base header
                    MessageHeader header = new()
                    {
                        SourcePolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                        FriendStatus = statusResult.FriendStatus.ToNotifyStatus(),
                        DataType = 0,
                        Type = MessageType.StatusNotification,
                        IsOnline = true,
                        IsValid = true,
                        ContentId = (ushort)statusResult.FriendStatus.CurrentContentsClass,
                        // Setup CFlags
                        SourceHandleNum = 0,
                        ControlFlag1 = 0,
                        ControlFlag2 = 1,
                        ControlFlag3 = 0
                    };

                    // Setup Payload
                    string polMailMessage = $"{fromName}\x7{subject}";
                    byte[] payload = Encoding.ASCII.GetBytes(polMailMessage);

                    QueuedNotifications.Enqueue(new NotificationObj(polProData, header, payload));
                }
            });
        }

        public void NotifyFriendStatusChange(string polProData, byte handleIndx, bool sendPicAndComment = false, bool clearStatus = false)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    // Get Status info
                    DbFriendStatusResult statusResult = Database.GetStatus(polProData);
                    if (statusResult == null)
                        return;

                    // Get List of online friends
                    List<DbFriendPolIdAndPosition> onlineFriends = Database.GetFriendPolIdAndNum(polProData, handleNum: handleIndx);
                    if (onlineFriends == null || onlineFriends.Count == 0) return;

                    FriendPayloadBuilder payloadBuilder = new(true);

                    if (sendPicAndComment)
                    {
                        Tuple<uint, string> displayPicIdAndComment = Database.GetCommentAndDisplayPicId(polProData, handleIndx);
                        payloadBuilder.DisplayPic(displayPicIdAndComment.Item1).Comment(displayPicIdAndComment.Item2);
                    }

                    byte[] payload = payloadBuilder.BuildPayload();

                    // Build base header
                    MessageHeader header = new()
                    {
                        SourcePolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                        FriendStatus = !clearStatus ? statusResult.FriendStatus.ToNotifyStatus() : new NotifyStatusData { Status = 0, ActiveCharacterBits = 0, ContentClass = 0, IsLoginAndReceiveMsg = 0, Purpose = 0 },
                        SourceHandleNum = handleIndx,
                        // Setup CFlags
                        ControlFlag1 = 1,
                        ControlFlag2 = 0,
                        ControlFlag3 = 0,
                        ContentId = (ushort)statusResult.FriendStatus.CurrentContentsClass,
                        // Data Info
                        DataType = 0x0,
                        Type = MessageType.StatusNotification,
                        Code = 0x0,
                        IsOnline = true,
                        IsValid = true,
                        // Msg Info
                        IsPolProRequest = true
                    };

                    // Get the polId a creationPosition of user from friend's list.... finish building header and queue.
                    foreach (DbFriendPolIdAndPosition polIdAndPosition in onlineFriends)
                    {
                        MessageHeader targetHeader = MessageHeader.Copy(header);
                        NotifyStatusData newStatusData = header.FriendStatus.Copy();
                        targetHeader.SourceHandleNum = handleIndx;
                        targetHeader.FriendStatus = newStatusData;
                        targetHeader.TargetPolProId = SqCrypto.PolProDataToPolId(polIdAndPosition.PolId, 0, 0);
                        targetHeader.FriendPosition = polIdAndPosition.Position;
                        targetHeader.FriendStatus.Status++; // Status off by one for some reason
                        QueuedNotifications.Enqueue(new NotificationObj(polIdAndPosition.PolId, targetHeader, payload));
                    }
                }
            });
        }

        /*
         * Portrait and comment of one account, or an empty payload when null is passed. A status
         * notice has to describe whoever it is about, not whoever triggered it.
        */
        private static byte[] BuildStatusPayload(string polProData)
        {
            FriendPayloadBuilder payloadBuilder = new(true);

            if (polProData != null)
            {
                Tuple<uint, string> displayPicIdAndComment = Database.GetCommentAndDisplayPicId(polProData);
                payloadBuilder.DisplayPic(displayPicIdAndComment.Item1).Comment(displayPicIdAndComment.Item2);
            }

            return payloadBuilder.BuildPayload();
        }

        public void NotifyFriendLinkChange(string polProData, byte? flIndex, string friendPolProData, bool sendPicAndComment = false, bool clearStatus = false, bool requireMutual = true)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    NotifyStatusData clearStatusData = new() { Status = 0, ActiveCharacterBits = 0, ContentClass = 0, IsLoginAndReceiveMsg = 0, Purpose = 0 };

                    List<DbFriendPolIdAndPosition> myEntry = Database.GetFriendPolIdAndNum(polProData, friendPolProData, requireMutual && !clearStatus);

                    byte[] myPayload = BuildStatusPayload(sendPicAndComment ? polProData : null);
                    byte[] friendPayload = BuildStatusPayload(sendPicAndComment ? friendPolProData : null);

                    // Build base header
                    MessageHeader header = new()
                    {
                        SourcePolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                        // Setup CFlags
                        ControlFlag1 = 1,
                        ControlFlag2 = 0,
                        ControlFlag3 = 0,
                        // Data Info
                        DataType = 0x0,
                        Type = MessageType.StatusNotification,
                        Code = 0x0,
                        IsOnline = true,
                        IsValid = true,
                        // Msg Info
                        IsPolProRequest = true
                    };

                    // Get Mine and Friend's Status info
                    DbFriendStatusResult myStatusResult = Database.GetStatus(polProData);
                    if (myStatusResult == null)
                        return;
                    DbFriendStatusResult friendStatusResult = Database.GetStatus(friendPolProData);
                    if (friendStatusResult == null)
                        return;

                    // Send my status to the friend. Needs their slot for me, so it waits if they
                    // have not stored me yet.
                    if (myEntry != null && myEntry.Count > 0)
                    {
                        MessageHeader targetHeaderFriend = MessageHeader.Copy(header);
                        targetHeaderFriend.FriendStatus = !clearStatus ? myStatusResult.FriendStatus.ToNotifyStatus() : clearStatusData;
                        targetHeaderFriend.SourceHandleNum = myStatusResult.FriendStatus.ActiveHandleNumber;
                        targetHeaderFriend.TargetPolProId = SqCrypto.PolProDataToPolId(friendPolProData, 0, 0);
                        targetHeaderFriend.FriendPosition = myEntry[0].Position;
                        targetHeaderFriend.ContentId = (ushort)targetHeaderFriend.FriendStatus.ContentClass;
                        targetHeaderFriend.FriendStatus.Status++; // Status off by one for some reason
                        QueuedNotifications.Enqueue(new NotificationObj(myEntry[0].PolId, targetHeaderFriend, myPayload));
                    }
                    else
                    {
                        Program.Log.Info($"{friendPolProData} has no entry for {polProData} yet, skipping their half of the status exchange.");
                    }

                    // Send the friend's status to myself, addressed as though it came from them.
                    // The caller already knows their slot in my list, so this needs no lookup.
                    if (flIndex != null)
                    {
                        MessageHeader targetHeaderSelf = MessageHeader.Copy(header);
                        targetHeaderSelf.FriendStatus = !clearStatus ? friendStatusResult.FriendStatus.ToNotifyStatus() : clearStatusData;
                        targetHeaderSelf.SourcePolProId = SqCrypto.PolProDataToPolId(friendPolProData, 0, 0);
                        targetHeaderSelf.SourceHandleNum = friendStatusResult.FriendStatus.ActiveHandleNumber;
                        targetHeaderSelf.TargetPolProId = 0;
                        targetHeaderSelf.FriendPosition = flIndex.Value;
                        targetHeaderSelf.ContentId = (ushort)targetHeaderSelf.FriendStatus.ContentClass;
                        targetHeaderSelf.FriendStatus.Status++; // Status off by one for some reason
                        QueuedNotifications.Enqueue(new NotificationObj(polProData, targetHeaderSelf, friendPayload));
                    }
                    else
                    {
                        Program.Log.Info($"{polProData} has no entry for {friendPolProData} yet, skipping my half of the status exchange.");
                    }
                }
            });
        }

        public void NotifyFriendCommentChange(string polProData, byte handleIndx, string comment)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    // Get Status info
                    DbFriendStatusResult statusResult = Database.GetStatus(polProData);
                    if (statusResult == null)
                        return;

                    // Copy comment string to byte array
                    byte[] payload = new FriendPayloadBuilder().Comment(comment).BuildPayload();

                    // Build base header
                    MessageHeader header = new()
                    {
                        SourcePolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                        SourceHandleNum = handleIndx,
                        FriendStatus = statusResult.FriendStatus.ToNotifyStatus(),
                        DataType = 0,
                        Type = MessageType.StatusNotification,
                        IsOnline = true,
                        IsValid = true,
                        ContentId = (ushort)statusResult.FriendStatus.CurrentContentsClass,
                        // Setup CFlags
                        ControlFlag1 = 1,
                        ControlFlag2 = 0,
                        ControlFlag3 = 0
                    };

                    // Get the polId a creationPosition of user from friend's list.... finish building header and queue.
                    List<DbFriendPolIdAndPosition> onlineFriends = Database.GetFriendPolIdAndNum(polProData, handleNum: handleIndx);
                    foreach (DbFriendPolIdAndPosition polIdAndPosition in onlineFriends)
                    {
                        MessageHeader targetHeader = MessageHeader.Copy(header);
                        NotifyStatusData newStatusData = header.FriendStatus.Copy();
                        targetHeader.FriendStatus = newStatusData;
                        targetHeader.TargetPolProId = SqCrypto.PolProDataToPolId(polIdAndPosition.PolId, 0, 0);
                        targetHeader.FriendPosition = polIdAndPosition.Position;
                        QueuedNotifications.Enqueue(new NotificationObj(polIdAndPosition.PolId, targetHeader, payload));
                    }
                }
            });
        }

        public void NotifyFriendPortraitChange(string polProData, byte handleIndx, uint displayPicId)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    // Get Status info
                    DbFriendStatusResult statusResult = Database.GetStatus(polProData);
                    if (statusResult == null)
                        return;

                    // Setup payload
                    byte[] payload = new FriendPayloadBuilder().DisplayPic(displayPicId).BuildPayload();

                    // Build base header
                    MessageHeader header = new()
                    {
                        SourcePolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                        SourceHandleNum = handleIndx,
                        FriendStatus = statusResult.FriendStatus.ToNotifyStatus(),
                        DataType = 0,
                        Type = MessageType.StatusNotification,
                        IsOnline = true,
                        IsValid = true,
                        ContentId = (ushort)statusResult.FriendStatus.CurrentContentsClass,
                        // Setup CFlags
                        ControlFlag1 = 1,
                        ControlFlag2 = 0,
                        ControlFlag3 = 0
                    };

                    // Get the polId a creationPosition of user from friend's list.... finish building header and queue.
                    List<DbFriendPolIdAndPosition> onlineFriends = Database.GetFriendPolIdAndNum(polProData, handleNum: handleIndx);
                    foreach (DbFriendPolIdAndPosition polIdAndPosition in onlineFriends)
                    {
                        MessageHeader targetHeader = MessageHeader.Copy(header);
                        NotifyStatusData newStatusData = header.FriendStatus.Copy();
                        targetHeader.FriendStatus = newStatusData;
                        targetHeader.FriendPosition = polIdAndPosition.Position;
                        QueuedNotifications.Enqueue(new NotificationObj(polIdAndPosition.PolId, targetHeader, payload));
                    }
                }
            });
        }

        public void NotifyFriendStatusInitLoadData(string polProData, byte handleIndx)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    List<DbFriendStatusResult> resultList = Database.GetAllFriendStatusForLogin(polProData);
                    foreach (DbFriendStatusResult result in resultList)
                    {
                        MessageHeader header = new()
                        {
                            SourcePolProId = SqCrypto.PolProDataToPolId(result.FriendPolIdData, 0, 0),
                            TargetPolProId = 0,
                            FriendStatus = result.FriendStatus.ToNotifyStatus(),
                            FriendPosition = result.MyFriendListPosition,
                            SourceHandleNum = result.FriendStatus.ActiveHandleNumber,
                            ControlFlag1 = 1,
                            ControlFlag2 = 0,
                            ControlFlag3 = 0,
                            ContentId = result.FriendStatus.CurrentContentsClass,
                            // Data Info
                            DataType = 0x0,
                            Type = MessageType.StatusNotification,
                            Code = 0x0,
                            IsOnline = true,
                            IsValid = true,
                            // Msg Info
                            IsPolProRequest = true
                        };

                        header.FriendStatus.Status++; // Status off by one for some reason

                        // Send data to logging in player
                        QueuedNotifications.Enqueue(new NotificationObj(polProData, header, result.PortraitAndCommentPayload));
                    }
                }
            });
        }

        /*
         * Every group the account holds, not just the active handle's - the client lists all four
         * at once, so a roster it never receives renders as an empty member list.
        */
        public void NotifyGroupStatusLoad(string polProData)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    foreach (ulong groupId in Database.GetGroupIds(polProData))
                        SendGroupRoster(polProData, groupId, false);
                }
            });
        }

        /*
         * Every member of one group, sent to one player. An invitee needs this because their
         * client only learns the group's id and name from the invitation message - the roster
         * has to be pushed. clearStatus sends everybody, themselves included, as offline, which
         * is what an invite that has not been accepted should look like.
        */
        public void NotifyGroupRoster(string polProData, ulong groupId, bool clearStatus = false)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    SendGroupRoster(polProData, groupId, clearStatus);
                }
            });
        }

        // Caller holds SyncObj
        private void SendGroupRoster(string polProData, ulong groupId, bool clearStatus)
        {
            List<DbGroupMemberStatusResult> members = Database.GetAllGroupMemberStatuses(groupId);

            foreach (DbGroupMemberStatusResult member in members)
            {
                GroupMember packed = new()
                {
                    ProfileId = member.HandleId,
                    Rank = member.Rank,
                    HandlePosition = member.HandlePosition
                };

                byte[] payload = new FriendPayloadBuilder(true)
                    .GroupData(groupId, packed.PackedInfo)
                    .DisplayPic(member.Portrait)
                    .Name(member.HandleName)
                    .Comment(member.Comment)
                    .BuildPayload();

                MessageHeader header = new()
                {
                    SourcePolProId = SqCrypto.PolProDataToPolId(member.PolIdData, 0, 0),
                    TargetPolProId = 0,
                    FriendStatus = !clearStatus ? member.GroupStatus.ToNotifyStatus() : new NotifyStatusData { Status = 0, ActiveCharacterBits = 0, ContentClass = 0, IsLoginAndReceiveMsg = 0, Purpose = 0 },
                    FriendPosition = 0,
                    SourceHandleNum = member.HandlePosition,
                    ControlFlag1 = 1,
                    ControlFlag2 = 0,
                    ControlFlag3 = 0,
                    ContentId = !clearStatus ? member.GroupStatus.CurrentContentsClass : (ushort)0,
                    // Data Info
                    DataType = 0x0,
                    Type = MessageType.StatusNotification,
                    Code = 0x0,
                    IsOnline = true,
                    IsValid = true,
                    // Msg Info
                    IsPolProRequest = true
                };

                QueuedNotifications.Enqueue(new NotificationObj(polProData, header, payload));
            }
        }

        public void NotifyAllGroupStatusChange(string polProData, byte handleIndx, bool sendPicAndComment = false, bool clearStatus = false)
        {
            // Groups hang off a handle, so ask for the ones belonging to the handle this notice
            // speaks for. A handle switch reaches the old set and the new set that way.
            foreach (ulong groupId in Database.GetGroupIds(polProData, handleIndx))
                NotifyGroupStatusChange(polProData, handleIndx, groupId, sendPicAndComment, clearStatus);
        }

        public void NotifyGroupStatusChange(string polProData, byte handleIndx, ulong groupId, bool sendPicAndComment = false, bool clearStatus = false)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    // Get Status info
                    DbGroupMemberStatusResult myStatus = Database.GetGroupMemberStatus(polProData, groupId, handleIndx);
                    if (myStatus == null)
                        return;
                    GroupMember packed = new()
                    {
                        ProfileId = myStatus.HandleId,
                        Rank = myStatus.Rank,
                        HandlePosition = myStatus.HandlePosition
                    };


                    // Get Members
                    List<GroupMemberNotifyTarget> onlineMembers = Database.GetGroupMemberNotifyTargets(polProData, groupId, handleIndx);
                    if (onlineMembers == null || onlineMembers.Count == 0) return;

                    FriendPayloadBuilder payloadBuilder = new(true);

                    payloadBuilder.GroupData(groupId, packed.PackedInfo);

                    if (sendPicAndComment)
                    {
                        Tuple<uint, string> displayPicIdAndComment = Database.GetCommentAndDisplayPicId(polProData, handleIndx);
                        payloadBuilder.DisplayPic(displayPicIdAndComment.Item1).Comment(myStatus.Comment);
                    }

                    byte[] payload = payloadBuilder.BuildPayload();

                    // Build base header
                    MessageHeader header = new()
                    {
                        SourcePolProId = SqCrypto.PolProDataToPolId(polProData, 0, 0),
                        FriendStatus = !clearStatus ? myStatus.GroupStatus.ToNotifyStatus() : new NotifyStatusData { Status = 0, ActiveCharacterBits = 0, ContentClass = 0, IsLoginAndReceiveMsg = 0, Purpose = 0 },
                        SourceHandleNum = handleIndx,
                        // Setup CFlags
                        ControlFlag1 = 1,
                        ControlFlag2 = 0,
                        ControlFlag3 = 0,
                        ContentId = (ushort)myStatus.GroupStatus.CurrentContentsClass,
                        // Data Info
                        DataType = 0x0,
                        Type = MessageType.StatusNotification,
                        Code = 0x0,
                        IsOnline = true,
                        IsValid = true,
                        // Msg Info
                        IsPolProRequest = true
                    };

                    // Get the polId a creationPosition of user from friend's list.... finish building header and queue.
                    foreach (GroupMemberNotifyTarget target in onlineMembers)
                    {
                        MessageHeader targetHeader = MessageHeader.Copy(header);
                        NotifyStatusData newStatusData = header.FriendStatus.Copy();
                        newStatusData.Status++; // Status off by one for some reason
                        targetHeader.SourceHandleNum = handleIndx;
                        targetHeader.FriendStatus = newStatusData;
                        targetHeader.TargetPolProId = SqCrypto.PolProDataToPolId(target.PolId, 0, 0);
                        targetHeader.FriendPosition = target.HandleNum;
                        QueuedNotifications.Enqueue(new NotificationObj(target.PolId, targetHeader, payload));
                    }
                }
            });
        }

        public void NotifyGroupMemberModify(string requesterPolProData, ulong groupId, ulong memberPolProId, byte memberHandlePosition, byte rank)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    string memberPolProData = SqCrypto.PolIdToPolProData(memberPolProId);
                    ulong profileId = Database.GetHandleIdByPosition(memberPolProData, memberHandlePosition);
                    if (profileId == 0)
                        return;

                    // Removal: capture recipients while the target is still a member.
                    // Add/rank change: apply the DB change first so a new member is included.
                    List<GroupMemberNotifyTarget> targets;
                    if (rank == GroupMember.RANK_REMOVED)
                    {
                        targets = Database.GetGroupMemberNotifyTargets(requesterPolProData, groupId);
                        Database.UpdateGroupMember(groupId, memberPolProId, memberHandlePosition, rank);
                    }
                    else
                    {
                        Database.UpdateGroupMember(groupId, memberPolProId, memberHandlePosition, rank);
                        targets = Database.GetGroupMemberNotifyTargets(requesterPolProData, groupId);
                    }

                    if (targets == null || targets.Count == 0)
                        return;

                    // An invite is news for the existing members only - the invitee hears about it
                    // through the group invitation message written to their mailbox. The rank 2 row
                    // inserted just above would otherwise put them in this list too.
                    if (rank == GroupMember.RANK_INVITED)
                        targets.RemoveAll(target => target.PolId == memberPolProData);

                    if (targets.Count == 0)
                        return;

                    GroupMember packed = new()
                    {
                        ProfileId = profileId,
                        Rank = rank,
                        HandlePosition = memberHandlePosition
                    };

                    // Rank-only event: no status flag so recipients' status bits stay untouched.
                    // The name and portrait still have to ride along or the client draws a blank
                    // row - it fills those from the payload's own blocks, not from the group data.
                    // A removal has no row left to read, and needs neither.
                    FriendPayloadBuilder payloadBuilder = new FriendPayloadBuilder(false)
                        .GroupData(groupId, packed.PackedInfo);

                    if (rank != GroupMember.RANK_REMOVED)
                    {
                        DbGroupMemberStatusResult member = Database.GetAllGroupMemberStatuses(groupId)
                            .Find(row => row.PolIdData == memberPolProData && row.HandlePosition == memberHandlePosition);

                        if (member != null)
                            payloadBuilder.DisplayPic(member.Portrait).Name(member.HandleName).Comment(member.Comment);
                        else
                            Program.Log.Warn($"No roster row for {memberPolProData} in group {groupId}, sending a nameless member notice.");
                    }

                    byte[] payload = payloadBuilder.BuildPayload();

                    MessageHeader header = new()
                    {
                        SourcePolProId = memberPolProId,
                        SourceHandleNum = memberHandlePosition,
                        ControlFlag1 = 1,
                        ControlFlag2 = 0,
                        ControlFlag3 = 0,
                        DataType = 0x0,
                        Type = MessageType.StatusNotification,
                        Code = 0x0,
                        IsOnline = true,
                        IsValid = true,
                        IsPolProRequest = true
                    };

                    foreach (GroupMemberNotifyTarget target in targets)
                    {
                        MessageHeader targetHeader = MessageHeader.Copy(header);
                        targetHeader.TargetPolProId = SqCrypto.PolProDataToPolId(target.PolId, 0, 0);
                        targetHeader.FriendPosition = target.HandleNum;
                        QueuedNotifications.Enqueue(new NotificationObj(target.PolId, targetHeader, payload));
                    }
                }
            });
        }

        public void NotifyGroupDisband(string requesterPolProData, ulong groupId)
        {
            Task.Run(() =>
            {
                lock (SyncObj)
                {
                    // Capture members before deletion; each gets a rank-1 notice about
                    // THEMSELVES, which makes their client wipe the whole group slot.
                    List<DbGroupMemberStatusResult> members = Database.GetAllGroupMemberStatuses(groupId);
                    Database.DeleteGroup(groupId);

                    foreach (DbGroupMemberStatusResult member in members)
                    {
                        // Requester's client cleans up via the disband response
                        if (member.PolIdData == requesterPolProData)
                            continue;

                        GroupMember packed = new()
                        {
                            ProfileId = member.HandleId,
                            Rank = GroupMember.RANK_REMOVED,
                            HandlePosition = member.HandlePosition
                        };

                        byte[] payload = new FriendPayloadBuilder(false)
                            .GroupData(groupId, packed.PackedInfo)
                            .BuildPayload();

                        MessageHeader header = new()
                        {
                            SourcePolProId = SqCrypto.PolProDataToPolId(member.PolIdData, 0, 0),
                            TargetPolProId = SqCrypto.PolProDataToPolId(member.PolIdData, 0, 0),
                            SourceHandleNum = member.HandlePosition,
                            ControlFlag1 = 1,
                            ControlFlag2 = 0,
                            ControlFlag3 = 0,
                            DataType = 0x0,
                            Type = MessageType.StatusNotification,
                            Code = 0x0,
                            IsOnline = true,
                            IsValid = true,
                            IsPolProRequest = true
                        };

                        QueuedNotifications.Enqueue(new NotificationObj(member.PolIdData, header, payload));
                    }
                }
            });
        }

        public uint GetNextSequenceNumber()
        {
            lock (SyncObj)
            {
                return SequenceNumber++;
            }
        }

        public void ClearQueue()
        {
            lock (SyncObj)
            {
                QueuedNotifications.Clear();
            }
        }

        public void StartNotifyLoop()
        {
            if (!SenderThread.IsAlive)
            {
                LoopIsAlive = true;
                SenderThread.Start();
            }
        }

        public void StopNotifyLoop()
        {
            if (!SenderThread.IsAlive)
                return;

            LoopIsAlive = false;
            while (!LoopEnded) ;
            IrcClient.Disconnect();
        }

        public void SenderLoop()
        {            
            DateTime lastConnectAttempt = DateTime.Now.AddDays(-1);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Program.Log.Info($"Notification sender thread started");
            Console.ForegroundColor = ConsoleColor.Gray;

            while (LoopIsAlive)
            {
                // Attempt to connect to the auth server if we happen to be disconnected.
                if (!IrcClient.IsConnected())
                {
                    if ((DateTime.Now - lastConnectAttempt).TotalSeconds > 10)
                    {
                        if (IrcClient.Connect(AuthServerIp, AuthServerPort))
                        {
                            DateTime authTimeStart = DateTime.Now;
                            while (!IrcClient.IsAuthenticated())
                            {
                                if ((DateTime.Now - authTimeStart).TotalSeconds > 5)
                                {
                                    Program.Log.Error("PolPro Notifier: Connection failed, is Auth down? Try again in 10 seconds.");
                                    lastConnectAttempt = DateTime.Now;
                                    break;
                                }
                                if (IrcClient.AuthenticationFailed())
                                {
                                    Program.Log.Error("The notification irc client could not connect to the auth server. Check settings!!!");
                                    Environment.Exit(1);
                                }
                            }
                        }
                        else
                        {
                            if (!LoopIsAlive)
                                continue;
                            Program.Log.Error("PolPro Notifier: Connection failed, is Auth down? Try again in 10 seconds.");
                            lastConnectAttempt = DateTime.Now;
                            continue;
                        }
                    }
                    else
                        continue;
                }

                bool isEmpty = false;
                lock (SyncObj)
                {
                    if (QueuedNotifications.Count == 0)
                        isEmpty = true;
                }

                if (isEmpty)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                lock (SyncObj)
                {
                    while (QueuedNotifications.Count > 0)
                    {
                        NotificationObj obj = QueuedNotifications.Dequeue();
                        MessageHeader header = obj.Header;
                        byte[] payload = obj.Payload;
                        string payloadB64 = payload != null ? SqCrypto.EncodeBase64(payload, payload.Length, false) : "";

                        //Program.Log.Info($"\n=====Sending NOTICE=====\nHEADER: {obj.Header}\nPAYLOAD:\n{Utils.ByteArrayToHex(payload)}");

                        if (!obj.DontModify)
                        {
                            header.SourcePolProId = SqCrypto.ChangeCryptPolId(header.SourcePolProId, 0, SqCrypto.POL_CRYPTKEY_MESSAGE);
                            header.TargetPolProId = SqCrypto.ChangeCryptPolId(header.TargetPolProId, 0, SqCrypto.POL_CRYPTKEY_MESSAGE);
                            header.Timestamp = Utils.UnixTimeStampUTC();
                            header.SequenceNumber = GetNextSequenceNumber();
                            header.IsPolProRequest = true;
                            header.PayloadStringSize = (uint)payloadB64.Length;
                        }

                        //Program.Log.Info($"\nHEADER BYTES: {(Utils.ByteArrayToHex(MessageHeader.ToBytes(header)))}");

                        string headerB64 = MessageHeader.ToB64(header);
                        string message = $"{headerB64}{payloadB64}";

                        IrcClient.Notice(obj.TargetPolProData, message);

                        //Program.Log.Info($"NOTICE: {message}");
                    }
                }
            }

            LoopEnded = true;
        }

        private void IncomingMsgCallback(string message)
        {
            string[] split = message.Split(' ');

            if (split.Length >= 1)
            {
                switch (split[0])
                {
                    case "OFFLINE":
                        if (split.Length == 2)
                        {
                            string polProData = split[1];
                            byte activeHandleNum = Database.GetStatus(polProData)?.FriendStatus.ActiveHandleNumber ?? 0;
                            NotifyFriendStatusChange(polProData, activeHandleNum);
                            NotifyAllGroupStatusChange(polProData, activeHandleNum);
                        }
                        return;
                }
            }
        }

        class NotificationObj(string targetPolProData, MessageHeader header, byte[] payload = null, bool dontModify = false)
        {
            public string TargetPolProData = targetPolProData;
            public MessageHeader Header = header;
            public byte[] Payload = payload;
            public bool DontModify = dontModify;
        }
    }
}
