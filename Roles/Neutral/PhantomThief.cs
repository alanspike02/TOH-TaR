using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Unity.Services.Authentication.Internal;
using UnityEngine;
using TownOfHost.Modules;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral
{
    internal class PhantomThief
    {
        private static readonly int Id = 32767;
        static List<byte> playerIdList = new();

        public static OptionItem ChangeTargetRoles;
        public static readonly string[] ChangeRoles =
        {
            CustomRoles.Crewmate.ToString(), CustomRoles.PhantomThief.ToString(),
        };
        public static readonly CustomRoles[] ThRoleChangeRoles =
        {
            CustomRoles.Crewmate, CustomRoles.PhantomThief,
        };

        private static Color GetRoleColor(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static string GetRoleColorCode(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            return hexColor;
        }

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PhantomThief);
            ChangeTargetRoles = StringOptionItem.Create(Id + 11, "ChangeTargetRoles", ChangeRoles, 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PhantomThief]);
        }
        public static void ChangeRole(PlayerControl voteTarget)
        {
            voteTarget.RpcSetCustomRole(ThRoleChangeRoles[ChangeTargetRoles.GetValue()]);
        }

        public static void NoticeDead()
        {
            foreach (var allpc in Main.AllPlayerControls)
            {
                Utils.SendMessage(string.Format(GetString("PtNotice")), 255, Utils.ColorString(GetRoleColor(CustomRoles.PhantomThief), GetString("MessageFromPtNotice")));
                return;
            }
        }
    }
}