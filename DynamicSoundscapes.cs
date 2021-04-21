using BepInEx;
using GorillaLocomotion;
using HarmonyLib;
using Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.VR;
using UnityEngine.XR;
using BepInEx.Configuration;


/*
add voice normalization
remove forest echo
add hand wind
change cosmetics sound position
add reverb to canyon houses
change cave ambience
make wind fade when stopping
add lava monke fire sounds with center panning
add low cut when you get tagged
hear wind for other people
add cave mine close reverb
cut length of tracks to 5 and increase quality
remove loud bird sound
make scream sound subtract wind sound
*/


namespace DynamicSoundscapes
{
    [BepInPlugin("org.auralius.monkeytag.ambientsounds", "Dynamic Soundscapes", "0.2.5.0")]
    [BepInProcess("Gorilla Tag.exe")]
    public class MonkePlugin : BaseUnityPlugin
    {
        static private ConfigEntry<float> configVolume;
        static private ConfigEntry<string> configForest;
        static private ConfigEntry<string> configCave;
        static private ConfigEntry<string> configCanyon;
        static private ConfigEntry<string> configCosmetics;
        static private ConfigEntry<string> configFall;
        static private ConfigEntry<string> configIntro;
        static private ConfigEntry<string> configComputer;
        static private ConfigEntry<string> configKeypress;
        static private ConfigEntry<string> configScream;

        public static String[] ambienceStrings = new string[] { }; // There has to be a better way to get it from the config!
        public static float volume = 0.08f;  // Set audio volume.
        private void Awake()
        {
            new Harmony("com.auralius.monkeytag.ambientsounds").PatchAll(Assembly.GetExecutingAssembly());

            ConfigFile customConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, "Dynamic Soundscapes.cfg"), true); // Create custom config file.

            configVolume = customConfig.Bind("Main", "Volume", 0.08f, "Max volume. ( Can get REALLY LOUD! Please try to not loose your hearing! )");
            configForest = customConfig.Bind("Sounds", "Forest", "https://cdn.discordapp.com/attachments/696322098305564682/832993410528575518/forest2.ogg", "Ambience played in forest area.");
            configCave = customConfig.Bind("Sounds", "Cave", "https://cdn.discordapp.com/attachments/696322098305564682/833034612467957780/cave2.ogg", "Ambience played in cave area.");
            configCanyon = customConfig.Bind("Sounds", "Canyon", "https://cdn.discordapp.com/attachments/696322098305564682/831511319479844894/canyon2.ogg", "Ambience played in canyon area.");
            configCosmetics = customConfig.Bind("Sounds", "Cosmetics", "https://cdn.discordapp.com/attachments/696322098305564682/831390225397710908/cosmetics.ogg", "Ambience played in cosmetic area.");
            configFall = customConfig.Bind("Sounds", "Falling", "https://cdn.discordapp.com/attachments/696322098305564682/831514441660629022/fall.ogg", "Sound played when falling");
            configIntro = customConfig.Bind("Sounds", "Intro", "https://cdn.discordapp.com/attachments/696322098305564682/832188272616275998/intro.ogg", "Sound played when plugin loads.");
            configComputer = customConfig.Bind("Sounds", "Computer", "https://cdn.discordapp.com/attachments/696322098305564682/833070725703139368/computer.ogg", "Ambience played from Gorilla OS computer");
            configKeypress = customConfig.Bind("Sounds", "Keypress", "https://cdn.discordapp.com/attachments/696322098305564682/832243629304578138/keypress.ogg", "Sound played when you press a key on the Gorilla OS computer.");
            configScream = customConfig.Bind("Sounds", "Void", "https://cdn.discordapp.com/attachments/737565897786523648/833131259374731324/scream.ogg", "Ambience when falling into the void.");

            ambienceStrings = new string[] // All the audio web links (or file links).
            {
                configForest.Value,
                configCave.Value,
                configCanyon.Value,
                configCosmetics.Value,
                configFall.Value,
                configIntro.Value,
                configComputer.Value,
                configKeypress.Value,
                configScream.Value
            };

            StartCoroutine(GetAudio()); // Download/get the audio clips.
        }

        public static AudioClip[] ambienceAudio = new AudioClip[9]; // All the audio clips.
        public static bool fetched = false; // Wait for the audio clips to load then run the main stuff of the mod.
        IEnumerator GetAudio() // Coroutine for loading audio as OGG and convert to audioClips.
        {
            for (int i = 0; i < ambienceStrings.Length; i++)
            {
                string path = string.Format("{0}/{1}", Application.streamingAssetsPath, ambienceStrings[i].GetHashCode()); // Get hash code of url link and use as file name.
                string url = System.IO.File.Exists(path) ? path : ambienceStrings[i]; // If the file exists then load that otherwise load from the internets.

                using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS)) // Use UnityWebRequest to get audio from the internets. Can also be used to get local file's and use those.
                {
                    yield return req.SendWebRequest(); // Send web request.

                    if (req.isNetworkError || req.isHttpError) // Shouldn't error, but if it does then log it to the console.
                        {
                            Console.WriteLine("Fetching track had error: " + req.error);
                    }
                    else
                    {
                        ambienceAudio[i] = DownloadHandlerAudioClip.GetContent(req); // Add audio to array.
                        System.IO.File.WriteAllBytes(path, req.downloadHandler.data); // Write to file.
                        Console.WriteLine("Got track " + i);
                    }
                }
            }
            fetched = true; // Finally got all the audio clips!
        }


        [HarmonyPatch(typeof(GorillaKeyboardButton))]
        [HarmonyPatch("OnTriggerEnter")]
        private class Keyboard_Patch
        {
            private static void Postfix(ref Collider collider)
            {
                if (fetched)
                {    
                    AudioSource.PlayClipAtPoint(ambienceAudio[7], collider.transform.position, volume); // Play keyboard sounds. https://www.youtube.com/watch?v=J---aiyznGQ
                }
            }
        }

        [HarmonyPatch(typeof(GorillaLocomotion.Player))]
        [HarmonyPatch("Update")]
        private class Soundscape_Patch
        {
            static List<AudioSource> sources = new List<AudioSource>();

            private enum Tracks : int // Tracks.
            {
                None,
                Forest,
                Cave,
                Canyon,
                Cosmetics,
                Falling, // Totally a track.
                Computer,
                Void = 9 // Oh no, this is bad!
            }
            private enum Areas : int // Areas.
            {
                None,
                Forest,
                Cosmetics,
                Cave,
                Canyon
            }

            private static Bounds[] boundList = new Bounds[] { }; // List of all audio bounds.

            private static string[] triggerList = new string[] { // List of triggers.
                "JoinPublicRoom (tree exit)",
                "JoinPublicRoom (cave entrance)",
                "LeavingCaveGeo",
                "EnteringCosmetics",
                "JoinPublicRoom (canyon)"
            };
            private enum UserReverbPresets : int // Reverb presets.
            {
                Room,
                Cave,
                Hallway,
                Forest,
                City
            }

            private static float[][] ReverbPresetArrays = new float[][] { // Oh god, please put this in another file!
                new float[] { // Room
                    0f, // Dry Level
                    -1000f, // Room
                    -454f, // Room HF
                    0f, // Room LF
                    0.4f, // Decay Time
                    0.83f, // Decay HF Ratio
                    -1646f, // Reflections Level
                    0f, // Reflections Decay
                    53f, // Reverb Level
                    0.003f, // Reverb Delay
                    5000f, // HF Reference
                    250f, // LF Reference
                    100f, // Diffusion
                    100f // Density
                },
                new float[] { // Cave
                    0f,
                    -1000f,
                    0f,
                    0f,
                    2.91f,
                    1.3f,
                    -602f,
                    0f,
                    -302f,
                    0.022f,
                    5000f,
                    250f,
                    100f,
                    100f
                },
                new float[] { // Hallway
                    0f,
                    -1000f,
                    -300f,
                    0f,
                    1.49f,
                    0.59f,
                    -1219f,
                    0f,
                    441f,
                    0.011f,
                    5000f,
                    250f,
                    100f,
                    100f
                },
                new float[] { // Forest
                    0f,
                    -1000f,
                    -3300f,
                    0f,
                    0.2f, // Changed from 1.49f to 
                    0.54f,
                    -2560f,
                    0f,
                    229f,
                    0.088f,
                    5000f,
                    250f,
                    79f,
                    100f
                },
                new float[] { // City
                    0f,
                    -1000f,
                    -800f,
                    0f,
                    1.49f,
                    0.67f,
                    -2273f,
                    0f,
                    -1691f,
                    0.011f,
                    5000f,
                    250f,
                    50f,
                    100f
                }
            };

            private static bool ranOnce = false; // Did it run once?
            private static float timer = 0f;
            private static bool audioChanged = true;
            private static bool firstLerp = true;
            private static Tracks curTrack = Tracks.Forest;
            private static Tracks lastTrack = Tracks.Forest;
            private static float lerpTrack = 0f;
            private static GorillaTriggerBox[] triggers = null;
            private static Bounds curBounds = new Bounds();

            private static AudioListener audio = new AudioListener();
            private static AudioReverbFilter audioReverb = new AudioReverbFilter();
            private static AudioLowPassFilter audioLowPass = new AudioLowPassFilter();
            private static UserReverbPresets curReverbPreset = UserReverbPresets.Room;
            private static UserReverbPresets lastReverbPreset = UserReverbPresets.Cave;

            private static int tagWait = 0;
            private static bool tagged = false;
            private static int tagTimer = 0;
            private static AudioSource pcSource = new AudioSource(); // This is the only 3D audio source in the entire mod. Bad!

            private static bool[] triggerSides = new bool[5];
            private static void SetLastTrack()
            {
                if (lastTrack != curTrack)
                    lastTrack = curTrack;
            }

            private static float timeSinceLoad = 0f;

            private static void SetReverbPreset() // Custom function for setting reverb preset.
            {
                if (curReverbPreset != lastReverbPreset)
                {
                    lastReverbPreset = curReverbPreset;
                    float[] arr = ReverbPresetArrays[(int)curReverbPreset];
                    audioReverb.dryLevel = arr[0];
                    audioReverb.room = arr[1];
                    audioReverb.roomHF = arr[2];
                    audioReverb.roomLF = arr[3];
                    audioReverb.decayTime = arr[4];
                    audioReverb.decayHFRatio = arr[5];
                    audioReverb.reflectionsLevel = arr[6];
                    audioReverb.reflectionsDelay = arr[7];
                    audioReverb.reverbLevel = arr[8];
                    audioReverb.reverbDelay = arr[9];
                    audioReverb.hfReference = arr[10];
                    audioReverb.lfReference = arr[11];
                    audioReverb.diffusion = arr[12];
                    audioReverb.density = arr[13];
                }
            }
            private static void Postfix(GorillaLocomotion.Player __instance)
            {
                try
                {
                    if (fetched)
                    {
                        Vector3 plyPos = __instance.bodyCollider.transform.position; // Get player position.

                        if (!ranOnce)
                        {
                            audio = Resources.FindObjectsOfTypeAll<AudioListener>()[0]; // Get audio listener.
                            audioReverb = audio.gameObject.AddComponent<AudioReverbFilter>();
                            audioLowPass = audio.gameObject.AddComponent<AudioLowPassFilter>();
                            audioReverb.enabled = true;
                            audioLowPass.enabled = false;
                            triggers = Resources.FindObjectsOfTypeAll<GorillaTriggerBox>();
                            for (int i = 0; i < ambienceAudio.Length; i++) // Create audio sources.
                            {
                                if (i != 7 || i != 8) // Is this actually working?
                                {
                                    AudioSource source = GorillaLocomotion.Player.Instance.gameObject.AddComponent<AudioSource>();
                                    sources.Add(source);
                                }
                            }
                            int cnt = 0;
                            foreach (AudioSource source in sources) // Set up audio sources.
                            {
                                source.volume = 0f;
                                source.loop = true;

                                source.clip = ambienceAudio[cnt];
                                source.Play();
                                cnt++;
                            }

                            foreach (GorillaTriggerBox trig in triggers) // Set up triggers.
                            {
                                if (triggerList.Contains<string>(trig.name))
                                    boundList.AddItem<Bounds>(trig.GetComponent<Collider>().bounds);
                            }

                            triggerSides[(int)Areas.None] = false; // How could I do this better? Actually I know, I'll do it later! Or will I?
                            triggerSides[(int)Areas.Forest] = false;
                            triggerSides[(int)Areas.Cosmetics] = false;
                            triggerSides[(int)Areas.Cave] = false;
                            triggerSides[(int)Areas.Canyon] = false;

                            ranOnce = true;
                            sources[5].volume = volume;
                            GameObject.Destroy(sources[5], ambienceAudio[5].length); // Destroy startup sound game object.
                            timeSinceLoad = Time.time;

                            //Inserting computer ambience here. Computer ambience is 3D sound for now.
                            pcSource = GorillaComputer.instance.gameObject.AddComponent<AudioSource>();
                            pcSource.volume = volume / 8;
                            pcSource.spatialBlend = 1f;
                            pcSource.maxDistance = 1f;
                            pcSource.clip = ambienceAudio[6];
                            pcSource.loop = true;
                            pcSource.Play();

                            Console.WriteLine(Application.streamingAssetsPath);

                            Console.WriteLine("Ambient Sounds is ready!");
                        }
                        if (Time.frameCount % 5 == 0) // OpTiMiZaTiOnS!?!?
                        {
                            Task t1 = new Task(() => // Run in new thread. Multi-threading for the win!
                            {
                                foreach (GorillaTriggerBox trigger in triggers) // Cycle through all triggers, maybe a better way?
                                {
                                    string name = trigger.name;
                                    Bounds bounds = trigger.GetComponent<Collider>().bounds;
                                    Vector3 closest = bounds.ClosestPoint(plyPos);
                                    Vector3 center = bounds.center;
                                    bool hit = bounds.Intersects(__instance.bodyCollider.bounds);
                                    float dist = (plyPos - closest).magnitude / 6;

                                    AudioSource source = new AudioSource();
                                    float tmpVolume = -0f;  
                                    Areas side = Areas.None;
                                    bool sideBool = false;
                                    switch (name) // Ignore everything in this switch statement, it's all a mess! AHHHHH!!
                                    {
                                        case "JoinPublicRoom (tree exit)":
                                            if (hit)
                                            {
                                                side = Areas.Forest;
                                                if (plyPos.z > center.z - 1)
                                                {
                                                    sideBool = true;
                                                    curReverbPreset = UserReverbPresets.Forest;
                                                    pcSource.volume = 0f;
                                                }
                                                else if (plyPos.x < center.x + 2 && plyPos.x > center.x - 3)
                                                {
                                                    sideBool = false;
                                                    curReverbPreset = UserReverbPresets.Room;
                                                    pcSource.volume = volume / 8;
                                                }
                                            }
                                            source = sources[(int)Tracks.Forest - 1];

                                            if (!triggerSides[(int)Areas.Forest])
                                            {
                                                if (!triggerSides[(int)Areas.Cosmetics])
                                                    tmpVolume = (1 - dist) * volume;
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                source.panStereo = Vector3.Dot(-__instance.headCollider.transform.right, dir) / Mathf.Clamp(2 - dist, 0, 2);
                                            }
                                            else
                                            {
                                                source.panStereo = 0f;
                                            }

                                            break;
                                        case "JoinPublicRoom (cave entrance)":
                                            if (hit)
                                            {
                                                side = Areas.Cave;
                                                if (plyPos.z < center.z)
                                                {
                                                    curReverbPreset = UserReverbPresets.Cave;
                                                    sideBool = true;
                                                }
                                                else
                                                {
                                                    curReverbPreset = UserReverbPresets.Hallway;
                                                    sideBool = false;
                                                }
                                            }
                                            source = sources[(int)Tracks.Cave - 1];
                                            if (triggerSides[(int)Areas.Cave])
                                                tmpVolume = Mathf.Clamp(dist, 0, 1) * volume;
                                            else
                                            {
                                                tmpVolume = 0;

                                                if (triggerSides[(int)Areas.Cave])
                                                    if (plyPos.y < 20)
                                                    {
                                                        Console.WriteLine("1");
                                                        curReverbPreset = UserReverbPresets.Forest;
                                                    }
                                                    else
                                                    {
                                                        curReverbPreset = UserReverbPresets.Cave;
                                                    }
                                            }
                                            break;
                                        case "LeavingCaveGeo":
                                            if (hit)
                                                if (plyPos.y < center.y)
                                                {
                                                    curReverbPreset = UserReverbPresets.Hallway;
                                                }
                                                else
                                                {
                                                    curReverbPreset = UserReverbPresets.Room;
                                                }

                                            break;
                                        case "EnteringCosmetics":
                                            if (hit)
                                            {
                                                side = Areas.Cosmetics;
                                                if (plyPos.x < center.x)
                                                {
                                                    sideBool = true;
                                                    pcSource.volume = 0f;
                                                }
                                                else
                                                {
                                                    sideBool = false;
                                                    pcSource.volume = volume / 8;
                                                }
                                            }
                                            source = sources[(int)Tracks.Cosmetics - 1];

                                            if (triggerSides[(int)Areas.Cosmetics])
                                                tmpVolume = Mathf.Clamp(dist * 2, 0, 1) * volume;
                                            else
                                                tmpVolume = 0;
                                            break;
                                            /*
                                            if (!triggerSides[(int)Areas.Cosmetics] && !triggerSides[(int)Areas.Forest])
                                            {
                                                tmpVolume = (1 - dist * 3) * volume;
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                source.panStereo = Vector3.Dot(-__instance.headCollider.transform.right, dir) / Mathf.Clamp(2 - dist, 0, 2);
                                            }
                                            else
                                            {
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                source.panStereo = Vector3.Dot(-__instance.headCollider.transform.right, dir) / Mathf.Clamp(2 - dist, 0, 2);
                                                if (triggerSides[(int)Areas.Forest])
                                                    tmpVolume = 0;
                                            }
                                            */
                                            break;
                                        case "JoinPublicRoom (canyon)":
                                            if (hit)
                                            {
                                                side = Areas.Canyon;
                                                if (plyPos.z < center.z)
                                                {
                                                    sideBool = true;
                                                    curReverbPreset = UserReverbPresets.City;
                                                }
                                                else if (plyPos.x < center.x + 2 && plyPos.x > center.x - 3)
                                                {
                                                    sideBool = false;
                                                    curReverbPreset = UserReverbPresets.Room;
                                                }
                                            }
                                            source = sources[(int)Tracks.Canyon - 1];
                                            if (!triggerSides[(int)Areas.Canyon] && !triggerSides[(int)Areas.Forest])
                                            {
                                                tmpVolume = (1 - dist) * volume;
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                source.panStereo = Vector3.Dot(-__instance.headCollider.transform.right, dir) / Mathf.Clamp(2 - dist, 0, 2);
                                            }
                                            else
                                            {
                                                source.panStereo = 0f;

                                                if (triggerSides[(int)Areas.Forest])
                                                    tmpVolume = 0;

                                                if (triggerSides[(int)Areas.Canyon])
                                                    if (plyPos.y < 5)
                                                    {
                                                        curReverbPreset = UserReverbPresets.Hallway;
                                                    }
                                                    else
                                                    {
                                                        curReverbPreset = UserReverbPresets.City;
                                                    }
                                            }
                                            break;
                                        case "QuitBox":
                                            source = sources[(int)Tracks.Void - 1];
                                            tmpVolume = (1 - Mathf.Clamp((float)Math.Log(dist / 2.5f), 0, 1)) * volume * 2;
                                            //tmpVolume = (1 - dist / 25) * volume;
                                            break;
                                    }
                                    if(tmpVolume != -0f) // Weird crap here.
                                    {
                                        source.volume = tmpVolume;
                                    }
                                    if(side != Areas.None) // Why do I have Areas.None??
                                    {
                                        triggerSides[(int)side] = sideBool;
                                    }
                                }
                                // Falling and reverb.
                                AudioSource falling = sources[(int)Tracks.Falling - 1];
                                float vel = __instance.GetComponent<Rigidbody>().velocity.magnitude;
                                
                                falling.pitch = Mathf.Clamp((float)Math.Log(vel / 5), 0, 3);
                                falling.volume = (float)Math.Log(vel / 10) * volume * 12;

                                SetReverbPreset();
                            });
                            t1.Start();
                        }
                        float time = Time.time - timeSinceLoad; // Startup sound. OpTiMiZe!
                        if (time < 1)
                            AudioListener.volume = time;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("AmbientSounds Error! : " + err);
                }
            }
        }
    }
}
