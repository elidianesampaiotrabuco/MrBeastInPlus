using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using Raldi.API;
using Raldi.NPCs;
using RaldiItems;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace Raldi
{
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {   
        public static Plugin Instance;
        public static AssetManager assetMan = new();

        public const string bnpcName = "MrBeast";

        public AudioManager loopAudio, audMan;

        public AudioManager LoopAudio
        {
            get
            {
                if (loopAudio == null)
                {
                    SetAudioMan(ref loopAudio, true, true);
                }
                return loopAudio;
            }
        }

        public AudioManager AudMan
        {
            get
            {
                if (audMan == null)
                {
                    SetAudioMan(ref audMan, true, false);
                }
                return audMan;
            }
        }

        private CustomNPC mrbeast_pref;

        public GameObject beasticator;
        public GameObject bulletPref;
        public TextMeshProUGUI stunText;

        public ItemObject _CreditCard;
        public ItemObject _BeastQuarter;
        public ItemObject[] _Glock = new ItemObject[maxGlocks];

        public SoundObject _BeastExplorer;
        public SoundObject _BeastWait;
        public SoundObject _BeastWon;
        public SoundObject _BeastChallengeIntro;
        public SoundObject _BeastChallengeInvite;
        public SoundObject _CardPickup;

        public ConfigEntry<bool> hardMode;

        public static int maxVariants = 11;
        private const string randomPosterDesc = "PST_PRI_MrBeast2_Variant";

        public static RoomCategory mrBeastRoomCat = EnumExtensions.ExtendEnum<RoomCategory>("BeastRoom");

        public void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Instance = this;
            harmony.PatchAllConditionals();

            hardMode = Config.Bind<bool>("Difficulty", "Hard Mode", false, "MrBeast will always know if you stole his credit card.");

            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadImportant, LoadingEventOrder.Pre);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad(), LoadingEventOrder.Pre);
            GeneratorManagement.Register(this, GenerationModType.Addend, AddStuff);

            ModdedSaveGame.AddSaveHandler(Info);

            RaldiManager raldiMan = gameObject.AddComponent<RaldiManager>();
            raldiMan.plugin = this;
        }

        public void CreateBulletPrefab()
        {
            bulletPref = new GameObject("BulletPref_DATA");
            DontDestroyOnLoad(bulletPref);

            var spriteRenderer = bulletPref.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = "spr_bullet".GetSprite();
            spriteRenderer.gameObject.AddComponent<SimpleBillboard>();
            spriteRenderer.sortingOrder = 99;

            bulletPref.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            var collider = bulletPref.AddComponent<SphereCollider>();
            collider.radius = 3.5f;
            collider.isTrigger = true;

            bulletPref.AddComponent<BulletProjectile>();
            bulletPref.SetActive(false);
        }

        public void ResetAudio()
        {
            if (loopAudio != null)
            {
                Destroy(loopAudio.gameObject);
            }
            if (audMan != null)
            {
                Destroy(audMan.gameObject);
            }
            loopAudio = null;
            audMan = null;
        }

        public void SetAudioMan(ref AudioManager aud, bool _2D, bool loop = false)
        {
            GameObject newAudioMan = new GameObject("New_AudioManager");
            DontDestroyOnLoad(newAudioMan);
            aud = newAudioMan.GetComponent<AudioManager>() ?? newAudioMan.AddComponent<AudioManager>();
            aud.audioDevice = newAudioMan.GetComponent<AudioSource>() ?? newAudioMan.AddComponent<AudioSource>();
            aud.SetLoop(loop);

            if (_2D)
            {
                aud.audioDevice.spatialBlend = 0f;
                aud.positional = false;
            }
        }

        public Entity mostRecentStunnedEntity;

        public IEnumerator StunEntity(Entity entity, HudManager hud)
        {
            try
            {
                audMan.PlaySingle(assetMan.Get<SoundObject>("Gun_Stun"));
                if (entity != null)
                {
                    entity.Squish(BulletProjectile.stunDuration);
                }
            }
            catch
            {
                Debug.LogError("Found problems in the StunEntity method.");
                yield break;
            }

            mostRecentStunnedEntity = entity;

            float timer = BulletProjectile.stunDuration;
            while (timer > 0f && entity != null && mostRecentStunnedEntity == entity)
            {
                timer -= Time.deltaTime;
                if (stunText != null)
                {
                    stunText.text = entity.name + " is stunned for " + (int)timer + " seconds!";
                    stunText.gameObject.SetActive(true);
                }
                else if (hud != null)
                {
                    IvanLomAPI.SetText(ref stunText, "StunText", hud.transform);
                }
                yield return null;
            }

            if ((mostRecentStunnedEntity == entity || entity == null) && stunText != null)
            {
                stunText.gameObject.SetActive(false);
            }
        }

        IEnumerator PreLoad()
        {
            yield return 1;
            yield return "loading raldi";
        }

        void LoadImportant()
        {
            FileNameConfig.LoadConfig(this);

            AssetLoader.LocalizationFromMod(this);

            assetMan.Add("TheftSong", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Music", FileNameConfig.GetFileName("mus_theft", "theft.ogg")), "Mus_Theft_Sub", SoundType.Music, Color.white, 0f));
            assetMan.Add("Nope", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("snd_nope", "nope.ogg")), "Sub_NOPE", SoundType.Voice, Color.white));

            assetMan.Add("Gun_Shoot", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("snd_glock_shoot", "shoot.ogg")), "Glock_Shoot", SoundType.Effect, Color.white));
            assetMan.Add("Gun_Reload", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("snd_glock_reload", "reload.ogg")), "Glock_Reload", SoundType.Effect, Color.white));
            assetMan.Add("Gun_Stun", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("snd_glock_stun", "stun.ogg")), "Glock_Stun", SoundType.Effect, Color.white));

            assetMan.Add("Beast_Explorer", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("explorer_beast", "explorer_beast.ogg")), "MrBeast_Explorer", SoundType.Voice, Color.cyan));
            assetMan.Add("Beast_STOLE", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("vo_beast_stole", "whothefuckstolecreditcard.ogg")), "MrBeast_STOLE", SoundType.Voice, Color.cyan));
            assetMan.Add("Beast_WHYHAVE", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("vo_beast_whyhave", "whydoyouhavemycard.ogg")), "MrBeast_WHYHAVE", SoundType.Voice, Color.cyan));
            assetMan.Add("Beast_Wait", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("vo_beast_wait", "waitasecond.ogg")), "MrBeast_Wait", SoundType.Voice, Color.cyan));
            assetMan.Add("Beast_Won", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("vo_beast_won", "wonchallenge.ogg")), "MrBeast_Won", SoundType.Voice, Color.cyan));
            assetMan.Add("Beast_ChallengeIntro", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("vo_beast_intro", "trapped100subscribers.ogg")), "MrBeast_ChallengeIntro", SoundType.Voice, Color.cyan));
            assetMan.Add("Beast_ChallengeInvite", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Sounds", FileNameConfig.GetFileName("vo_beast_challenge", "myyoutubechallenge.ogg")), "MrBeast_ChallengeInvite", SoundType.Voice, Color.cyan));

            _BeastExplorer = assetMan.Get<SoundObject>("Beast_Explorer");
            _BeastWait = assetMan.Get<SoundObject>("Beast_Wait");
            _BeastWon = assetMan.Get<SoundObject>("Beast_Won");
            _BeastChallengeInvite = assetMan.Get<SoundObject>("Beast_ChallengeInvite");
            _BeastChallengeIntro = assetMan.Get<SoundObject>("Beast_ChallengeIntro");

            _CardPickup = assetMan.Get<SoundObject>("TheftSong");

            Texture2D beastPosterTexture = "spr_mrbeast_poster".GetTexture();

                ItemObject creditCard = new ItemBuilder(Info) // create credit card
    .SetNameAndDescription("CreditCard",
    "Desc_CreditCard")
    .SetSprites("spr_creditcard_small".GetSprite(), "spr_creditcard_big".GetSprite()).SetEnum("CreditCard").SetShopPrice(200)
    .SetGeneratorCost(150).SetItemComponent<ITM_CreditCard>().SetPickupSound(_CardPickup).SetMeta(ItemFlags.Persists, ["money"]).Build();
                assetMan.Add("CreditCard", creditCard);
                _CreditCard = creditCard;

                LevelLoaderPlugin levelLoader = LevelLoaderPlugin.Instance;
                assetMan.Add("BeastDoorMats", ObjectCreators.CreateDoorDataObject("BeastDoor", "BeastDoor_Open".GetTexture(folder: "Rooms"), "BeastDoor_Closed".GetTexture(folder: "Rooms")));
                levelLoader.roomSettings.Add($"{bnpcName}NPC_Room", new RoomSettings(mrBeastRoomCat, RoomType.Room, new Color(172f / 255f, 0f, 252f / 255f), assetMan.Get<StandardDoorMats>("BeastDoorMats"), null));
                levelLoader.roomTextureAliases.Add("BeastFloor", "BeastFloor".GetTexture(folder: "Rooms"));
                levelLoader.roomTextureAliases.Add("BeastWall", "BeastWall".GetTexture(folder: "Rooms"));
                levelLoader.roomTextureAliases.Add("BeastCeil", "BeastCeil".GetTexture(folder: "Rooms"));

                List<WeightedRoomAsset> potentialBeastRooms = new List<WeightedRoomAsset>();
                string[] roomPaths = Directory.GetFiles(Path.Combine(AssetLoader.GetModPath(this), "Rooms"), "*.rbpl");
                for (int i = 0; i < roomPaths.Length; i++)
                {
                    BinaryReader reader = new BinaryReader(File.OpenRead(roomPaths[i]));
                    BaldiRoomAsset formatAsset = BaldiRoomAsset.Read(reader);
                    reader.Close();
                    ExtendedRoomAsset asset = LevelImporter.CreateRoomAsset(formatAsset);

                    if (formatAsset.basicObjects.Find(x => x.prefab == "locker") != null)
                    {
                        asset.basicSwaps = new()
                    {
                        new()
                        {
                            chance = 0.05f,
                            potentialReplacements =
                            [
                                new()
                                {
                                    weight = 100,
                                    selection = levelLoader.basicObjects["bluelocker"].transform
                                }
                            ],
                            prefabToSwap = levelLoader.basicObjects["locker"].transform
                        }
                    };
                    }
                    asset.lightPre = levelLoader.lightTransforms["standardhanging"];
                    potentialBeastRooms.Add(new()
                    {
                        selection = asset,
                        weight = 1000
                    });
                }

                MrBeast beast = new NPCBuilder<MrBeast>(Info) // spawn poop quarter generator npc from raldi
        .SetName(bnpcName).SetEnum(bnpcName)
        .SetMinMaxAudioDistance(1, 900).IgnorePlayerOnSpawn()
        .AddLooker().AddTrigger()
        .AddSpawnableRoomCategories(mrBeastRoomCat)
        .AddPotentialRoomAssets(potentialBeastRooms.ToArray())
        .SetPoster(beastPosterTexture, "PST_PRI_MrBeast1", "PST_PRI_MrBeast2_Variant1").Build();

                beast.LoadNPC("spr_MrBeast".GetSprite(), "mus_theme".GetSound(string.Empty, Color.cyan, ".ogg", SoundType.Music, hasSubtitle: false, folder: "Music"), 12f);
                assetMan.Add($"{bnpcName}NPC", beast);

            mrbeast_pref = beast;

                ItemObject quarterItem = new ItemBuilder(Info) // make a dublicate of the quarter
        .SetEnum("Quarter").SetNameAndDescription("ITM_Quarter", "Desc_Quarter")
        .SetShopPrice(50).SetGeneratorCost(250).SetItemComponent<ITM_Quarter>().SetSprites("spr_quarter_small".GetSprite(), "spr_quarter_big".GetSprite())
        .SetMeta(ItemFlags.None, []).Build();

                assetMan.Add("MrBeastQuarter", quarterItem);
                _BeastQuarter = quarterItem;

            ItemObject[] itemVersions = new ItemObject[maxGlocks];
            for (int i = 0; i < maxGlocks; i++)
            {
                int useCount = i + 1;
                itemVersions[i] = new ItemBuilder(Info)
    .SetEnum($"Glock ({useCount})").SetNameAndDescription("ITM_Glock (" + useCount + ")", "Desc_Glock")
    .SetShopPrice(400).SetGeneratorCost(400).SetItemComponent<ITM_Glock>().SetSprites("spr_glock_small".GetSprite(), "spr_glock_big".GetSprite())
    .SetMeta(ItemFlags.CreatesEntity, []).Build();

                assetMan.Add($"Glock (" + useCount + ")", itemVersions[i]);

                ITM_Glock itemComponent = itemVersions[i].item as ITM_Glock;
                if (itemComponent != null)
                {
                    itemComponent.uses = useCount;
                }
            }

            foreach (var version in itemVersions)
            {
                ITM_Glock component = version.item as ITM_Glock;
                if (component != null)
                {
                    component.variants = itemVersions;
                }
            }

            _Glock = itemVersions;

            RaldiLoaderSupport.AddLoaderStuff(beast, _CreditCard, _BeastQuarter, _Glock);
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
            {
                RaldiEditorSupport.AddEditorStuff(beast, ["BeastFloor", "BeastWall", "BeastCeil"], _Glock);
            }
        }

        void AddStuff(string floorName, int floorNumber, SceneObject sceneObject)
        {
            try
            {
                string newPosterDesc = randomPosterDesc + Random.Range(0, maxVariants);
                assetMan.Get<MrBeast>(bnpcName + "NPC").SpawnNPC(floorName, floorNumber, sceneObject, 1000, "F", "endless");
                _CreditCard.GenerateItem(floorName, floorNumber, sceneObject, 1000, "F", "endless");
                _Glock[Random.Range(1, maxGlocks)].GenerateItem(floorName, floorNumber, sceneObject, 500, "F", "endless");
                _Glock[Random.Range(1, maxGlocks)].AddItemInTheShop(floorName, floorNumber, sceneObject, 500, "F", "endless");
                _BeastQuarter.AddItemInTheShop(floorName, floorNumber, sceneObject, 500, "F", "endless");
                mrbeast_pref.Poster.textData[1].textKey = newPosterDesc;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public const int maxGlocks = 5;
    }

    public struct PluginInfo
    {
        public const string PLUGIN_GUID = "ilandsticky.bbplus.mrbeast";
        public const string PLUGIN_NAME = "MrBeast in BB+";
        public const string PLUGIN_VERSION = "1.2.0";
    }
}