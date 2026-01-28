using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using Raldi.API;
using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Raldi
{
    public static class IvanLomAPI // Also referred to as StickyAPI, because of the port i'll work on
    {
        public static void LoadNPC(this CustomNPC character, Sprite sprite, SoundObject music, float speed = 12f)
        {
            if (character == null)
            {
                Debug.LogError("Custom NPC is null");
                return;
            }

            character.plugin = Plugin.Instance;

            character.data.audMan = character.GetComponent<AudioManager>() != null ? character.GetComponent<AudioManager>() : character.gameObject.AddComponent<AudioManager>();
            character.data.wahahAudMan = character.gameObject.AddComponent<PropagatedAudioManager>();
            if (music != null)
            {
                character.data.wahahAudMan.ReflectionSetVariable("soundOnStart", new SoundObject[] { music });
                character.data.wahahAudMan.ReflectionSetVariable("loopOnStart", true);
            }

            if (character.spriteRenderer[0] == null)
            {
                character.spriteRenderer[0] = character.gameObject.GetComponent<SpriteRenderer>() != null ?
                    character.gameObject.GetComponent<SpriteRenderer>() : character.gameObject.AddComponent<SpriteRenderer>();
            }

            character.Navigator?.SetSpeed(speed);
            character.spriteRenderer[0].transform.localScale = Vector3.one * 0.8f;
            character.spriteRenderer[0].transform.position += new Vector3(0.1f, 0.1f, 0f);
            character.spriteRenderer[0].sprite = sprite;
        }

        public static void SpawnNPC(this CustomNPC character, string floorName, int floorNumber, SceneObject sceneObject, int chance, params string[] potentialFloorNames)
        {
            if (character == null)
            {
                Debug.LogError("Custom NPC is null");
                return;
            }
            CustomLevelObject[] levelObjects = sceneObject.GetCustomLevelObjects();
            foreach (string availableFloorNames in potentialFloorNames)
            {
                if (floorName.StartsWith(availableFloorNames) || sceneObject.GetMeta().tags.Contains(availableFloorNames))
                {
                    sceneObject.potentialNPCs.Add(new WeightedNPC() { selection = character, weight = chance });
                    sceneObject.MarkAsNeverUnload();
                    break;
                }
            }
        }

        public static void AddItemInTheShop(this ItemObject itm, string floorName, int floorNumber, SceneObject sceneObject, int chance, params string[] potentialFloorNames)
        {
            if (itm == null)
            {
                Debug.LogError("Custom item is null");
                return;
            }
            CustomLevelObject[] levelObjects = sceneObject.GetCustomLevelObjects();
            foreach (string availableFloorNames in potentialFloorNames)
            {
                if (floorName.StartsWith(availableFloorNames) || sceneObject.GetMeta().tags.Contains(availableFloorNames))
                {
                    WeightedItemObject[] currentItems = sceneObject.shopItems;
                    int currentLength = currentItems != null ? currentItems.Length : 0;
                    WeightedItemObject[] newArray = new WeightedItemObject[currentLength + 1];

                    if (currentLength > 0)
                        Array.Copy(currentItems, newArray, currentLength);

                    newArray[currentLength] = new WeightedItemObject() { selection = itm, weight = chance };

                    sceneObject.shopItems = newArray;
                    sceneObject.MarkAsNeverUnload();
                    break;
                }
            }
        }

        public static void GenerateItem(this ItemObject itm, string floorName, int floorNumber, SceneObject sceneObject, int chance, params string[] potentialFloorNames)
        {
            if (itm == null)
            {
                Debug.LogError("Custom item is null");
                return;
            }
            CustomLevelObject[] levelObjects = sceneObject.GetCustomLevelObjects();
            foreach (string availableFloorNames in potentialFloorNames)
            {
                if (floorName.StartsWith(availableFloorNames) || sceneObject.GetMeta().tags.Contains(availableFloorNames))
                {
                    for (int i = 0; i < levelObjects.Length; i++)
                    {
                        if (levelObjects[i].IsModifiedByMod(Plugin.Instance.Info)) continue;
                        levelObjects[i].potentialItems = levelObjects[i].potentialItems.AddItem(new WeightedItemObject() { selection = itm, weight = chance }).ToArray();
                        levelObjects[i].MarkAsModifiedByMod(Plugin.Instance.Info);
                    }
                    break;
                }
            }
        }

        public static void SetPosAndRotation(this Transform transform, Vector3 pos, Quaternion rotation, bool local)
        {
            if (local)
            {
                transform.localPosition = pos;
                transform.localRotation = rotation;
                return;
            }
            transform.SetPositionAndRotation(pos, rotation);
        }

        public static void SetPosAndRotation(this Transform transform, Vector3 pos, Quaternion rotation) => transform.SetPositionAndRotation(pos, rotation);

        public static SoundObject GetSound(this string soundName, string subtitle, Color color, string format = ".ogg", SoundType sfxType = SoundType.Effect, bool hasSubtitle = true, string folder = "Sounds")
        {
            string fileName = FileNameConfig.GetFileName(soundName, soundName + format);
            if (!fileName.Contains(format))
            {
                fileName += format;
            }
            if (!Plugin.assetMan.ContainsKey(soundName))
            {
                Plugin.assetMan.Add(soundName, ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Plugin.Instance, folder, fileName), subtitle, sfxType, color, hasSubtitle ? -1f : 0f));
            }

            SoundObject sound = Plugin.assetMan.Get<SoundObject>(soundName);
            if (sound == null)
            {
                Debug.LogError("Custom SoundObject is null");
                return null;
            }
            return sound;
        }

        public static Sprite GetSprite(this string spriteName, string folder = "Sprites", string format = ".png", string secondFolder = "")
        {
            string fileName = FileNameConfig.GetFileName(spriteName, spriteName + format);
            if (!fileName.Contains(format))
            {
                fileName += format;
            }

            if (!Plugin.assetMan.ContainsKey(spriteName))
            {
                Texture2D texture;
                if (secondFolder != "")
                {
                    texture = AssetLoader.TextureFromMod(Plugin.Instance, folder, secondFolder, fileName);
                }
                else
                {
                    texture = AssetLoader.TextureFromMod(Plugin.Instance, folder, fileName);
                }

                Sprite sprite = AssetLoader.SpriteFromTexture2D(texture, 50f);
                Plugin.assetMan.Add(spriteName, sprite);
            }

            Sprite spr = Plugin.assetMan.Get<Sprite>(spriteName);
            if (spr == null)
            {
                Debug.LogError("Custom sprite is null");
                return null;
            }
            return spr;
        }

        public static Texture2D GetTexture(this string spriteName, string folder = "Sprites", string format = ".png")
        {
            string fn = FileNameConfig.GetFileName(spriteName, spriteName + format);
            if (!fn.Contains(format))
            {
                fn += format;
            }
            if (!Plugin.assetMan.ContainsKey(spriteName))
            {
                Plugin.assetMan.Add(spriteName, AssetLoader.TextureFromMod(Plugin.Instance, folder, fn));
            }
            Texture2D t = Plugin.assetMan.Get<Texture2D>(spriteName);
            if (t == null)
            {
                Debug.LogError("Custom texture is null");
                return null;
            }
            return t;
        }

        public static SoundObject GetRandomSound(this string subtitle, Color color, string format = ".ogg", params string[] soundName)
        {
            int sr = UnityEngine.Random.Range(0, soundName.Length);
            return GetSound(soundName[sr], subtitle, color, format);
        }

        public static Material GetDefaultSpriteMaterial()
        {
            Shader spriteShader = Shader.Find("Sprites/Default");

            if (spriteShader == null)
            {
                Debug.LogError("Unable to find shader");
                return null;
            }

            Material defaultSpriteMaterial = new Material(spriteShader);
            return defaultSpriteMaterial;
        }

        public static void SetText(ref TextMeshProUGUI text, string textName, Transform hudTransform)
        {
            text = new GameObject(textName).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(hudTransform, false);
            text.rectTransform.sizeDelta = new Vector2(500f, 80f);
            text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.localPosition = Vector3.zero;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.localPosition = Vector3.zero;
            text.gameObject.SetActive(true);
        }

        public static bool CreateFlagInStatement(out bool flag, bool val)
        {
            flag = val;
            return flag;
        }
    }
}