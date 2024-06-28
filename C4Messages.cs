using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core.Translations;

namespace C4MessagesPlugin;
public class C4MessagesPlugin : BasePlugin
{
    public override string ModuleName => "C4 Messages";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Kento";
    public override string ModuleDescription => "C4 Messages.";

    float fDetonateTime = 0.0f;
    float fDefuseEndTime = 0.0f;
    CCSPlayerController? cDefusingClient = null;
    bool bCurrentlyDefusing = false;

    [GameEventHandler]
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        fDetonateTime = Server.CurrentTime + ConVar.Find("mp_c4timer")!.GetPrimitiveValue<int>();
        cDefusingClient = null;
        bCurrentlyDefusing = false;

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnBombExploded(EventBombExploded @event, GameEventInfo info)
    {
        float fTimeRemaining = fDefuseEndTime - fDetonateTime;

        if (cDefusingClient is not null && fTimeRemaining >= 0.0f)
        {
            string sTimeRemaining = fTimeRemaining.ToString("0.00");
            string sPlayerName = cDefusingClient.PlayerName;
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                player.PrintToChat(Localizer["BombExplodedTimeLeftMessage", sPlayerName, sTimeRemaining]);
            }
        }
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        if (@event.Userid is null || !@event.Userid.IsValid) return HookResult.Continue;

        string sTimeRemaining = (fDetonateTime - Server.CurrentTime).ToString("0.00");
        string sPlayerName = @event.Userid.PlayerName;

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            player.PrintToChat(Localizer["SuccessfulDefuseTimeLeftMessage", sPlayerName, sTimeRemaining]);
        }
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnBombBegindefuse(EventBombBegindefuse @event, GameEventInfo info)
    {
        if (@event.Userid is null || !@event.Userid.IsValid) return HookResult.Continue;

        bool bHasKit = @event.Haskit;

        float fEndTime = Server.CurrentTime + (bHasKit ? 5.0f : 10.0f);
        bCurrentlyDefusing = true;

        if (cDefusingClient is null || fDefuseEndTime < fDetonateTime)
        {
            fDefuseEndTime = fEndTime;
            cDefusingClient = @event.Userid;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnBombAbortdefuse(EventBombAbortdefuse @event, GameEventInfo info)
    {
        if (@event.Userid is null || !@event.Userid.IsValid) return HookResult.Continue;

        if (@event.Userid == cDefusingClient) {
            bCurrentlyDefusing = false;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        fDetonateTime = 0.0f;
        fDefuseEndTime = 0.0f;
        cDefusingClient = null;
        bCurrentlyDefusing = false;

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event.Userid != cDefusingClient || !bCurrentlyDefusing)    return HookResult.Continue;
        if (@event.Userid is null || !@event.Userid.IsValid)            return HookResult.Continue;
        if (@event.Attacker is null || !@event.Attacker.IsValid)        return HookResult.Continue;

        float fTimeRemaining = fDefuseEndTime - Server.CurrentTime;
        string sPlayerName = @event.Userid.PlayerName;

        if (fTimeRemaining > 0.0f)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                player.PrintToChat(Localizer["DefuserDiedTimeLeftMessage", sPlayerName, fTimeRemaining.ToString("0.00")]);
            }
        }
        else
        {
            fTimeRemaining *= -1.0f;

            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                player.PrintToChat(Localizer["PostDefuseKillTimeMessage", sPlayerName, fTimeRemaining.ToString("0.00")]);
            }
        }

        return HookResult.Continue;
    }
}