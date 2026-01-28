using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using Raldi.NPCs;
using System.Collections.Generic;

namespace Raldi.API
{
    public static class RaldiEditorSupport
    {
        public const string creditName = "CreditCard";
        public const string beastQuarterName = "BeastQuarter";
        public const string glockName = "Glock (";
        private static MrBeast beast;
        private static ItemObject[] _glock;

        public static void AddEditorStuff(MrBeast beastNPC, string[] roomTextures, ItemObject[] _Glock)
        {
            beast = beastNPC;
            _glock = _Glock;
            LevelStudioPlugin loaderPlugin = LevelStudioPlugin.Instance;
            AssetManager assetMan = Plugin.assetMan;
            loaderPlugin.selectableTextures.Add(roomTextures[0]);
            loaderPlugin.selectableTextures.Add(roomTextures[1]);
            loaderPlugin.selectableTextures.Add(roomTextures[2]);
            EditorInterface.AddNPCVisual(Plugin.bnpcName, beastNPC);
            EditorLevelData.AddDefaultTextureAction((Dictionary<string, TextureContainer> dict) =>
            {
                dict.Add($"{Plugin.bnpcName}NPC_Room", new TextureContainer(roomTextures[0], roomTextures[1], roomTextures[2]));
            });
            EditorInterfaceModes.AddModeCallback(AddContent);
        }

        public static void AddContent(EditorMode mode, bool vanillaCompliant)
        {
            AssetManager assetMan = Plugin.assetMan;
            EditorInterfaceModes.AddToolToCategory(mode, "rooms", new RoomTool($"{Plugin.bnpcName}NPC_Room", "Editor_MrBeast_Room".GetSprite(folder: "Editor")));
            EditorInterfaceModes.AddToolToCategory(mode, "npcs", new NPCTool(Plugin.bnpcName, "Editor_MrBeast_NPC".GetSprite(folder: "Editor")));
            EditorInterfaceModes.AddToolToCategory(mode, "items", new ItemTool(creditName, Plugin.Instance._CreditCard.itemSpriteSmall));
            EditorInterfaceModes.AddToolToCategory(mode, "items", new ItemTool(beastQuarterName, Plugin.Instance._BeastQuarter.itemSpriteSmall));
            for (int i = 0; i < _glock.Length; i++)
            {
                EditorInterfaceModes.AddToolToCategory(mode, "items", new ItemTool(glockName + $"{i + 1})", _glock[i].itemSpriteSmall));
            }
            EditorInterfaceModes.AddToolToCategory(mode, "posters", new PosterTool(beast.Poster.baseTexture.name));
        }
    }

    public static class RaldiLoaderSupport
    {
        public static void AddLoaderStuff(MrBeast beastNPC, ItemObject _CreditCardItm, ItemObject _BeastQuarterItm, ItemObject[] _GlockItm)
        {
            LevelLoaderPlugin loaderPlugin = LevelLoaderPlugin.Instance;
            AssetManager assetMan = Plugin.assetMan;
            loaderPlugin.npcAliases.Add(Plugin.bnpcName, beastNPC);
            loaderPlugin.itemObjects.Add(RaldiEditorSupport.creditName, _CreditCardItm);
            loaderPlugin.itemObjects.Add(RaldiEditorSupport.beastQuarterName, _BeastQuarterItm);
            for (int i = 0; i < _GlockItm.Length; i++)
            {
                loaderPlugin.itemObjects.Add(RaldiEditorSupport.glockName + $"{i + 1})", _GlockItm[i]);
            }
            loaderPlugin.posterAliases.Add(beastNPC.Poster.baseTexture.name, beastNPC.Poster);
        }
    }
}