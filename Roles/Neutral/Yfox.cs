using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Hazel;
using static TownOfHost.Options;
using UnityEngine;
using TownOfHost.Modules;
using static TownOfHost.Translator;
using static UnityEngine.GraphicsBuffer;
using MS.Internal.Xml.XPath;

namespace TownOfHost.Roles.Neutral {
    public static class Yfox
    {
        private static readonly int Id = 904550;
        private static List<byte> playerIdList = new();

        public static OptionItem YfoxCooldown;
        private static OptionItem YfoxDuration;
        private static OptionItem YfoxNotice;

        private static Dictionary<byte, long> InvisTime = new();
        private static Dictionary<byte, long> lastTime = new();
        private static Dictionary<byte, int> ventedId = new();
        public static List<PlayerControl> targetList = new();
        private static bool YfoxCanNotice;

        private static readonly HashSet<byte> TargetList = new();
        private static readonly Dictionary<byte, Color> TargetColorlist = new();
        private static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
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
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Yfox);
            YfoxCooldown = FloatOptionItem.Create(Id + 2, "YfoxCooldown", new(1f, 999f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yfox])
                .SetValueFormat(OptionFormat.Seconds);
            YfoxDuration = FloatOptionItem.Create(Id + 4, "YfoxDuration", new(1f, 999f, 1f), 15f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yfox])
                .SetValueFormat(OptionFormat.Seconds);
            YfoxNotice= BooleanOptionItem.Create(Id + 6, "YfoxNotice", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Yfox])
            .SetValueFormat(OptionFormat.Seconds);
            OverrideTasksData.Create(Id + 20, TabGroup.NeutralRoles, CustomRoles.Yfox);
        }
        public static void Init()
        {
            playerIdList = new();
            InvisTime = new();
            lastTime = new();
            ventedId = new();
            TargetList.Clear();
            TargetColorlist.Clear();
            YfoxCanNotice = YfoxNotice.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (target.Is(CustomRoleTypes.Impostor)) continue;
                else if (target.IsNeutralKiller()) continue;
                if (target.GetCustomRole() is CustomRoles.GM) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
        private static void SendRPC(PlayerControl pc)
        {
            if (pc.AmOwner) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetYfoxTimer, SendOption.Reliable, pc.GetClientId());
            writer.Write((InvisTime.TryGetValue(pc.PlayerId, out var x) ? x : -1).ToString());
            writer.Write((lastTime.TryGetValue(pc.PlayerId, out var y) ? y : -1).ToString());
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            InvisTime = new();
            lastTime = new();
            long invis = long.Parse(reader.ReadString());
            long last = long.Parse(reader.ReadString());
            byte playerId = reader.ReadByte();
            bool add = reader.ReadBoolean();
            if (invis > 0) InvisTime.Add(PlayerControl.LocalPlayer.PlayerId, invis);
            if (last > 0) lastTime.Add(PlayerControl.LocalPlayer.PlayerId, last);
            if (add)
                LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            else
                LocateArrow.RemoveAllTarget(playerId);
        }
        public static bool CanGoInvis(byte id)
            => GameStates.IsInTask && !InvisTime.ContainsKey(id) && !lastTime.ContainsKey(id);
        public static bool IsInvis(byte id) => InvisTime.ContainsKey(id);

        private static long lastFixedTime = 0;
        public static void AfterMeetingTasks()
        {
            lastTime = new();
            InvisTime = new();
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => playerIdList.Contains(x.PlayerId)))
            {
                lastTime.Add(pc.PlayerId, Utils.GetTimeStamp());
                SendRPC(pc);
            }
        }

        private static bool IsFoxTarget(PlayerControl target) => IsEnable && (target.Is(CustomRoleTypes.Impostor) || (target.IsNeutralKiller()) || (target.Is(CustomRoles.Sheriff)));
        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!GameStates.IsInTask || !IsEnable) return;

            var now = Utils.GetTimeStamp();

            if (lastTime.TryGetValue(player.PlayerId, out var time) && time + (long)YfoxCooldown.GetFloat() < now)
            {
                lastTime.Remove(player.PlayerId);
                if (!player.IsModClient()) player.Notify(("YfoxCanVent"));
                SendRPC(player);
            }

            if (lastFixedTime != now)
            {
                lastFixedTime = now;
                Dictionary<byte, long> newList = new();
                List<byte> refreshList = new();
                foreach (var it in InvisTime)
                {
                    var pc = Utils.GetPlayerById(it.Key);
                    if (pc == null) continue;
                    var remainTime = it.Value + (long)YfoxDuration.GetFloat() - now;
                    if (remainTime < 0)
                    {
                        lastTime.Add(pc.PlayerId, now);
                        pc?.MyPhysics?.RpcBootFromVent(ventedId.TryGetValue(pc.PlayerId, out var id) ? id : Main.LastEnteredVent[pc.PlayerId].Id);
                        NameNotifyManager.Notify(pc, ("YfoxInvisStateOut"));
                        SendRPC(pc);
                        continue;
                    }
                    else if (remainTime <= 10)
                    {
                        if (!pc.IsModClient()) pc.Notify(string.Format(("YfoxInvisStateCountdown"), remainTime));
                    }
                    newList.Add(it.Key, it.Value);
                }
                InvisTime.Where(x => !newList.ContainsKey(x.Key)).Do(x => refreshList.Add(x.Key));
                InvisTime = newList;
                refreshList.Do(x => SendRPC(Utils.GetPlayerById(x)));
            }
            foreach (var target in Main.AllAlivePlayerControls)
            {
                var foxId = player.PlayerId;
                if (!IsFoxTarget(target) || !player.GetPlayerTaskState().IsTaskFinished || !player.Is(CustomRoles.Yfox)) continue;

                var targetId = target.PlayerId;
                NameColorManager.Add(foxId, targetId);

                TargetArrow.Add(foxId, targetId);

                //ターゲットは共通なので2回登録する必要はない
                if (!TargetList.Contains(targetId))
                {
                    TargetList.Add(targetId);

                    TargetColorlist.Add(targetId, target.GetRoleColor());
                }
            }
        }
        public static void OnCoEnterVent(PlayerPhysics __instance, int ventId)
        {
            var pc = __instance.myPlayer;
            if (!AmongUsClient.Instance.AmHost || IsInvis(pc.PlayerId)) return;
            new LateTask(() =>
            {
                if (CanGoInvis(pc.PlayerId))
                {
                    ventedId.Remove(pc.PlayerId);
                    ventedId.Add(pc.PlayerId, ventId);

                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, 34, SendOption.Reliable, pc.GetClientId());
                    writer.WritePacked(ventId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    InvisTime.Add(pc.PlayerId, Utils.GetTimeStamp());
                    SendRPC(pc);
                    NameNotifyManager.Notify(pc, ("YfoxInvisState"), YfoxDuration.GetFloat());
                }
                else
                {
                    __instance.myPlayer.MyPhysics.RpcBootFromVent(ventId);
                    NameNotifyManager.Notify(pc, ("YfoxInvisInCooldown"));
                }
            }, 0.5f, "Yfox Vent");
        }
        public static void OnEnterVent(PlayerControl pc, Vent vent)
        {
            if (!pc.Is(CustomRoles.Yfox) || !IsInvis(pc.PlayerId)) return;

            InvisTime.Remove(pc.PlayerId);
            lastTime.Add(pc.PlayerId, Utils.GetTimeStamp());
            SendRPC(pc);

            pc?.MyPhysics?.RpcBootFromVent(vent.Id);
            NameNotifyManager.Notify(pc, ("YfoxInvisStateOut"));
        }
        public static string GetHudText(PlayerControl pc)
        {
            if (pc == null || !GameStates.IsInTask || !PlayerControl.LocalPlayer.IsAlive()) return "";
            var str = new StringBuilder();
            if (IsInvis(pc.PlayerId))
            {
                var remainTime = InvisTime[pc.PlayerId] + (long)YfoxDuration.GetFloat() - Utils.GetTimeStamp();
                str.Append(string.Format(("YfoxInvisStateCountdown"), remainTime));
            }
            else if (lastTime.TryGetValue(pc.PlayerId, out var time))
            {
                var cooldown = time + (long)YfoxCooldown.GetFloat() - Utils.GetTimeStamp();
                str.Append(string.Format(("YfoxInvisCooldownRemain"), cooldown));
            }
            else
            {
                str.Append(("YfoxCanVent"));
            }
            return str.ToString();
        }

        public static string GetFoxArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (!IsThisRole(seer.PlayerId)) return "";
            if (GameStates.IsMeeting) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (!seer.GetPlayerTaskState().IsTaskFinished) return "";
            if (!seer.IsAlive()) return "";
            var arrows = "";
            foreach (var targetId in TargetList)
            {
                var arrow = TargetArrow.GetArrows(seer, targetId);
                arrows += Utils.ColorString(TargetColorlist[targetId], arrow);
            }
            return arrows;

        }

        public static void OnCompleteTask(PlayerControl player)
        {
            var playertask = player.GetPlayerTaskState();
            if (playertask.CompletedTasksCount >= playertask.AllTasksCount && player.Is(CustomRoles.Yfox))
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    Utils.SendMessage(string.Format(GetString("YfoxNotice2")), 255, Utils.ColorString(GetRoleColor(CustomRoles.Yfox), GetString("MessageFromYfoxNotice")));
                    return;
                }
            }
        }
    }
}