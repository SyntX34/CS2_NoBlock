using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;

namespace NoBlock;

[MinimumApiVersion(96)]
public class NoBlock : BasePlugin
{
    public override string ModuleName => "[Custom] No Block";
    public override string ModuleAuthor => "Manifest @Road To Glory & WD-, +SyntX34";
    public override string ModuleDescription => "Allows for players to walk through each other without being stopped due to colliding.";
    public override string ModuleVersion => "V. 1.1 [Beta]";

    private readonly WIN_LINUX<int> OnCollisionRulesChangedOffset = new WIN_LINUX<int>(173, 172);

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerSpawn>(Event_PlayerSpawn, HookMode.Post);
    }

    private HookResult Event_PlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (!@event.Userid.IsValid)
        {
            return HookResult.Continue;
        }
        
        CCSPlayerController player = @event.Userid;
        
        if(player.Connected != PlayerConnectedState.PlayerConnected)
        {
            return HookResult.Continue;
        }
        
        if (!player.PlayerPawn.IsValid)
        {
            return HookResult.Continue;
        }

        // Use AddTimer instead of Timer.Instance
        AddTimer(0.1f, () => PlayerSpawnNextFrame(player, player.PlayerPawn), TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private void PlayerSpawnNextFrame(CCSPlayerController player, CHandle<CCSPlayerPawn> pawn)
    {
        if (!player.IsValid || !pawn.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
            return;
        
        if (pawn.Value == null)
            return;
            
        // Use COLLISION_GROUP_DEBRIS instead of COLLISION_GROUP_DISSOLVING
        pawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
        pawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
        
        // Call the collision rules changed function
        VirtualFunctionVoid<nint> collisionRulesChanged = new VirtualFunctionVoid<nint>(pawn.Value.Handle, OnCollisionRulesChangedOffset.Get());
        collisionRulesChanged.Invoke(pawn.Value.Handle);
        
        // Add an additional timer with slight delay to ensure the collision is properly applied
        AddTimer(0.1f, () => 
        {
            if (!player.IsValid || !pawn.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
                return;
                
            if (pawn.Value == null)
                return;
                
            // Apply the collision group again after a short delay
            pawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
            pawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
            collisionRulesChanged.Invoke(pawn.Value.Handle);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }
}

public class WIN_LINUX<T>
{
    [JsonPropertyName("Windows")]
    public T Windows { get; private set; }

    [JsonPropertyName("Linux")]
    public T Linux { get; private set; }

    public WIN_LINUX(T windows, T linux)
    {
        this.Windows = windows;
        this.Linux = linux;
    }

    public T Get()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return this.Windows;
        }
        else
        {
            return this.Linux;
        }
    }
}