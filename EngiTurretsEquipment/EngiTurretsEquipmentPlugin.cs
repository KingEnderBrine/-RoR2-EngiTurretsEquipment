using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using EngiTurretsEquipment;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.CharacterAI;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion(EngiTurretsEquipmentPlugin.Version)]
namespace EngiTurretsEquipment
{
    [BepInDependency(RiskOfOptionsIntegration.RiskOfOptionsGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, Name, Version)]
    public class EngiTurretsEquipmentPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.KingEnderBrine.EngiTurretsEquipment";
        public const string Name = "EngiTurretsEquipment";
        public const string Version = "1.0.0";

        internal static EngiTurretsEquipmentPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger => Instance?.Logger;

        internal static ConfigEntry<bool> RequireGesture { get; private set; }
        internal static ConfigEntry<float> CooldownMultiplier { get; private set; }

        private static GameObject engiTurret;
        private static GameObject engiWalkerTurret;
        private static CharacterMaster engiTurretMaster;
        private static CharacterMaster engiWalkerTurretMaster;

        private void Start()
        {
            Instance = this;

            RequireGesture = Config.Bind("Main", "RequireGestureOfTheDrowned", false, "If set to true, turrets will only use equipment if they have Gesture of the Drowned. Otherwise they will use equipment when attacking.");
            CooldownMultiplier = Config.Bind("Main", "CooldownMultiplier", 1f, "Additional cooldown multiplier for the turrets.");

            if (Chainloader.PluginInfos.ContainsKey(RiskOfOptionsIntegration.RiskOfOptionsGUID))
            {
                RiskOfOptionsIntegration.Init();
            }

            engiTurret = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiTurretBody.prefab").WaitForCompletion();
            engiWalkerTurret = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiWalkerTurretBody.prefab").WaitForCompletion();
            if (!engiTurret.GetComponent<EquipmentSlot>())
            {
                engiTurret.AddComponent<EquipmentSlot>();
            }
            if (!engiWalkerTurret.GetComponent<EquipmentSlot>())
            {
                engiWalkerTurret.AddComponent<EquipmentSlot>();
            }

            var engiTurretMasterObject = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiTurretMaster.prefab").WaitForCompletion();
            engiTurretMaster = engiTurretMasterObject.GetComponent<CharacterMaster>();
            var engiWalkerTurretMasterObject = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiWalkerTurretMaster.prefab").WaitForCompletion();
            engiWalkerTurretMaster = engiWalkerTurretMasterObject.GetComponent<CharacterMaster>();

            if (!RequireGesture.Value)
            {
                UpdateShouldFireEquipment();
            }
            RequireGesture.SettingChanged += (s, e) => UpdateShouldFireEquipment();

            new ILHook(typeof(Inventory).GetMethod(nameof(Inventory.CalculateEquipmentCooldownScale), BindingFlags.NonPublic | BindingFlags.Instance), CalculateEquipmentCooldown);
        }

        private static void UpdateShouldFireEquipment()
        {
            foreach (var driver in engiTurretMaster.GetComponents<AISkillDriver>())
            {
                switch (driver.customName)
                {
                    case "FireAtEnemy":
                        driver.shouldFireEquipment = !RequireGesture.Value;
                        break;
                }
            }

            foreach (var driver in engiWalkerTurretMaster.GetComponents<AISkillDriver>())
            {
                switch (driver.customName)
                {
                    case "StrafeAndFireAtEnemy":
                    case "ChaseAndFireAtEnemy":
                        driver.shouldFireEquipment = !RequireGesture.Value;
                        break;
                }
            }
        }

        private void CalculateEquipmentCooldown(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(x => x.MatchRet()))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Call, typeof(EngiTurretsEquipmentPlugin).GetMethod(nameof(GetMultiplier), BindingFlags.NonPublic | BindingFlags.Static));
                c.Emit(OpCodes.Mul);
            }
            else
            {
                Logger.LogError("Couldn't apply cooldown scale hook");
            }
        }

        private static float GetMultiplier(Inventory inventory)
        {
            var master = inventory.GetComponent<CharacterMaster>();
            if (master)
            {
                var index = master.masterIndex;
                if (index == engiTurretMaster.masterIndex ||
                    index == engiWalkerTurretMaster.masterIndex)
                {
                    return CooldownMultiplier.Value;
                }
            }

            return 1;
        }
    }
}