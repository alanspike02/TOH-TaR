using System.Collections.Generic;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor
{
    public static class Outlaw
    {
        static readonly int Id = 35000;
        static List<byte> playerIdList = new();
        public static OptionItem IncreasePercentage;
        public static OptionItem OutlawDeferment;
        public static OptionItem OptionCanKillOnlyOnce;
        private static OptionItem OutlawDefaultKillCool;

        private static bool IsKilled = false;
        private static bool KilledOnce= false;
        private static bool CanKillOnlyOnce = false;
        private static float Outlawkc;
        private static float Zero;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Outlaw);
            OutlawDefaultKillCool = FloatOptionItem.Create(Id + 10, "OutlawDefaultKillCool", new(0f, 180f, 2.5f), 235f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Outlaw])
                .SetValueFormat(OptionFormat.Seconds);
            OutlawDeferment = FloatOptionItem.Create(Id + 12, "OutlawDeferment", new(1f, 30f, 1f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Outlaw])
                .SetValueFormat(OptionFormat.Seconds);
            IncreasePercentage = FloatOptionItem.Create(Id + 14, "IncreasePercentage", new(1f, 10f, 0.25f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Outlaw])
                .SetValueFormat(OptionFormat.Multiplier);
            OptionCanKillOnlyOnce = BooleanOptionItem.Create(Id + 16, "CanKillOnlyOnce", true, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Outlaw]);
        }
        public static void Init()
        {
            playerIdList = new();
            IsKilled = false;
            KilledOnce = false;
            CanKillOnlyOnce = OptionCanKillOnlyOnce.GetBool();
            Outlawkc = OutlawDefaultKillCool.GetFloat();
            Zero = 0;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = OutlawDefaultKillCool.GetFloat();

        public static void OnCheckMurder(PlayerControl killer)
        {
            if (!killer.Is(CustomRoles.Outlaw)) return;
            if (KilledOnce)
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Outlawkc;
                killer.SyncSettings();
                return;
            }
            if (!IsKilled)
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Main.MinKillCooldown;
                killer.SyncSettings();
                IsKilled = true;
                new LateTask(() =>
                {
                    if (IsKilled)
                    {
                        Main.AllPlayerKillCooldown[killer.PlayerId] = Outlawkc;
                        killer.SyncSettings();
                        killer.SetKillCooldown();
                        IsKilled = false;
                    }
                }, OutlawDeferment.GetFloat(), "Outlaw Deferment");
            }
            else if (IsKilled)
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Outlawkc * IncreasePercentage.GetFloat();
                killer.SyncSettings();
                if (CanKillOnlyOnce)
                {
                    KilledOnce = true;
                }
                IsKilled = false;
            }
        }
        }
    }