using HarmonyLib;
using System;
using DiscordGameSDKWrapper;
using Elements.Core;
using ResoniteModLoader;
using System.Reflection;
using FrooxEngine.Interfacing;
using FrooxEngine;

namespace DiscordInviteMod
{
    public class DiscordInviteMod : ResoniteMod
    {
        public override string Name => "DiscordInviteMod";
        public override string Author => "xLinka";
        public override string Version => "1.0.0";

        public override void OnEngineInit()
        {
            var harmony = new Harmony("com.xLinka.discordinvitemod");
            harmony.PatchAll();
            UniLog.Log("Discord Invite Mod is running.");
        }
    }

    [HarmonyPatch]
    public static class DiscordConnectorPatches
    {
        static MethodBase TargetMethod() => AccessTools.Method(typeof(DiscordConnector), "SetCurrentStatus");

        static void Postfix(DiscordConnector __instance, World world, bool isPrivate, int totalWorldCount)
        {
            bool initialized = (bool)AccessTools.Property(typeof(DiscordConnector), "Initialized").GetValue(__instance, null);
            if (!initialized) return;

            var discordField = AccessTools.Field(typeof(DiscordConnector), "discord");
            var discord = (Discord)discordField.GetValue(__instance);
            if (discord == null) return;

            UpdateDiscordPresence(__instance, discord, world, isPrivate, totalWorldCount);
        }

        private static void UpdateDiscordPresence(DiscordConnector instance, Discord discord, World world, bool isPrivate, int totalWorldCount)
        {
            var activityManager = discord.GetActivityManager();
            var activity = new Activity
            {
                ApplicationId = 1159154461059592333L, 
                Assets =
                {
                    LargeImage = instance.LARGE_IMAGE_ID,
                    SmallImage = instance.SMALL_IMAGE_ID
                },
                State = isPrivate ? "In a Quiet Hidden World" : "In a Exposed World",
                Details = $"{world.Name}: {world.UserCount}/{world.MaxUsers} users",
                Party =
                {
                    Id = world.SessionId,
                    Size =
                    {
                        CurrentSize = world.UserCount,
                        MaxSize = world.MaxUsers
                    }
                },
                Timestamps =
                {
                    Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };

            if (!isPrivate)
            {
                activity.Secrets.Join = $"ressession://{world.SessionId}";
            }

            activityManager.UpdateActivity(activity, result =>
            {
                if (result == Result.Ok)
                {
                    UniLog.Log("Discord: Rich presence updated successfully.");
                }
                else
                {
                    UniLog.Log($"Discord: Failed to update rich presence. Error: {result}");
                }
            });
        }
    }
}
