using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WaterWipeout", "Magnumk", "1.0.0")]
	[Description("Drowns players that disconnect under water")]
    class WaterWipeout : RustPlugin
    {
        private PluginConfig config;
        private List<string> drownedPlayers = new List<string>();
        private Timer drowningTimer;

        private class PluginConfig
        {
            public float DrowningDamage { get; set; }
            public float DrowningCheckInterval { get; set; }
            public float PlayerHeadHeight { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    DrowningDamage = 50f,
                    DrowningCheckInterval = 20f,
                    PlayerHeadHeight = 1.0f
                };
            }
        }

        protected override void LoadDefaultConfig() => config = PluginConfig.DefaultConfig();

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
            if (config == null)
            {
                LoadDefaultConfig();
            }
            SaveConfig();
            LoadDefaultMessages();
            drownedPlayers = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("DrownedPlayers");
            drowningTimer = timer.Every(config.DrowningCheckInterval, CheckDrowning);
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoteText"] = "Message from ADMIN: DO NOT DISCONNECT UNDER WATER!!"
            }, this);
        }

        private string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        void Unloaded()
        {
            if (drowningTimer != null && !drowningTimer.Destroyed)
                drowningTimer.Destroy();
            Interface.Oxide.DataFileSystem.WriteObject("DrownedPlayers", drownedPlayers);
        }

        private void CheckDrowning()
        {
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player != null && IsPlayerUnderwater(player))
                {
                    ApplyDrowningLogic(player);
                }
            }
        }

        private void ApplyDrowningLogic(BasePlayer player)
        {
            player.Hurt(config.DrowningDamage, Rust.DamageType.Drowned);
            if (!drownedPlayers.Contains(player.UserIDString))
            {
                drownedPlayers.Add(player.UserIDString);
                Interface.Oxide.DataFileSystem.WriteObject("DrownedPlayers", drownedPlayers);
            }
        }

        private bool IsPlayerUnderwater(BasePlayer player)
        {
            Vector3 playerPosition = player.transform.position;
            float waterLevel = TerrainMeta.WaterMap.GetHeight(playerPosition);
            float playerHeadPosition = playerPosition.y + config.PlayerHeadHeight;
            return playerHeadPosition < waterLevel;
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (drownedPlayers.Contains(player.UserIDString))
            {
                var noteItem = ItemManager.CreateByItemID(1414245162, 1);
                if (noteItem != null)
                {
                    noteItem.text = GetMessage("NoteText");
                    noteItem.MarkDirty();
                    player.GiveItem(noteItem);
                    drownedPlayers.Remove(player.UserIDString);
                    Interface.Oxide.DataFileSystem.WriteObject("DrownedPlayers", drownedPlayers);
                }
            }
        }
    }
}
