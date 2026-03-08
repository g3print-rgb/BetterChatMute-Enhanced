
# BetterChatMute (Enhanced Moderation Fork)

**Author:** SeesAll  
**Based on original plugin by:** LaserHydra

## Overview
This repository contains an enhanced version of **BetterChatMute**, designed to work alongside the BetterChat plugin for Rust servers running **uMod/Oxide**.

The goal of this revision is to improve moderation visibility, auditing, and integration with Discord while maintaining the original behavior of the plugin.

---

# Why This Fork Exists

The original BetterChatMute plugin provides a lightweight mute system, but it lacks several features that server owners commonly need for moderation management.

This revision adds:

* Discord moderation logging
* moderation audit data
* configurable settings
* improved mute tracking

All while keeping the same commands and behavior administrators expect.

---

# Major Enhancements

## 1. Discord Webhook Logging

Mute actions can now be automatically logged to Discord using webhook integration.

Events logged include:

* Player muted
* Player unmuted manually
* Temporary mute expiration

This allows moderation teams to track actions in real time without being in-game.

---

## 2. Discord Embed Formatting

Instead of plain text messages, moderation logs now use **Discord embeds** for cleaner presentation.

Embeds include:

* Player name
* SteamID
* Mute type (temporary or permanent)
* Duration
* Reason
* Moderator responsible
* Timestamp
* Server name footer

This significantly improves readability in moderation channels.

---

## 3. Moderation Audit Data

Mute records now store additional information:

* Target player name
* Target SteamID
* Moderator name
* Moderator SteamID
* Reason
* Timestamp
* Expiration time

This provides a complete audit trail for moderation actions.

---

## 4. Configurable Plugin Settings

The plugin now includes a configuration file allowing administrators to control:

* Discord webhook URL
* whether Discord logging is enabled
* which events are logged
* embed usage
* server name prefix

Example config:

```
{
  "Server Prefix": "ScavengersHaven NA 3x",
  "Discord WebhookURL": "WEBHOOK URL HERE",
  "Enable Discord Logging": true,
  "Log Mutes To Discord": true,
  "Log Unmutes To Discord": true,
  "Log Expired Mutes To Discord": true,
  "Use Discord Embeds": true
}
```

---

# No Changes to Admin Commands

All original commands remain unchanged:

```
/mute
/unmute
/mutelist
/toggleglobalmute
```

Existing moderation workflows will continue to work exactly the same.

---

# Performance Impact

The plugin remains extremely lightweight.

Discord logging only triggers when moderation events occur:

* mute
* unmute
* expiration

These events are rare and have negligible performance impact on the server.

---

# Compatibility

This plugin is fully compatible with:

* BetterChat
* existing mute data files
* existing moderation commands

Servers can upgrade without wiping previous mute data.

---

# Credits

Original plugin by **LaserHydra**.

This fork expands the moderation capabilities while preserving the simplicity and reliability of the original design.
