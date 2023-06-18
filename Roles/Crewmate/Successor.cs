using System.Collections.Generic;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Crewmate
{
    public static class Successor
    {
        static readonly int Id = 215300;
        static List<byte> playerIdList = new();
        public static OptionItem IncreaseMeetingTime;
        public static OptionItem MeetingTimeLimit;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Successor);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void OnReportDeadBody(GameData.PlayerInfo target, PlayerControl __instance)
        {
            var BodyRoles = target.GetCustomRole();
            if (!__instance.Is(CustomRoles.Successor)) return;
            if (__instance.Is(CustomRoles.Successor) && target != null)
            {
                if (BodyRoles.GetCustomRoleTypes() == CustomRoleTypes.Impostor || BodyRoles.GetCustomRoleTypes() == CustomRoleTypes.Neutral || BodyRoles == CustomRoles.Sheriff)
                {
                    __instance.RpcMurderPlayerV2(__instance);
                    Main.PlayerStates[__instance.PlayerId].SetDead();
                    return;
                }
                __instance.RpcSetCustomRole(BodyRoles);
                __instance.SyncSettings();
            }
        }
    }
}