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

namespace Crystal.Mahjong
{
    public class MjConstants
    {
        // Hardcoded PolIds
        public const ulong POLID_BALANCER     = 0x00000113405d1b2c; // pp0001
        public const ulong POLID_PROFILE      = 0x0000011341108228; // pp0002
        public const ulong POLID_AUCTION      = 0x0000015c3c898227; // pp0003
        public const ulong POLID_RANK         = 0x000000dc8475c22a; // pp0004

        // Opcodes
        public enum Opcodes
        {
            MjISAY                      = 0x00,
            MjISAYGALLEY                = 0x01,
            MjPLAYREQ                   = 0x02,
            MjPLAYCANCEL                = 0x03,
            MjRESERVEACK                = 0x04,
            MjMEMBERLISTREQ             = 0x05,
            MjGALLEYREQ                 = 0x06,
            MjGALLEYLEAVEREQ            = 0x07,
            MjMEMBERBANISH              = 0x08,
            MjCHMASTER                  = 0x09,
            MjTBLCONFSTART              = 0x0A,
            MjGAMESTART                 = 0x0B,
            MjMASTERCMDACK              = 0x0C,
            MjTBLCONFALL                = 0x0D,
            MjNOTICEMEMBER              = 0x0E,
            MjNOTICECANCEL              = 0x0F,
            MjNOTICECALL                = 0x10,
            MjNOTICECALLACK             = 0x11,
            MjNOTICEGAMESTART           = 0x12,
            MjNOTICETIMEUPWARNING       = 0x13,
            MjNOTICEGAMESETUP           = 0x14,
            MjNOTICESERVERQUIT          = 0x15,
            MjLEAVEROOM                 = 0x16,
            MjENTERROOM                 = 0x17,
            MjLEAVECONTENTS             = 0x18,
            MjENTERCONTENTS             = 0x19,
            MjMOVEACK                   = 0x1A,
            MjTGMPING                   = 0x1B,
            MjTGMPONG                   = 0x1C,
            MjLEAVEGAME                 = 0x1D,
            MjLEAVEGAMEACK              = 0x1E,
            MjENTERGAME                 = 0x1F,
            MjENTERGAMEACK              = 0x20,
            MjREADY                     = 0x21,
            MjHAIPAI                    = 0x22,
            MjHAIPAIACK                 = 0x23,
            MjTSUMO                     = 0x24,
            MjSUTE                      = 0x25,
            MjNAKI                      = 0x26,
            MjNAKIACK                   = 0x27,
            MjYAKUDISP                  = 0x28,
            MjYAKUDISPACK               = 0x29,
            MjSEISAN                    = 0x2A,
            MjSEISANACK                 = 0x2B,
            MjGAMEEND                   = 0x2C,
            MjBYE                       = 0x2D,
            MjALLDATA                   = 0x2E,
            MjALLDATAACK                = 0x2F,
            MjGALLEYACK                 = 0x30,
            MjSASHIUMASTART             = 0x31,
            MjSASHIUMAREQUEST           = 0x32,
            MjSASHIUMASELECT            = 0x33,
            MjSASHIUMAARGEE             = 0x34,
            MjSASHIUMARESULT            = 0x35,
            MjGAMERESULT                = 0x36,
            MjGAMERESULTACK             = 0x37,
            MjGAMERESULTHALF1           = 0x38,
            MjGAMERESULTHALF1ACK        = 0x39,
            MjGAMERESULTHALF2           = 0x3A,
            MjGAMERESULTHALF2ACK        = 0x3B,
            MjMEMBERLEAVE               = 0x3C,
            MjMEMBERLEAVEANSER          = 0x3D,
            MjHAIPAIDEBUG               = 0x3E,
            MjHAIPAIDEBUGACK            = 0x3F,
            MjGETLNDV                   = 0x40,
            MjGETLNDVACK                = 0x41,
            MjREADYSTATUS               = 0x42,
            MjCHECKSAVEDATA             = 0x43,
            MjCHECKSAVEDATAACK          = 0x44,
            MjCHATMEMBERADD             = 0x45,
            MjCHATMEMBERDEL             = 0x46,
            MjCHATMEMBERACK             = 0x47,
            MjCHATINFOMSG               = 0x48,
            UdCONNECT                   = 0x49,
            UdCONNECTACK                = 0x4A,
            UdDISCONNECT                = 0x4B,
            UdDISCONNECTACK             = 0x4C,
            UdGETSAVEDATA               = 0x4D,
            UdGETSAVEDATASTATUS         = 0x4E,
            UdGETSAVEDATAACK            = 0x4F,
            UdSETSAVEDATA               = 0x50,
            UdSETSAVEDATASTATUS         = 0x51,
            UdSETSAVEDATAACK            = 0x52,
            UdSAVEDATAPACKET            = 0x53,
            UdDETECT                    = 0x54,
            UdDETECTACK                 = 0x55,
            UdPLAYERLIST                = 0x56,
            UdPLAYERLISTACK             = 0x57,
            EmENTRYEVENT                = 0x58,
            EmENTRYEVENTACK             = 0x59,
            EmEVENTOPENCLOSE            = 0x5A,
            EmREPORTRANKINGID           = 0x5B,
            EmISEVENT                   = 0x5C,
            EmISEVENTACK                = 0x5D,
            CmNOTICEMSGOPEN             = 0x5E,
            CmNOTICEMSGCLOSE            = 0x5F,
            CmNOTICEJANHOLOWKICK        = 0x60,
            EmEVENTPARTICIPANTADD       = 0x61,
            EmEVENTPARTICIPANTADDACK    = 0x62,
            EmGETEVENTPARTICIPANT       = 0x63,
            EmGETEVENTPARTICIPANTACK    = 0x64
        }
    }
}

