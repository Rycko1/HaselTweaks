using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselTweaks.Structs;

public unsafe partial struct Statics
{
    [MemberFunction("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 8B F8 C6 05")]
    public static partial ulong WindowProcHandler(nint hwnd, int uMsg, int wParam);

    [MemberFunction("E8 ?? ?? ?? ?? 8B 44 24 78 89 44 24 44")]
    public static partial void GetTodoArgs(EventHandler* questEventHandler, BattleChara* localPlayer, int i, uint* numHave, uint* numNeeded, uint* itemId);

    [MemberFunction("66 83 F9 1E 0F 83")]
    public static partial nint UpdateQuestWork(ushort index, nint questData, bool a3, bool a4, bool a5);

    [MemberFunction("80 F9 07 77 10")]
    public static partial byte IsGatheringPointRare(byte gatheringPointType);

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B 05 ?? ?? ?? ?? 48 8D 8C 24 ?? ?? ?? ?? 48 8B D0 E8 ?? ?? ?? ?? 8B 4E 08")]
    public static partial byte* GetGatheringPointName(RaptureTextModule** module, byte gatheringTypeId, byte gatheringPointType);

    [StaticAddress("41 0F B6 45 ?? 48 8D 0D", 8)]
    public static partial byte** ColorThemeTypePaths();
}
