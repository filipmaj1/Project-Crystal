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
using System.Collections.Generic;
using System.Text;
using static Crystal.TetraMaster.Packets.TmPacket;

namespace Crystal.TetraMaster.Packets
{
    public class TmPacketBuilder
    {
        private string StrPacket;
        private List<TmGameCommand> GameCommands = new List<TmGameCommand>();

        public TmPacketBuilder(byte opcode1, byte opcode2, byte opcode3)
        {
            StrPacket = $"GTM0G{opcode1:X2}00{opcode2:X2}{opcode3:X2}";
        }

        public TmPacketBuilder Command(TmGameCommand command)
        {
            GameCommands.Add(command);
            return this;
        }

        public string Build()
        {
            // Append all the commands
            foreach (TmGameCommand gameCommand in GameCommands)
                StrPacket += gameCommand.ToString();

            return StrPacket;
        }
    }

    public class TmGameCommandBuilder
    {
        private string StrSoFar;

        public TmGameCommandBuilder(string name)
        {
            StrSoFar = $"@{name}=";
        }

        public TmGameCommandBuilder Param(string name, params object[] value)
        {
            StrSoFar += $"/{name}=";

            for (int i = 0; i < value.Length; i++)
            {
                if (i != 0)
                    StrSoFar += "|";

                object curVal = value[i];

                // Number
                if (curVal is int)
                    StrSoFar += ((int)curVal).ToString();
                if (curVal is uint)
                    StrSoFar += ((int)(uint)curVal).ToString();
                if (curVal is short)
                    StrSoFar += ((int)curVal).ToString();
                if (curVal is ushort)
                    StrSoFar += ((int)(ushort)curVal).ToString();
                if (curVal is byte)
                    StrSoFar += ((int)(byte)curVal).ToString();
                // String
                if (curVal is string)
                    StrSoFar += (string)curVal;
                // ID
                if (curVal is long || curVal is ulong)
                    StrSoFar += string.Format("{0:X16}", (ulong)curVal);
                // Byte Array
                if (curVal is byte[])
                    StrSoFar += BitConverter.ToString((byte[])curVal).Replace("-", "");
            }

            return this;
        }

        public TmGameCommandBuilder ParamHexStr(string name, params string[] value)
        {
            StrSoFar += $"/{name}=";

            for (int i = 0; i < value.Length; i++)
            {
                if (i != 0)
                    StrSoFar += "|";

                string curVal = value[i];
                StrSoFar += Convert.ToHexString(Encoding.ASCII.GetBytes(curVal));            
            }

            return this;
        }

        public TmGameCommand End()
        {
            return new TmGameCommand(StrSoFar);
        }
    }
}
