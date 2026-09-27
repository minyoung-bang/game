using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GuildHallVerification
{
    public static void Run()
    {
        try
        {
            GuildHallSceneSetup.PrepareRuntimeArt();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Verification camera").AddComponent<Camera>();
            camera.tag="MainCamera"; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.04f,.04f,.07f); camera.aspect=1672f/941;
            var background = new GameObject("Guild Hall Pixel Art").AddComponent<SpriteRenderer>();
            background.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GuildHallPixelConcept.png");
            var view=camera.gameObject.AddComponent<GuildHallPresentation>(); view.Initialize(camera);
            string dir=Path.GetFullPath("../../outputs/GuildHallUnity/Preview");
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++) if(args[i]=="-guildVerificationOutput") dir=args[i+1];
            Directory.CreateDirectory(dir);
            var arrangements=new HashSet<string>();
            for(int seed=0;seed<100;seed++)
            {
                view.Reseat(seed); var occupied=new HashSet<int>(view.SeatForGuest);
                if(occupied.Count!=6) throw new Exception("Duplicate seat at seed "+seed);
                arrangements.Add(string.Join(",",view.SeatForGuest));
            }
            if(arrangements.Count<20) throw new Exception("Seat shuffle is not varying.");
            int[] smallRoster={0,2,4}; view.Reseat(23,smallRoster);
            var activeSeats=new HashSet<int>();
            for(int i=0;i<GuildHallPresentation.GuestCount;i++)
            {
                bool shouldBeActive=Array.IndexOf(smallRoster,i)>=0;
                if(view.Renderers[i].enabled!=shouldBeActive) throw new Exception("Active guest roster visibility mismatch at guest "+i);
                if(shouldBeActive && (view.SeatForGuest[i]<0 || !activeSeats.Add(view.SeatForGuest[i]))) throw new Exception("Active roster has a missing or duplicate seat.");
                if(!shouldBeActive && view.SeatForGuest[i]!=-1) throw new Exception("Inactive guest still owns a seat.");
            }
            if(activeSeats.Count!=smallRoster.Length) throw new Exception("Active roster seat count mismatch.");
            for(int i=0;i<6;i++)
            {
                if(view.Portraits[i].width!=512 || view.Portraits[i].height!=512) throw new Exception("Invalid full sprite dimensions");
                File.WriteAllBytes(Path.Combine(dir,"guest-"+i+".png"),view.Portraits[i].EncodeToPNG());
            }
            view.Reseat(17); Capture(camera,Path.Combine(dir,"seating.png"));
            Vector3 rest=view.Renderers[0].transform.localScale;
            var motion=view.Renderers[0].GetComponent<GuestSpriteMotion>();
            view.SetHovered(0); motion.Tick(1,1);
            if(view.Renderers[0].transform.localScale.x<=rest.x) throw new Exception("Hover failed to enlarge");
            Capture(camera,Path.Combine(dir,"hover.png"));
            view.SetHovered(-1); motion.Tick(1,1);
            if(Vector3.Distance(rest,view.Renderers[0].transform.localScale)>.0001f) throw new Exception("Hover did not restore");
            view.Reseat(42); Capture(camera,Path.Combine(dir,"seating-alternate.png"));
            File.WriteAllText(Path.Combine(dir,"verification.txt"),"PASS: Unity compilation and camera render; 100 seeds without duplicate seats; "+arrangements.Count+" distinct arrangements; six complete 512x512 portraits; hover grows and returns to rest.\n");
            Debug.Log("GUILD_SEATING_VERIFICATION_PASS");
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    private static void Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1672,941,24);
        camera.targetTexture=target; camera.aspect=1672f/941;
        camera.GetComponent<GuildHallPresentation>().FitCamera(); camera.Render();
        RenderTexture previous=RenderTexture.active; RenderTexture.active=target;
        var image=new Texture2D(1672,941,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1672,941),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=previous; camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
}
