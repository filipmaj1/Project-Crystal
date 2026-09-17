#include <std/mem.pat>

using undefined = u8;
using byte      = u8;
using word      = u16;
using dword     = u32;
using ushort    = u16;
using uint      = u32;


// Repeating type4/type10 related blocks
struct SubType {
    u16 val_u16;
    u8 val_u8;
    u8 pad[9];
};

struct charadata_t {
    s32 objType; // 0, 2, 3: Player Types. 1: NPC
    s32 unitID;
    char lobbyStr[16];
    s32 diffType;
    float x;
    float y;
    float z;
    float r;
    s32 field8_0x2c;
    u32 udpIp;
    u32 udpPort;
    float height; // Sets namplate height if it's height is set to 0.
    float mass;   // If 0, immovable object. The higher the mass the easier to push.
    float boundingRadius; // How far the user collides from model.
    float nameplateHeight; // 0 - Bottom, 1 - Middle, 2 - Top (prob default)
    u32 infoFlags; // 0x1: Nameplate On, 0x10: Can Target
    u32 field16_0x4c;
    u32 animationID;
    u32 field18_0x54;
    char firstName[17];
    char lastName[17];
    u8 sexID;
    u8 field22_0x7b;
    u8 u8_1;
    u8 padding_0x7d[3]; // field24_0x7d to field26_0x7f
    u32 field27_0x80;
    u32 field28_0x84;
    u8 field29_0x88;
    u8 unitType; // 4 has a model. Seen: 1, 2, 3, 4, 5, 6, 14, 1E.
    u8 rank;     // Rank Icon
    u8 unitTypeBasedFlag; // Set to: 0, 1, or & 7.
    ushort unknown;
    u8 padding_0x8e[10]; // field34_0x8e to field43_0x97


    SubType blocks[9]; // 0x98 through 0x103

    ushort field143_0x104; // type4Related
    byte field144_0x106;   // type4Related
    
    // Massive undefined block
    u8 padding_0x107[125]; // field145_0x107 to field269_0x183
    
    ushort field270_0x184;
    u8 padding_0x186[2];
    byte unknownChara2;
    byte unknownChara3;
    ushort headID;
    byte bodyModelID;
    byte npcOrPC;
    byte bodyBuild;
    
    u8 padding_0x18f[41]; // field280_0x18f to field320_0x1b7
    
    ushort u16_0;
    ushort u16_1;
    ushort u16_2;
    ushort u16_3;
    uint u32_1;
    uint u32_2;
};

// Placement
charadata_t charadata @ 0x00;