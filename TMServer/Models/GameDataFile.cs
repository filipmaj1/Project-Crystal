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

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Crystal.TetraMaster.Models
{
    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public unsafe struct GameDataHeader
    {
        public uint FileVersion;
        public uint GameId;
        public ulong PolId;
        private fixed byte CharaNameBuff[0x10];
        public ulong HandleId;

        public const int SIZE = 0x30;

        public string CharaName
        {
            get
            {
                fixed (byte* ptr = &CharaNameBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.ASCII.GetBytes(value);
                int len = str.Length <= 0x10 ? str.Length : 0x10;
                fixed (byte* pStr = &CharaNameBuff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public unsafe struct GameDataStatsSection
    {
        public uint AverageRank;
        public uint Money;
        public ushort CardPower;
        public byte Title;
        public byte Guild;
        public ushort NumCards;
        public ushort NumCards2;
        public uint PlayersBeaten;
        public uint UnkStat1;
        public uint BiggestPrize;
        public uint AveragePrize;
        public uint GrandTotal;

        private fixed byte Reserved[0x24];

        public ushort ConsecutiveWins;
        public uint WinningStreak;
        public byte UnkStat2;
        private fixed byte NameOfMemorableWinBuff[0x20];

        public const int SIZE = 0x80;

        public string NameOfMemorableWin
        {
            get
            {
                fixed (byte* ptr = &NameOfMemorableWinBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x20));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.ASCII.GetBytes(value);
                int len = str.Length <= 0x20 ? str.Length : 0x20;
                fixed (byte* pStr = &NameOfMemorableWinBuff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public unsafe struct GameDataOptionsSection
    {
        public byte OptCardPlacement;
        public byte OptUnknown1;
        public byte OptVibration;
        public byte OptSeVolume;
        public byte OptBgmVolume;
        public byte OptTradeAccRequire;
        public byte OptDisplayRankings;
        public byte OptUnknown2;

        public uint TetRuleWager;
        public uint TetRuleDoubleUp;
        public uint TetRuleSpecialTile;
        public uint TetRuleChanceBlock;
        public uint TetRuleRotatingBlock;
        public uint TetRuleQuitMode;
        public uint TetRuleTimeLimit;
        public uint TetUnknown3;

        public uint TabObserveType;
        public uint TabCardLevelUpper;
        public uint TabCardLevelLower;
        public uint TabAUpper;
        public uint TabALower;
        public uint TabComment;
        public uint TabHasPassword;
        private fixed byte TabPasswordBuff[0xC];

        public byte OptAutoMemberDisplay;
        public byte OptAutoChatDisplay;
        public byte OptChatWindowSize;
        public byte OptChatWindowTransparency;

        private fixed byte DeckName1Buff[0x11];
        private fixed byte DeckName2Buff[0x11];

        public byte OptLinkHandles;

        public byte BadgeIcon;
        public uint VsRating;
        public uint Reserved;
        public uint VsGames;
        public uint PrizePointsAquired;

        public const int SIZE = 0x98;

        public string TabPassword
        {
            get
            {
                fixed (byte* ptr = &TabPasswordBuff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0xC));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.ASCII.GetBytes(value);
                int len = str.Length <= 0xC ? str.Length : 0xC;
                fixed (byte* pStr = &TabPasswordBuff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                }
            }
        }

        public string DeckName1
        {
            get
            {
                fixed (byte* ptr = &DeckName1Buff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.ASCII.GetBytes(value);
                int len = str.Length <= 0x16 ? str.Length : 0x10;
                fixed (byte* pStr = &DeckName1Buff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                }
            }
        }

        public string DeckName2
        {
            get
            {
                fixed (byte* ptr = &DeckName2Buff[0])
                {
                    string str = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(ptr, 0x10));
                    return str[..str.IndexOf('\0')];
                }
            }

            set
            {
                ReadOnlySpan<byte> str = Encoding.ASCII.GetBytes(value);
                int len = str.Length <= 0x16 ? str.Length : 0x10;
                fixed (byte* pStr = &DeckName2Buff[0])
                {
                    str.CopyTo(new Span<byte>(pStr, len));
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public struct GameDataCard
    {
        public ushort Portrait;
        public byte Offense;
        public byte Type;
        public byte PhysicalDefense;
        public byte MagicalDefense;
        public byte Unknown;
        public byte Directions;
        public ushort DeckPosition;

        public const int SIZE = 0xC;
    }

    [StructLayout(LayoutKind.Sequential, Size = SIZE)]
    public unsafe struct GameDataFile
    {
        public GameDataHeader Header;
        public GameDataStatsSection Stats;
        public GameDataOptionsSection Options;
        private fixed byte CardDataBuff[1000 * 0xC];

        public const int SIZE = 0x3028;
        public const uint FileVersion = 0x300;

        public byte[] CardData
        {
            get
            {
                fixed (byte* ptr = &CardDataBuff[0])
                {
                    byte[] bytes = new byte[1000 * 0xC];
                    var ptrSpan = new Span<byte>(ptr, 1000 * 0xC);
                    ptrSpan.CopyTo(bytes.AsSpan());
                    return bytes;
                }
            }

            set
            {
                ReadOnlySpan<byte> data = value;
                if (data.Length != 1000 * 0xC)
                    return;
                fixed (byte* ptr = &CardDataBuff[0])
                {
                    data.CopyTo(new Span<byte>(ptr, 1000 * 0xC));
                }
            }
        }
    }
}
