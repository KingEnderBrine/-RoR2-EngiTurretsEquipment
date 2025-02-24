using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using RiskOfOptions;
using RiskOfOptions.Options;
using UnityEngine;

namespace EngiTurretsEquipment
{
    public static class RiskOfOptionsIntegration
    {
        public const string RiskOfOptionsGUID = "com.rune580.riskofoptions";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Init()
        {
            var texture = new Texture2D(128, 128);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EngiTurretsEquipment.icon.png"))
            using (var reader = new BinaryReader(stream))
            {
                texture.LoadImage(reader.ReadBytes((int)stream.Length));
                var sprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0f, 0f));
                ModSettingsManager.SetModIcon(sprite);
            }

            ModSettingsManager.AddOption(
                new CheckBoxOption(EngiTurretsEquipmentPlugin.RequireGesture),
                EngiTurretsEquipmentPlugin.GUID,
                EngiTurretsEquipmentPlugin.Name);
            ModSettingsManager.AddOption(new FloatFieldOption(EngiTurretsEquipmentPlugin.CooldownMultiplier),
                EngiTurretsEquipmentPlugin.GUID,
                EngiTurretsEquipmentPlugin.Name);
        }
    }
}
