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

namespace DynamicSoundscapes
{
    [BepInPlugin("org.auralius.monkeytag.ambientsounds", "Dynamic Soundscapes", "0.2.5.0")]
    [BepInProcess("Gorilla Tag.exe")]
    public class MonkePlugin : BaseUnityPlugin
    {
        private ConfigEntry<string> configForest;
        private ConfigEntry<string> configCave;
        private ConfigEntry<string> configCanyon;
        private ConfigEntry<string> configCosmetics;
        private ConfigEntry<string> configFall;
        private ConfigEntry<string> configIntro;
        private ConfigEntry<string> configKeypress;
        private void Awake()
        {
            new Harmony("com.auralius.monkeytag.ambientsounds").PatchAll(Assembly.GetExecutingAssembly());

            ConfigFile customConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, "Dynamic Soundscapes.cfg"), true);
            configForest = customConfig.Bind("Sounds", "Forest", "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg", "Ambience played in forest area.");
            configCave = customConfig.Bind("Sounds", "Cave", "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg", "Ambience played in cave area.");
            configCanyon = customConfig.Bind("Sounds", "Canyon", "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg", "Ambience played in canyon area.");
            configCosmetics = customConfig.Bind("Sounds", "Cosmetics", "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg", "Ambience played in cosmetic area.");
            configFall = customConfig.Bind("Sounds", "Falling", "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg", "Sound played when falling");
            configIntro = customConfig.Bind("Sounds", "Intro", "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg", "Sound played when plugin loads.");
            configKeypress = customConfig.Bind("Sounds", "Keypress", "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg", "Sound played when you press a key on the gorila OS computer.");


            StartCoroutine(GetAudio());
        }

        public static String[] ambienceStrings = new string[]{
            "https://cdn.discordapp.com/attachments/696322098305564682/832535170947088394/forest.ogg",
            "https://cdn.discordapp.com/attachments/696322098305564682/830529231624470548/cave.ogg",
            "https://cdn.discordapp.com/attachments/696322098305564682/831511319479844894/canyon2.ogg",
            "https://cdn.discordapp.com/attachments/696322098305564682/831390225397710908/cosmetics.ogg",
            "https://cdn.discordapp.com/attachments/696322098305564682/831514441660629022/fall.ogg",
            "https://cdn.discordapp.com/attachments/696322098305564682/832188272616275998/intro.ogg",
            "https://cdn.discordapp.com/attachments/696322098305564682/832243629304578138/keypress.ogg"
        };

        public static AudioClip[] ambienceAudio = new AudioClip[7];
        public static bool fetched = false;
        public static readonly float volume = 0.08f;
        IEnumerator GetAudio()
        {
            for (int i = 0; i < ambienceStrings.Length; i++)
            {
                string path = string.Format("{0}/{1}", Application.streamingAssetsPath, ambienceStrings[i].GetHashCode());
                string url = System.IO.File.Exists(path) ? path : ambienceStrings[i];

                using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
                {
                    yield return req.SendWebRequest();

                    if (req.isNetworkError || req.isHttpError)
                    {
                        Console.WriteLine("Fetching track had error: " + req.error);
                    }
                    else
                    {
                        ambienceAudio[i] = DownloadHandlerAudioClip.GetContent(req);
                        System.IO.File.WriteAllBytes(path, req.downloadHandler.data);
                        Console.WriteLine("Got track " + i);
                    }
                }
            }
            fetched = true;

        }


        [HarmonyPatch(typeof(GorillaKeyboardButton))]
        [HarmonyPatch("OnTriggerEnter")]
        private class Keyboard_Patch
        {
            private static void Postfix(ref Collider collider)
            {
                if (fetched)
                { // This was easy!
                    AudioSource.PlayClipAtPoint(ambienceAudio[6], collider.transform.position, volume);
                }
            }
        }

        [HarmonyPatch(typeof(GorillaLocomotion.Player))]
        [HarmonyPatch("Update")]
        private class Soundscape_Patch
        {
            static List<AudioSource> sources = new List<AudioSource>();

            private enum Tracks : int
            {
                None,
                Forest,
                Cave,
                Canyon,
                Cosmetics,
                Falling
            }
            private enum Areas : int
            {
                None,
                Forest,
                //Stump,
                Cosmetics,
                //CosmeticsLerp,
                Cave,
                //CaveLerpTop,
                //CaveLerpBottom,
                Canyon
                //CanyonLerp
            }

            private static Bounds[] boundList = new Bounds[] { };

            private static string[] triggerList = new string[] { "JoinPublicRoom (tree exit)", "JoinPublicRoom (cave entrance)", "LeavingCaveGeo", "EnteringCosmetics", "JoinPublicRoom (canyon)" };
            private enum UserReverbPresets : int
            {
                Room,
                Cave,
                Hallway,
                Forest,
                City,
                /*
                None,
                Cave,
                Mine,
                Outside,
                Stump,
                OutsideOcclusion
                */
            }

            private static float[][] ReverbPresetArrays = new float[][] {
                new float[] { // Room
                    0f,
                    -1000f,
                    -454f,
                    0f,
                    0.4f,
                    0.83f,
                    -1646f,
                    0f,
                    53f,
                    0.003f,
                    5000f,
                    250f,
                    100f,
                    100f
                },
                new float[] {
                    0f, // Cave
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
                new float[] {
                0f, // Hallway
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
                new float[] {
                    0f, // Forest
                    -1000f,
                    -3300f,
                    0f,
                    1.49f,
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
                new float[] {
                    0f, // City
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

            private static bool ranOnce = false;
            private static float timer = 0f;
            private static bool audioChanged = true;
            private static bool firstLerp = true;
            private static Tracks curTrack = Tracks.Forest;
            private static Tracks lastTrack = Tracks.Forest;
            //private static Areas curArea = Areas.Stump;
            //private static Areas lastArea = Areas.Stump;
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

            private static bool[] triggerSides = new bool[5];
            /*
            private static void SetLastArea()
            {
                if (lastArea != curArea)
                    lastArea = curArea;
            }
            */
            private static void SetLastTrack()
            {
                if (lastTrack != curTrack)
                    lastTrack = curTrack;
            }

            private static void SetReverbPreset()
            {
                if (curReverbPreset != lastReverbPreset)
                {
                    lastReverbPreset = curReverbPreset;
                    var arr = ReverbPresetArrays[(int)curReverbPreset];
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
                        var plyPos = __instance.bodyCollider.transform.position;

                        if (!ranOnce)
                        {
                            audio = Resources.FindObjectsOfTypeAll<AudioListener>()[0];
                            audioReverb = audio.gameObject.AddComponent<AudioReverbFilter>();
                            audioLowPass = audio.gameObject.AddComponent<AudioLowPassFilter>();
                            audioReverb.enabled = true;
                            audioLowPass.enabled = false;
                            triggers = Resources.FindObjectsOfTypeAll<GorillaTriggerBox>();
                            /*
                            foreach (var trig in triggers)
                            {
                                if (triggerList.Contains<string>(trig.name))
                                {
                                    var bounds = trig.GetComponent<Collider>().bounds;
                                    var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                    GameObject.Destroy(obj.GetComponent<Rigidbody>());
                                    GameObject.Destroy(obj.GetComponent<Collider>());
                                    obj.transform.position = bounds.center;
                                    obj.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
                                    //ALPHA STARTS HERE
                                    Material mat = obj.GetComponent<Renderer>().material;
                                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                    mat.SetInt("_ZWrite", 0);
                                    mat.DisableKeyword("_ALPHATEST_ON");
                                    mat.DisableKeyword("_ALPHABLEND_ON");
                                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                                    mat.renderQueue = 3000;
                                    mat.color = new Color(0.2f, 1f, 0.2f, 0.25f);
                                    //ALPHA ENDS HERE
                                }
                            }
                            */
                            for (int i = 0; i < ambienceAudio.Length; i++)
                            {
                                AudioSource source = GorillaLocomotion.Player.Instance.gameObject.AddComponent<AudioSource>();
                                sources.Add(source);
                            }
                            var cnt = 0;
                            foreach (AudioSource source in sources)
                            {
                                source.volume = 0f;
                                source.loop = true;

                                source.clip = ambienceAudio[cnt];
                                source.Play();
                                cnt++;
                            }

                            foreach (var trig in triggers)
                            {
                                boundList.AddItem<Bounds>(trig.GetComponent<Collider>().bounds);
                            }
                            triggerSides[(int)Areas.None] = false;
                            triggerSides[(int)Areas.Forest] = false;
                            triggerSides[(int)Areas.Cosmetics] = false;
                            triggerSides[(int)Areas.Cave] = false;
                            triggerSides[(int)Areas.Canyon] = false;

                            //curReverbPreset = UserReverbPresets.Cave;
                            //curReverbPreset = UserReverbPresets.Room;
                            ranOnce = true;
                            sources[5].volume = volume;
                            sources[5].bypassEffects = true;
                            //sources[5].PlayOneShot(ambienceAudio[5]);
                            GameObject.Destroy(sources[5], ambienceAudio[5].length);
                            Console.WriteLine("Ambient Sounds is ready!");
                            //sources[6].PlayOneShot(ambienceAudio[6]);
                            //GameObject.Destroy(sources[6], ambienceAudio[6].length);

                            GameObject.Find("Shoulder Camera").GetComponent<Camera>().enabled = false; ;
                        }
                        if (Time.frameCount % 5 == 0)
                        {
                            Task t1 = new Task(() =>
                            {
                                foreach (GorillaTriggerBox trigger in triggers)
                                {
                                    string name = trigger.name;
                                    Bounds bounds = trigger.GetComponent<Collider>().bounds;
                                    Vector3 closest = bounds.ClosestPoint(plyPos);
                                    Vector3 center = bounds.center;
                                    bool hit = bounds.Intersects(__instance.bodyCollider.bounds);
                                    //if (hit) Console.WriteLine(trigger.name);
                                    float dist = (plyPos - closest).magnitude / 6;

                                    //Array[] tmpTriggerSides = { Areas.None, false };
                                    //Areas tmpTriggerSides = Areas.None;
                                    //bool tmpTriggerSidesOn = false;
                                    AudioSource source = new AudioSource();
                                    float tmpVolume = -0f; // WTF?
                                    Areas side = Areas.None;
                                    bool sideBool = false;
                                    switch (name)
                                    {
                                        // replace with https://stackoverflow.com/a/29804967
                                        case "JoinPublicRoom (tree exit)":
                                            if (hit)
                                            {
                                                side = Areas.Forest;
                                                if (plyPos.z > center.z - 1)
                                                {
                                                    sideBool = true;
                                                    curReverbPreset = UserReverbPresets.Forest;
                                                }
                                                else if (plyPos.x < center.x + 2 && plyPos.x > center.x - 3)
                                                {
                                                    sideBool = false;
                                                    curReverbPreset = UserReverbPresets.Room;
                                                }
                                            }
                                            source = sources[(int)Tracks.Forest - 1];

                                            if (!triggerSides[(int)Areas.Forest])
                                            {
                                                if (!triggerSides[(int)Areas.Cosmetics])
                                                    tmpVolume = (1 - dist) * volume;
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                //source.panStereo -= Vector3.Dot(__instance.headCollider.transform.right, dir);
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
                                            if (!triggerSides[(int)Areas.Cave] && !triggerSides[(int)Areas.Forest])
                                            {
                                                tmpVolume = (1 - dist) * volume;
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                //source.panStereo -= Vector3.Dot(__instance.headCollider.transform.right, dir);
                                                source.panStereo = Vector3.Dot(-__instance.headCollider.transform.right, dir) / Mathf.Clamp(2 - dist, 0, 2);
                                            }
                                            else
                                            {
                                                if (triggerSides[(int)Areas.Forest])
                                                    tmpVolume = 0;
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
                                                }
                                                else
                                                {
                                                    sideBool = false;
                                                }
                                            }
                                            source = sources[(int)Tracks.Cosmetics - 1];

                                            if (!triggerSides[(int)Areas.Cosmetics] && !triggerSides[(int)Areas.Forest])
                                            {
                                                tmpVolume = (1 - dist * 3) * volume;
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                //source.panStereo -= Vector3.Dot(__instance.headCollider.transform.right, dir);
                                                source.panStereo = Vector3.Dot(-__instance.headCollider.transform.right, dir) / Mathf.Clamp(2 - dist, 0, 2);
                                            }
                                            else
                                            {
                                                //source.panStereo = 0f;
                                                Vector3 dir = (__instance.headCollider.transform.position - closest).normalized;
                                                source.panStereo = Vector3.Dot(-__instance.headCollider.transform.right, dir) / Mathf.Clamp(2 - dist, 0, 2);
                                                if (triggerSides[(int)Areas.Forest])
                                                    tmpVolume = 0;
                                            }
                                            break;
                                        case "JoinPublicRoom (canyon)":
                                            source = sources[(int)Tracks.Canyon - 1];
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
                                                        //Console.WriteLine("aw pepis");
                                                    }
                                                    else
                                                    {
                                                        curReverbPreset = UserReverbPresets.City;
                                                    }
                                            }
                                            break;
                                    }
                                    if(tmpVolume != -0f)
                                    {
                                        source.volume = tmpVolume;
                                    }
                                    if(side != Areas.None)
                                    {
                                        triggerSides[(int)side] = sideBool;
                                    }
                                }
                            });
                            t1.Start();
                        }
                        if (Time.frameCount % 5 == 0)
                        {
                            Task t2 = new Task(() =>
                            {
                                AudioSource falling = sources[(int)Tracks.Falling - 1];
                                float vel = __instance.GetComponent<Rigidbody>().velocity.magnitude;
                                falling.pitch = Mathf.Clamp((float)Math.Log(vel / 5), 0, 3);
                                falling.volume = (float)Math.Log(vel / 10);

                                //Console.WriteLine((float)Math.Pow(vel / 50, vel / 50));
                                //Console.WriteLine(curReverbPreset);
                                //Console.WriteLine(lastReverbPreset);


                                SetReverbPreset();
                            });
                            t2.Start();
                        }
                        float time = Time.timeSinceLevelLoad;
                        if (time < 1)
                            AudioListener.volume = time;
                        //AudioListener.volume = Mathf.Clamp((float)Math.Log((time + 1) * 2), 0, 1);
                        /*
                        for (int i = 0; i < sources.Count; i++)
                        {
                            AudioSource source = sources[i];
                            //Vector3 closest = boundList[i].ClosestPoint(plyPos);
                            //source.volume = (plyPos - closest).magnitude / 6;
                        }
                        */

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
/*
LeavingCaveGeo
EnteringCaveGeo
JoinPublicRoom (cave entrance)

<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
source.PlayOneShot(ambienceAudio[cnt], 1f);
                                    //Console.WriteLine("Balls");
                                    //__instance.GetComponent<Rigidbody>().velocity += new Vector3(0, 0, 10) * Time.deltaTime;
*/
//trig.GetComponent<MeshRenderer>().enabled = true;
//      for(int i = 0; i<ambienceAudio.){
//GorillaLocomotion.Player.Instance.gameObject.AddComponent<AudioSource>();
// }
/*
 * //static readonly AudioSource source = Resources.FindObjectsOfTypeAll<AudioSource>()[3];
if (audioChanged)
{
    if (Time.time - timer > 0.05)
    {
        lerpTrack += 0.01f;
        timer = Time.time;
        if (!firstLerp)
            sources[lastTrack].volume = Mathf.Lerp(volume, 0, lerpTrack);

        sources[curTrack].volume = Mathf.Lerp(0, volume, lerpTrack);

        if (lerpTrack >= 1f)
        {
            lerpTrack = 0f;
            firstLerp = false;
            Console.WriteLine("Lerped track!: " + curTrack);
            audioChanged = false;
        }
    }
}
*/

/*
if (__instance.GetComponent<Rigidbody>().transform.position.y < 0 && lastArea != 1)
{
    lastArea = 1;
    lastTrack = curTrack;
    curTrack = 1;
    audioChanged = true;
    Console.WriteLine("Cave track!");
}
else if (__instance.GetComponent<Rigidbody>().transform.position.y > 0 && lastArea != 0)
{
    lastArea = 0;
    lastTrack = curTrack;
    curTrack = 0;
    audioChanged = true;
    Console.WriteLine("Forest track!");
}
*/

/*


                        foreach (var trig in triggers)
                        {
                            Bounds bounds = trig.GetComponent<Collider>().bounds;
                            bool hit = bounds.Intersects(__instance.bodyCollider.bounds);
                            if (hit)
                            {
                                Vector3 center = trig.GetComponent<Collider>().bounds.center;
                                Console.WriteLine(trig.name);
                                bool refresh = true;
                                Areas area = Areas.None;
                                Tracks track = Tracks.None;
                                Console.WriteLine(plyPos.x < center.x + 2 && plyPos.x > center.x - 2);
                                switch (trig.name)
                                {
                                    case "JoinPublicRoom (tree exit)":
                                        if (plyPos.z > center.z - 1)
                                        {
                                            area = Areas.Forest;
                                            track = Tracks.Forest;
                                        }
                                        else if (plyPos.x < center.x + 2 && plyPos.x > center.x - 2.5)
                                        {
                                            area = Areas.Stump;
                                            track = Tracks.Forest;
                                        }
                                        break;
                                    default:
                                        refresh = false;
                                        break;
                                }
                                if (refresh)
                                {
                                    SetLastArea();
                                    curArea = area;
                                    SetLastTrack();
                                    curTrack = track;
                                    curBounds = bounds;
                                }
                            }
                        }
                        //https://docs.unity3d.com/ScriptReference/Collider.ClosestPointOnBounds.html

                        float lerp1 = 0f;
                        float lerp2 = 0f;
                        Vector3 closest = curBounds.ClosestPoint(plyPos);

                        switch (curArea)
                        {
                            case Areas.Stump:
                                lerp1 = 1 - (plyPos - closest).magnitude / 6;
                                lerp2 = 0;
                                break;
                            case Areas.Forest:
                                lerp1 = 1;
                                lerp2 = 0;
                                break;
                        }
                        /*
                        if (curArea == Areas.Stump)
                        {
                            lerp1 = 1 - (plyPos - closest).magnitude / 6;
                            lerp2 = 0;
                            //Console.WriteLine(Mathf.Lerp(0, volume, lerp1));
                            //Console.WriteLine("track: " + (int)curTrack);
                        }
                        
if (curArea != Areas.None && curTrack != Tracks.None)
{
    sources[(int)curTrack - 1].volume = Mathf.Lerp(0, volume, lerp1);
    if (curArea != Areas.Stump && curArea != Areas.Forest)
        sources[(int)lastTrack - 1].volume = Mathf.Lerp(0, volume, lerp2);
}


https://mynoise.net/NoiseMachines/meadowCreekSoundscapeGenerator.php?c=0&l=2020202020202020670000
LITERALLY THE GREATEST WEBSITE IN EXISTANCE!    
*/

/*
 * int tagMat = 0;
                                if (PhotonNetwork.InRoom && PhotonNetwork.IsConnectedAndReady)
                                    try
                                    {
                                        tagMat = (int)PhotonNetwork.LocalPlayer.CustomProperties["matIndex"];
                                    }
                                    catch { }
                                if (tagMat == 1 || tagMat == 2)
                                {
                                    if (tagWait != 1 && tagWait != 2)
                                    {
                                        tagged = true;
                                    }
                                }
                                else
                                    tagWait = 0;

                                if (tagged)
                                {
                                    if (tagWait != 1)
                                    {
                                        tagWait = 1;
                                        float tagTimer = Time.time;
                                        audioLowPass.enabled = true;
                                    }
                                    if (tagWait == 1)
                                    {
                                        audioLowPass.cutoffFrequency = (Time.time - tagTimer) * 7333;
                                        if (Time.time - 3 > tagTimer)
                                        {
                                            audioLowPass.enabled = false;
                                            tagWait = 2;
                                            tagged = false;
                                        }
                                    }
                                }
*/