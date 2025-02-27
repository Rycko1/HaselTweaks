using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

/*
 * ClientObjectIndex:
 *  0 used in Character, PvPCharacter
 *  1 used in Inspect, CharaCard
 *  2 used in TryOn, GearSetPreview
 *  3 used in Colorant
 *  4 used in BannerList, BannerEdit
 *  0 - 7 used in BannerParty
 */

[StructLayout(LayoutKind.Explicit, Size = 0x2C8)]
public unsafe partial struct CharaView : ICreatable
{
    [FieldOffset(0x8)] public CharaViewState State; // initialization state of KernelTexture, Camera etc. that happens in Render()
    [FieldOffset(0xC)] public uint ClientObjectId; // ClientObjectManager = non-networked objects, ClientObjectIndex + 40
    [FieldOffset(0x10)] public uint ClientObjectIndex;
    [FieldOffset(0x14)] public uint CameraType; // turns portrait ambient/directional lighting on/off
    [FieldOffset(0x18)] public nint CameraManager;
    [FieldOffset(0x20)] public Camera* Camera;
    //[FieldOffset(0x28)] public nint Unk28; // float CharacterRotation?
    [FieldOffset(0x30)] public AgentInterface* Agent; // for example: AgentTryOn
    //[FieldOffset(0x38)] public nint AgentCallbackReady; // if set, called when State changes to Ready
    //[FieldOffset(0x40)] public nint AgentCallback; // not investigated, used inside vf7 and vf11
    [FieldOffset(0x48)] public CharaViewCharacterData CharacterData;

    //[FieldOffset(0xB8)] public uint UnkB8;
    //[FieldOffset(0xBC)] public uint UnkBC;
    //[FieldOffset(0xC0)] public float UnkC0;
    [FieldOffset(0xC4)] public float ZoomRatio;

    [FixedSizeArray<CharaViewItem>(14)]
    [FieldOffset(0xD0)] public fixed byte Items[0x20 * 14];

    [FieldOffset(0x2B8)] public bool CharacterDataCopied;
    [FieldOffset(0x2B9)] public bool CharacterLoaded;

    public static CharaView* Create()
        => IMemorySpace.GetUISpace()->Create<CharaView>();

    [MemberFunction("E8 ?? ?? ?? ?? 41 80 A6 ?? ?? ?? ?? ?? 48 8D 05")]
    public readonly partial void Ctor();

    [VirtualFunction(0)]
    public readonly partial void Dtor(bool freeMemory);

    [VirtualFunction(1)]
    public readonly partial void Initialize(nint agent, int clientObjectId, nint agentCallbackReady);

    [VirtualFunction(2)]
    public readonly partial void Release();

    [VirtualFunction(3)]
    public readonly partial void ResetPositions();

    [VirtualFunction(4)]
    public readonly partial void Vf4(float a2);

    [VirtualFunction(5)]
    public readonly partial void Vf5(float a2);

    [VirtualFunction(6)]
    public readonly partial void Vf6(float a2, float a3);

    [VirtualFunction(7)]
    public readonly partial byte Vf7(nint a2); // called by Render()

    [VirtualFunction(8)]
    public readonly partial void Vf8(); // noop

    [VirtualFunction(9)]
    public readonly partial void Vf9(); // noop

    [VirtualFunction(10)]
    public readonly partial void Vf10(); // noop

    [VirtualFunction(11)]
    public readonly partial bool Vf11(CharaViewCharacter* obj);

    [VirtualFunction(12)]
    public readonly partial float Vf12(int a2, int a3);

    [MemberFunction("0F 10 02 0F 11 41 48")]
    public readonly partial void SetCustomizeData(CharaViewCharacterData* data);

    [MemberFunction("E8 ?? ?? ?? ?? EB 27 8B D6")]
    public readonly partial void Render(uint frameIndex);

    [MemberFunction("E8 ?? ?? ?? ?? 48 85 C0 75 05 0F 57 C9")]
    public readonly partial CharaViewCharacter* GetCharacter();

    [MemberFunction("E8 ?? ?? ?? ?? 49 8D 4F 10 88 85")]
    public readonly partial bool IsAnimationPaused();

    [MemberFunction("E8 ?? ?? ?? ?? B2 01 48 8B CE E8 ?? ?? ?? ?? 32 C0")]
    public readonly partial void ToggleAnimationPaused(bool paused);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 47 28 BA")]
    public readonly partial void UnequipGear(bool hasCharacterData = false, bool characterLoaded = true);

    [MemberFunction("E8 ?? ?? ?? ?? 45 33 DB FF C3")]
    public readonly partial void SetItemSlotData(byte slotId, uint itemId, byte stain, int a5 = 0, byte a6 = 1);

    [MemberFunction("E8 ?? ?? ?? ?? B1 01 0F B6 86")]
    public readonly partial void ToggleDrawWeapon(bool drawn);
}

public enum CharaViewState : uint
{
    Ready = 6
}

[StructLayout(LayoutKind.Explicit, Size = 0x68)]
public unsafe partial struct CharaViewCharacterData : ICreatable
{
    [FieldOffset(0)] public CustomizeData CustomizeData; // see Glamourer.Customization.CharacterCustomization
    //[FieldOffset(0x1A)] public byte Unk1A;
    //[FieldOffset(0x1B)] public byte Unk1B;
    [FieldOffset(0x1C)] public fixed uint ItemIds[14];
    [FieldOffset(0x54)] public fixed byte ItemStains[14];
    [FieldOffset(0x62)] public byte ClassJobId;
    [FieldOffset(0x63)] public bool VisorHidden;
    [FieldOffset(0x64)] public bool WeaponHidden;
    [FieldOffset(0x65)] public bool VisorClosed;
    //[FieldOffset(0x66)] public byte Unk66;
    //[FieldOffset(0x67)] public byte Unk67;

    public static CharaViewCharacterData* Create()
        => IMemorySpace.GetUISpace()->Create<CharaViewCharacterData>();

    public static CharaViewCharacterData* CreateFromLocalPlayer()
    {
        var obj = Create();
        obj->ImportLocalPlayerEquipment();
        return obj;
    }

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8D 45 10 48 8B CF")]
    public readonly partial void Ctor();

    [MemberFunction("E9 ?? ?? ?? ?? 41 0F B6 40 ?? 88 42 62")]
    public readonly partial void ImportLocalPlayerEquipment();
}

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct CharaViewItem
{
    [FieldOffset(0x0)] public byte SlotId;
    [FieldOffset(0x1)] public byte EquipSlotCategory;
    [FieldOffset(0x2)] public byte GlamourEquipSlotCategory;
    [FieldOffset(0x3)] public byte StainId;
    [FieldOffset(0x4)] public byte GlamourStainId;
    //[FieldOffset(0x5)] public byte Unk5;
    //[FieldOffset(0x6)] public byte Unk6;
    //[FieldOffset(0x7)] public byte Unk7;
    [FieldOffset(0x8)] public uint ItemId;
    [FieldOffset(0xC)] public uint GlamourItemId;
    [FieldOffset(0x10)] public WeaponModelId ModelMain;
    [FieldOffset(0x18)] public WeaponModelId ModelSub;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewCharacter // FFXIVClientStructs.FFXIV.Client.Game.Character.Character
{
    [FieldOffset(0x1A8)] public CharaViewCharacterCharacterData CharacterData;
    [FieldOffset(0x6E8)] public DrawDataContainer DrawDataContainer;
    [FieldOffset(0x920)] public CharaViewCharacterActionTimelineManager ActionTimelineManager;
    [FieldOffset(0xC60)] public CharaViewCharacterFacialAnimationManager FacialAnimationManager; // name not set in stone
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewCharacterCharacterData
{
    [MemberFunction("44 0F B6 49 ?? 88 51 3A")]
    public readonly partial void SetClassJob(short classJobId);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewCharacterFacialAnimationManager
{
    [FieldOffset(0x604)] public Vector2 HeadDirection;
    [FieldOffset(0x60C)] public Vector2 EyeDirection;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewCharacterActionTimelineManager
{
    [FieldOffset(0x80)] public CharaViewGameObjectSchedulerTimeline** BaseAnimation;

    [FieldOffset(0x310)] public ushort BannerTimelineRowId;
    [FieldOffset(0x312)] public byte BannerFacialRowId;

    [FieldOffset(0x32C)] public uint BannerTimelineAdditionalData;
    [FieldOffset(0x330)] public uint BannerTimelineIcon;
    [FieldOffset(0x334)] public ushort BannerTimelineUnlockCondition;
    [FieldOffset(0x336)] public ushort BannerTimelineSortKey;
    [FieldOffset(0x338)] public byte BannerTimelineType;

    [MemberFunction("48 8B 41 08 F6 80 ?? ?? ?? ?? ?? 74 10 84 D2")]
    public readonly partial void SetExpression(byte id);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewGameObjectSchedulerTimeline
{
    [FieldOffset(0x34)] public float CurrentTimestamp;

    [FieldOffset(0x44)] public float TbmhLength;
    [FieldOffset(0x48)] public float AnimationLength; // C010
}
