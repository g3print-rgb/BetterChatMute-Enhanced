/*
Fixed / Changed by SeesAll, version incremented by 1 to indicate a change. The readme notes can be found at https://github.com/SeesAll/
I take no credit for this plugin, I merely fixed or changed a few things.
*/
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Better Chat Mute", "SeesAll", "1.3.0")]
    [Description("Simple mute system, made for use with Better Chat")]
    internal class BetterChatMute : CovalencePlugin
    {
        private static Dictionary<string, MuteInfo> _mutes;
        private bool _isDataDirty;
        private bool _globalMute;
        private PluginConfig _config;

        #region Hooks

        private void Loaded()
        {
            permission.RegisterPermission("betterchatmute.permanent", this);

            LoadConfigValues();
            LoadData(out _mutes);

            if (_mutes == null)
                _mutes = new Dictionary<string, MuteInfo>();

            NormalizeLoadedMuteData();
            SaveData(_mutes);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["No Permission"] = "You don't have permission to use this command.",
                ["No Reason"] = "Unknown reason",
                ["Muted"] = "{player} was muted by {initiator}: {reason}.",
                ["Muted Time"] = "{player} was muted by {initiator} for {time}: {reason}.",
                ["Unmuted"] = "{player} was unmuted by {initiator}.",
                ["Not Muted"] = "{player} is currently not muted.",
                ["Mute Expired"] = "{player} is no longer muted.",
                ["Invalid Time Format"] = "Invalid time format. Example: 1d2h3m4s = 1 day, 2 hours, 3 min, 4 sec",
                ["Nobody Muted"] = "There is nobody muted at the moment.",
                ["Invalid Syntax Mute"] = "/mute <player|steamid> \"[reason]\" [time: 1d1h1m1s]",
                ["Invalid Syntax Unmute"] = "/unmute <player|steamid>",
                ["Player Name Not Found"] = "Could not find player with name '{name}'",
                ["Player ID Not Found"] = "Could not find player with ID '{id}'",
                ["Multiple Players Found"] = "Multiple matching players found: \n{matches}",
                ["Time Muted Player Joined"] = "{player} is temporarily muted. Remaining time: {time}",
                ["Time Muted Player Chat"] = "You may not chat, you are temporarily muted. Remaining time: {time}",
                ["Muted Player Joined"] = "{player} is permanently muted.",
                ["Muted Player Chat"] = "You may not chat, you are permanently muted.",
                ["Global Mute Enabled"] = "Global mute was enabled. Nobody can chat while global mute is active.",
                ["Global Mute Disabled"] = "Global mute was disabled. Everybody can chat again.",
                ["Global Mute Active"] = "Global mute is active, you may not chat."
            }, this);

            timer.Repeat(10f, 0, ProcessExpiredMutes);
        }

#if RUST
        private object OnPlayerChat(BasePlayer bplayer, string message, ConVar.Chat.ChatChannel chatChannel)
        {
            IPlayer player = bplayer.IPlayer;
            bool isPublicMessage = chatChannel == ConVar.Chat.ChatChannel.Global;
#else
        private object OnUserChat(IPlayer player, string message)
        {
            bool isPublicMessage = true;
#endif
            if (plugins.Exists("BetterChat"))
                return null;

            return HandleChat(player, isPublicMessage);
        }

        private void OnBetterChat(Dictionary<string, object> messageData)
        {
#if RUST
            var chatChannel = (ConVar.Chat.ChatChannel)messageData["ChatChannel"];
            bool isPublicMessage = chatChannel == ConVar.Chat.ChatChannel.Global;
#else
            bool isPublicMessage = true;
#endif
            if (HandleChat((IPlayer)messageData["Player"], isPublicMessage) != null)
                messageData["CancelOption"] = 2;
        }

        private void OnUserInit(IPlayer player)
        {
            UpdateMuteStatus(player);

            if (!MuteInfo.IsMuted(player))
                return;

            if (_mutes[player.Id].Timed)
            {
                PublicMessage("Time Muted Player Joined",
                    new KeyValuePair<string, string>("player", SanitizeName(player.Name)),
                    new KeyValuePair<string, string>("time", FormatTime(_mutes[player.Id].ExpireDate - DateTime.UtcNow)));
            }
            else
            {
                PublicMessage("Muted Player Joined", new KeyValuePair<string, string>("player", SanitizeName(player.Name)));
            }
        }

        #endregion

        #region Commands

        [Command("toggleglobalmute", "bcm.toggleglobalmute"), Permission("betterchatmute.use.global")]
        private void CmdGlobalMute(IPlayer player, string cmd, string[] args)
        {
            _globalMute = !_globalMute;
            PublicMessage(_globalMute ? "Global Mute Enabled" : "Global Mute Disabled");
        }

        [Command("mutelist", "bcm.mutelist"), Permission("betterchatmute.use")]
        private void CmdMuteList(IPlayer player, string cmd, string[] args)
        {
            if (_mutes.Count == 0)
            {
                player.Reply(lang.GetMessage("Nobody Muted", this, player.Id));
                return;
            }

            var lines = _mutes.Select(kvp =>
            {
                MuteInfo mute = kvp.Value;
                string targetName = GetStoredPlayerName(kvp.Key, mute);
                string duration = mute.Timed ? FormatTime(mute.ExpireDate - DateTime.UtcNow) : "Permanent";
                return $"{targetName} ({kvp.Key}): {duration}";
            });

            player.Reply(string.Join(Environment.NewLine, lines));
        }

        [Command("mute", "bcm.mute"), Permission("betterchatmute.use")]
        private void CmdMute(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                player.Reply(lang.GetMessage("Invalid Syntax Mute", this, player.Id));
                return;
            }

            string reason = string.Empty;
            TimeSpan? timeSpan = null;

            IPlayer target = GetPlayer(args[0], player);
            if (target == null)
                return;

            for (int i = 1; i < args.Length; i++)
            {
                if (TryParseTimeSpan(args[i], out timeSpan))
                {
                    args[i] = null;
                    break;
                }
            }

            if (timeSpan == null && !permission.UserHasPermission(player.Id, "betterchatmute.permanent") && player.Id != "server_console")
            {
                player.Reply(lang.GetMessage("No Permission", this, player.Id));
                return;
            }

            reason = string.Join(" ", args.Skip(1).Where(a => a != null).ToArray());
            reason = string.IsNullOrEmpty(reason) ? lang.GetMessage("No Reason", this) : reason;

            if (timeSpan == null)
            {
                API_Mute(target, player, reason, true, true);
            }
            else
            {
                API_TimeMute(target, player, timeSpan.Value, reason, true, true);
            }
        }

        [Command("unmute", "bcm.unmute"), Permission("betterchatmute.use")]
        private void CmdUnmute(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 1)
            {
                player.Reply(lang.GetMessage("Invalid Syntax Unmute", this, player.Id));
                return;
            }

            IPlayer target = GetPlayer(args[0], player);
            if (target == null)
                return;

            if (!MuteInfo.IsMuted(target))
            {
                player.Reply(lang.GetMessage("Not Muted", this, player.Id).Replace("{player}", SanitizeName(target.Name)));
                return;
            }

            API_Unmute(target, player, true, true);
        }

        #endregion

        #region API Methods

        private void API_Mute(IPlayer target, IPlayer player, string reason = "", bool callHook = true, bool broadcast = true)
        {
            reason = string.IsNullOrEmpty(reason) ? lang.GetMessage("No Reason", this) : reason;

            var muteInfo = CreateMuteInfo(target, player, MuteInfo.NonTimedExpireDate, reason);
            _mutes[target.Id] = muteInfo;
            SaveData(_mutes);

            if (callHook)
                Interface.CallHook("OnBetterChatMuted", target, player, reason);

            if (broadcast)
            {
                PublicMessage("Muted",
                    new KeyValuePair<string, string>("initiator", SanitizeName(player.Name)),
                    new KeyValuePair<string, string>("player", SanitizeName(target.Name)),
                    new KeyValuePair<string, string>("reason", reason));
            }

            SendMuteWebhook(muteInfo);
        }

        private void API_TimeMute(IPlayer target, IPlayer player, TimeSpan timeSpan, string reason = "", bool callHook = true, bool broadcast = true)
        {
            reason = string.IsNullOrEmpty(reason) ? lang.GetMessage("No Reason", this) : reason;

            var muteInfo = CreateMuteInfo(target, player, DateTime.UtcNow + timeSpan, reason);
            _mutes[target.Id] = muteInfo;
            SaveData(_mutes);

            if (callHook)
                Interface.CallHook("OnBetterChatTimeMuted", target, player, timeSpan, reason);

            if (broadcast)
            {
                PublicMessage("Muted Time",
                    new KeyValuePair<string, string>("initiator", SanitizeName(player.Name)),
                    new KeyValuePair<string, string>("player", SanitizeName(target.Name)),
                    new KeyValuePair<string, string>("time", FormatTime(timeSpan)),
                    new KeyValuePair<string, string>("reason", reason));
            }

            SendMuteWebhook(muteInfo);
        }

        private bool API_Unmute(IPlayer target, IPlayer player, bool callHook = true, bool broadcast = true)
        {
            if (!MuteInfo.IsMuted(target))
                return false;

            MuteInfo muteInfo = _mutes[target.Id];
            _mutes.Remove(target.Id);
            SaveData(_mutes);

            if (callHook)
                Interface.CallHook("OnBetterChatUnmuted", target, player);

            if (broadcast)
            {
                PublicMessage("Unmuted",
                    new KeyValuePair<string, string>("initiator", SanitizeName(player.Name)),
                    new KeyValuePair<string, string>("player", SanitizeName(target.Name)));
            }

            SendManualUnmuteWebhook(target, player, muteInfo);
            return true;
        }

        private void API_SetGlobalMuteState(bool state, bool broadcast = true)
        {
            _globalMute = state;

            if (broadcast)
                PublicMessage(_globalMute ? "Global Mute Enabled" : "Global Mute Disabled");
        }

        private bool API_GetGlobalMuteState() => _globalMute;

        private bool API_IsMuted(IPlayer player) => player != null && _mutes.ContainsKey(player.Id);

        private List<string> API_GetMuteList() => _mutes.Keys.ToList();

        #endregion

        #region Helpers

        private void NormalizeLoadedMuteData()
        {
            bool changed = false;

            foreach (KeyValuePair<string, MuteInfo> entry in _mutes.ToList())
            {
                MuteInfo mute = entry.Value;
                if (mute == null)
                {
                    _mutes.Remove(entry.Key);
                    changed = true;
                    continue;
                }

                mute.TargetId = string.IsNullOrEmpty(mute.TargetId) ? entry.Key : mute.TargetId;

                IPlayer player = players.FindPlayerById(entry.Key);
                if (string.IsNullOrEmpty(mute.TargetName) && player != null)
                {
                    mute.TargetName = SanitizeName(player.Name);
                    changed = true;
                }

                if (string.IsNullOrEmpty(mute.Reason))
                {
                    mute.Reason = lang.GetMessage("No Reason", this);
                    changed = true;
                }
            }

            if (changed)
                SaveData(_mutes);
        }

        private MuteInfo CreateMuteInfo(IPlayer target, IPlayer initiator, DateTime expireDate, string reason)
        {
            return new MuteInfo
            {
                TargetId = target.Id,
                TargetName = SanitizeName(target.Name),
                Reason = reason,
                ExpireDate = expireDate,
                MutedById = initiator?.Id ?? "server_console",
                MutedByName = SanitizeName(initiator?.Name ?? "Server Console"),
                MutedAtUtc = DateTime.UtcNow
            };
        }

        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            return name.Replace("<", "‹").Replace(">", "›");
        }

        private string GetStoredPlayerName(string playerId, MuteInfo mute)
        {
            IPlayer livePlayer = players.FindPlayerById(playerId);
            if (livePlayer != null && !string.IsNullOrEmpty(livePlayer.Name))
                return SanitizeName(livePlayer.Name);

            if (!string.IsNullOrEmpty(mute?.TargetName))
                return mute.TargetName;

            return "Unknown Player";
        }

        private void PublicMessage(string key, params KeyValuePair<string, string>[] replacements)
        {
            string message = lang.GetMessage(key, this);

            foreach (KeyValuePair<string, string> replacement in replacements)
                message = message.Replace($"{{{replacement.Key}}}", replacement.Value);

            server.Broadcast(message);
            Puts(message);
        }

        private object HandleChat(IPlayer player, bool isPublicChat)
        {
            if (!isPublicChat)
                return null;

            UpdateMuteStatus(player);

            object hookResult = Interface.CallHook("OnBetterChatMuteHandle", player, MuteInfo.IsMuted(player) ? JObject.FromObject(_mutes[player.Id]) : null);
            if (hookResult != null)
                return hookResult;

            if (MuteInfo.IsMuted(player))
            {
                if (_mutes[player.Id].Timed)
                {
                    player.Reply(lang.GetMessage("Time Muted Player Chat", this, player.Id)
                        .Replace("{time}", FormatTime(_mutes[player.Id].ExpireDate - DateTime.UtcNow)));
                }
                else
                {
                    player.Reply(lang.GetMessage("Muted Player Chat", this, player.Id));
                }

                return true;
            }

            if (_globalMute && !permission.UserHasPermission(player.Id, "betterchatmute.use.global"))
            {
                player.Reply(lang.GetMessage("Global Mute Active", this, player.Id));
                return true;
            }

            return null;
        }

        private void UpdateMuteStatus(IPlayer player)
        {
            if (!MuteInfo.IsMuted(player))
                return;

            if (_mutes[player.Id].Expired)
                ExpireMute(player.Id, _mutes[player.Id]);
        }

        private void ProcessExpiredMutes()
        {
            if (_mutes.Count == 0)
                return;

            List<KeyValuePair<string, MuteInfo>> expiredMutes = _mutes.Where(m => m.Value != null && m.Value.Expired).ToList();
            if (expiredMutes.Count == 0)
                return;

            foreach (KeyValuePair<string, MuteInfo> expired in expiredMutes)
                ExpireMute(expired.Key, expired.Value);

            if (_isDataDirty)
            {
                SaveData(_mutes);
                _isDataDirty = false;
            }
        }

        private void ExpireMute(string playerId, MuteInfo mute)
        {
            IPlayer player = players.FindPlayerById(playerId);
            string playerName = GetStoredPlayerName(playerId, mute);

            _mutes.Remove(playerId);
            PublicMessage("Mute Expired", new KeyValuePair<string, string>("player", playerName));

            Interface.CallHook("OnBetterChatMuteExpired", player);
            SendMuteExpiredWebhook(playerId, playerName, mute);

            _isDataDirty = true;
        }

        private IPlayer GetPlayer(string nameOrId, IPlayer requestor)
        {
            if (nameOrId.IsSteamId())
            {
                IPlayer player = players.All.FirstOrDefault(p => p.Id == nameOrId);

                if (player == null)
                    requestor.Reply(lang.GetMessage("Player ID Not Found", this, requestor.Id).Replace("{id}", nameOrId));

                return player;
            }

            List<IPlayer> foundPlayers = new List<IPlayer>();

            foreach (IPlayer player in players.Connected)
            {
                if (string.Equals(player.Name, nameOrId, StringComparison.CurrentCultureIgnoreCase))
                    return player;

                if (player.Name.IndexOf(nameOrId, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    foundPlayers.Add(player);
            }

            switch (foundPlayers.Count)
            {
                case 0:
                    requestor.Reply(lang.GetMessage("Player Name Not Found", this, requestor.Id).Replace("{name}", nameOrId));
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    string[] names = foundPlayers.Select(current => SanitizeName(current.Name)).ToArray();
                    requestor.Reply(lang.GetMessage("Multiple Players Found", this, requestor.Id).Replace("{matches}", string.Join(", ", names)));
                    break;
            }

            return null;
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;

            List<string> values = new List<string>();

            if (time.Days != 0)
                values.Add($"{time.Days} day(s)");

            if (time.Hours != 0)
                values.Add($"{time.Hours} hour(s)");

            if (time.Minutes != 0)
                values.Add($"{time.Minutes} minute(s)");

            if (time.Seconds != 0)
                values.Add($"{time.Seconds} second(s)");

            return values.Count == 0 ? "0 second(s)" : values.ToSentence();
        }

        private static bool TryParseTimeSpan(string source, out TimeSpan? timeSpan)
        {
            int seconds = 0, minutes = 0, hours = 0, days = 0;

            Match s = new Regex(@"(\d+?)s", RegexOptions.IgnoreCase).Match(source);
            Match m = new Regex(@"(\d+?)m", RegexOptions.IgnoreCase).Match(source);
            Match h = new Regex(@"(\d+?)h", RegexOptions.IgnoreCase).Match(source);
            Match d = new Regex(@"(\d+?)d", RegexOptions.IgnoreCase).Match(source);

            if (s.Success)
                seconds = Convert.ToInt32(s.Groups[1].ToString());

            if (m.Success)
                minutes = Convert.ToInt32(m.Groups[1].ToString());

            if (h.Success)
                hours = Convert.ToInt32(h.Groups[1].ToString());

            if (d.Success)
                days = Convert.ToInt32(d.Groups[1].ToString());

            source = source.Replace(seconds + "s", string.Empty);
            source = source.Replace(minutes + "m", string.Empty);
            source = source.Replace(hours + "h", string.Empty);
            source = source.Replace(days + "d", string.Empty);

            if (!string.IsNullOrEmpty(source) || !(s.Success || m.Success || h.Success || d.Success))
            {
                timeSpan = null;
                return false;
            }

            timeSpan = new TimeSpan(days, hours, minutes, seconds);
            return true;
        }

        private void SendMuteWebhook(MuteInfo muteInfo)
        {
            if (!_config.EnableDiscordLogging || !_config.LogMutesToDiscord || string.IsNullOrWhiteSpace(_config.DiscordWebhookUrl))
                return;

            string duration = muteInfo.Timed ? FormatTime(muteInfo.ExpireDate - muteInfo.MutedAtUtc) : "Permanent";

            if (_config.UseDiscordEmbeds)
            {
                var fields = new List<object>
                {
                    new { name = "Player", value = $"`{muteInfo.TargetName}` (`{muteInfo.TargetId}`)", inline = false },
                    new { name = "Type", value = muteInfo.Timed ? "Temporary" : "Permanent", inline = true },
                    new { name = "Duration", value = duration, inline = true },
                    new { name = "Muted By", value = $"`{muteInfo.MutedByName}` (`{muteInfo.MutedById}`)", inline = false },
                    new { name = "Reason", value = string.IsNullOrWhiteSpace(muteInfo.Reason) ? "Unknown reason" : muteInfo.Reason, inline = false },
                    new { name = "Muted At (UTC)", value = muteInfo.MutedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"), inline = false }
                };

                SendDiscordEmbed("Player Muted", 15158332, fields);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**{_config.ServerPrefix}**");
            sb.AppendLine($"Player muted: `{muteInfo.TargetName}` (`{muteInfo.TargetId}`)");
            sb.AppendLine($"Type: `{(muteInfo.Timed ? "Temporary" : "Permanent")}`");
            sb.AppendLine($"Duration: `{duration}`");
            sb.AppendLine($"Reason: {muteInfo.Reason}");
            sb.AppendLine($"Muted by: `{muteInfo.MutedByName}` (`{muteInfo.MutedById}`)");
            sb.AppendLine($"Muted at (UTC): `{muteInfo.MutedAtUtc:yyyy-MM-dd HH:mm:ss}`");

            SendDiscordMessage(sb.ToString());
        }

        private void SendManualUnmuteWebhook(IPlayer target, IPlayer initiator, MuteInfo priorMute)
        {
            if (!_config.EnableDiscordLogging || !_config.LogUnmutesToDiscord || string.IsNullOrWhiteSpace(_config.DiscordWebhookUrl))
                return;

            string targetName = SanitizeName(target?.Name) ?? priorMute?.TargetName ?? "Unknown Player";
            string targetId = target?.Id ?? priorMute?.TargetId ?? "Unknown";
            string initiatorName = SanitizeName(initiator?.Name ?? "Server Console");
            string initiatorId = initiator?.Id ?? "server_console";

            if (_config.UseDiscordEmbeds)
            {
                var fields = new List<object>
                {
                    new { name = "Player", value = $"`{targetName}` (`{targetId}`)", inline = false },
                    new { name = "Unmuted By", value = $"`{initiatorName}` (`{initiatorId}`)", inline = false },
                    new { name = "Time (UTC)", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), inline = false }
                };

                SendDiscordEmbed("Player Unmuted", 3066993, fields);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**{_config.ServerPrefix}**");
            sb.AppendLine($"`{initiatorName}` (`{initiatorId}`) unmuted `{targetName}` (`{targetId}`)");
            sb.AppendLine($"Time (UTC): `{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}`");

            SendDiscordMessage(sb.ToString());
        }

        private void SendMuteExpiredWebhook(string playerId, string playerName, MuteInfo priorMute)
        {
            if (!_config.EnableDiscordLogging || !_config.LogExpiredMutesToDiscord || string.IsNullOrWhiteSpace(_config.DiscordWebhookUrl))
                return;

            if (_config.UseDiscordEmbeds)
            {
                var fields = new List<object>
                {
                    new { name = "Player", value = $"`{playerName}` (`{playerId}`)", inline = false },
                    new { name = "Expired At (UTC)", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), inline = false }
                };

                SendDiscordEmbed("Mute Expired", 3447003, fields);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"**{_config.ServerPrefix}**");
            sb.AppendLine($"`{playerName}` (`{playerId}`) is no longer muted.");
            sb.AppendLine($"Mute expired at (UTC): `{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}`");

            SendDiscordMessage(sb.ToString());
        }

        private void SendDiscordMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(_config.DiscordWebhookUrl) || string.IsNullOrWhiteSpace(content))
                return;

            var payload = JsonConvert.SerializeObject(new { content });
            PostDiscordPayload(payload);
        }

        private void SendDiscordEmbed(string title, int color, List<object> fields)
        {
            if (string.IsNullOrWhiteSpace(_config.DiscordWebhookUrl))
                return;

            var embed = new
            {
                title,
                color,
                fields,
                footer = new { text = _config.ServerPrefix },
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var payload = JsonConvert.SerializeObject(new
            {
                embeds = new[] { embed }
            });

            PostDiscordPayload(payload);
        }

        private void PostDiscordPayload(string payload)
        {
            var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

            webrequest.Enqueue(_config.DiscordWebhookUrl, payload, (code, response) =>
            {
                if (code == 204 || code == 200)
                    return;

                PrintWarning($"Discord webhook returned code {code}. Response: {response}");
            }, this, Core.Libraries.RequestMethod.POST, headers);
        }

        #region Data & Config Helper

        private string DataFileName => Title.Replace(" ", string.Empty);

        private void LoadData<T>(out T data, string filename = null)
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<T>(filename ?? DataFileName);
            }
            catch
            {
                data = Activator.CreateInstance<T>();
            }
        }

        private void SaveData<T>(T data, string filename = null) => Interface.Oxide.DataFileSystem.WriteObject(filename ?? DataFileName, data);

        protected override void LoadDefaultConfig()
        {
            _config = PluginConfig.CreateDefault();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            LoadConfigValues();
        }

        private void LoadConfigValues()
        {
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                    throw new Exception("Config was null");
            }
            catch
            {
                PrintWarning("Configuration file is invalid; using defaults.");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private class PluginConfig
        {
            [JsonProperty("Server Prefix")]
            public string ServerPrefix { get; set; }

            [JsonProperty("Discord WebhookURL")]
            public string DiscordWebhookUrl { get; set; }

            [JsonProperty("Enable Discord Logging")]
            public bool EnableDiscordLogging { get; set; }

            [JsonProperty("Log Mutes To Discord")]
            public bool LogMutesToDiscord { get; set; }

            [JsonProperty("Log Unmutes To Discord")]
            public bool LogUnmutesToDiscord { get; set; }

            [JsonProperty("Log Expired Mutes To Discord")]
            public bool LogExpiredMutesToDiscord { get; set; }

            [JsonProperty("Use Discord Embeds")]
            public bool UseDiscordEmbeds { get; set; }

            public static PluginConfig CreateDefault()
            {
                return new PluginConfig
                {
                    ServerPrefix = "ScavengersHaven NA 3x",
                    DiscordWebhookUrl = "WEBHOOK URL GOES HERE",
                    EnableDiscordLogging = false,
                    LogMutesToDiscord = true,
                    LogUnmutesToDiscord = true,
                    LogExpiredMutesToDiscord = true,
                    UseDiscordEmbeds = true
                };
            }
        }

        #endregion

        #endregion

        #region Classes

        public class MuteInfo
        {
            public DateTime ExpireDate = DateTime.MinValue;

            [JsonIgnore]
            public bool Timed => ExpireDate != DateTime.MinValue;

            [JsonIgnore]
            public bool Expired => Timed && ExpireDate < DateTime.UtcNow;

            [JsonProperty("Reason")]
            public string Reason { get; set; }

            [JsonProperty("Target Name")]
            public string TargetName { get; set; }

            [JsonProperty("Target ID")]
            public string TargetId { get; set; }

            [JsonProperty("Muted By Name")]
            public string MutedByName { get; set; }

            [JsonProperty("Muted By ID")]
            public string MutedById { get; set; }

            [JsonProperty("Muted At UTC")]
            public DateTime MutedAtUtc { get; set; }

            public static bool IsMuted(IPlayer player) => player != null && _mutes.ContainsKey(player.Id);

            public static readonly DateTime NonTimedExpireDate = DateTime.MinValue;
        }

        #endregion
    }
}
