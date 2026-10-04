using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Runs production methods, not a reimplementation of the balance rules.
public static partial class GuildBalanceVerification
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static int assertions;
    static readonly List<string> results = new List<string>();
    static object Get(object obj,string field) => obj.GetType().GetField(field,Flags).GetValue(obj);
    static void Set(object obj,string field,object value) => obj.GetType().GetField(field,Flags).SetValue(obj,value);
    static object Call(object obj,string method,params object[] args) => obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
    static void Check(bool value,string message) { assertions++; if(!value) throw new Exception(message); }
    static int Int(object obj,string field) => (int)Get(obj,field);
    static GuildHallRuntime Create()
    {
        var game = new GameObject("Balance test").AddComponent<GuildHallRuntime>();
        Call(game,"ApplyBalanceV05");
        return game;
    }
    static void Done(GuildHallRuntime game,string message)
    { UnityEngine.Object.DestroyImmediate(game.gameObject); results.Add(message); }

    public static void Run()
    {
        try
        {
            UnityEngine.Random.InitState(5005);
            WorkbookParity();
            Visits();
            EconomyAndTraining();
            QuestLifecycle();
            Presentation();
            string report = "PASS: " + assertions + " assertions\n" + string.Join("\n",results);
            File.WriteAllText("balance-v05-verification.txt",report);
            Debug.Log(report);
            EditorApplication.Exit(0);
        }
        catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    static void Visits()
    {
        var game=Create();
        var levels=(int[])Get(game,"facilityLevels");
        foreach(int guild in new[]{1,10}) foreach(int publicity in new[]{0,5}) foreach(int expansion in new[]{0,5})
        {
            Set(game,"guildLevel",guild); levels[2]=publicity; levels[0]=expansion;
            double sum=0;
            for(int i=0;i<10000;i++) { int n=(int)Call(game,"DrawVisitorCount"); Check(n>=0 && n<=2+expansion,"Seat limit"); sum+=n; }
            double expected=Math.Min((5+guild)*(.25+publicity*.03),2+expansion);
            Check(Math.Abs(sum/10000-expected)<.03,"Visitor expectation "+guild+"/"+publicity+"/"+expansion);
            results.Add("Visitors GL"+guild+" promo"+publicity+" expansion"+expansion+": "+(sum/10000).ToString("0.000"));
        }
        Set(game,"guildLevel",1); levels[0]=0; levels[2]=0;
        IList guests=(IList)Get(game,"guests");
        object bran=guests[1]; Set(bran,"gold",1234);
        var seen=new HashSet<int>{1,5};
        // Reproduce SeatOpeningRoster state without needing a rendered presentation.
        Set(game,"visitorNights",1);
        foreach(int id in seen) { Set(guests[id],"hasVisited",true); Set(guests[id],"lastVisitNight",1); }
        for(int night=1;night<=30;night++)
        {
            int[] roster=(int[])Call(game,"SelectEveningRoster");
            Check(roster.Length>=1 && roster.Length<=2,"Initial visit bounds");
            Check(new HashSet<int>(roster).Count==roster.Length,"Duplicate hero");
            int newcomers=0;
            foreach(int id in roster)
            {
                if(seen.Add(id))
                {
                    newcomers++;
                    Check(Int(guests[id],"level")==1,"Authored hero keeps fixed level 1");
                    int lv=Int(guests[id],"level");
                    Check(Int(guests[id],"gold")>=20+8*(lv-1) && Int(guests[id],"gold")<=36+12*(lv-1),"New wallet");
                }
            }
            Check(newcomers==(night<=3 || night==6 ? 1:0),"Finite authored newcomer cadence night "+night);
            if(night>=6) Check(seen.Count==6,"Sixth authored hero never arrived");
            Check(Int(game,"visitorNights")==night+1,"Visit clock must advance on return-only nights");
            Check(Int(bran,"gold")==1234,"Revisit wallet reset");
        }
        Check(guests.Count==6,"Procedural duplicate visitor was created");
        Check((int)Call(game,"GuildMemberCount")<=4,"Lodging exceeded");
        Done(game,"30 visitor nights: cadence, identities, unique seats, wallet retention.");
    }

    static void EconomyAndTraining()
    {
        var game=Create();
        IList guests=(IList)Get(game,"guests");
        object bran=guests[1];
        Array[] items=(Array[])Get(game,"inventory");
        for(int c=0;c<3;c++) for(int i=0;i<items[c].Length;i++)
            Check(Int(items[c].GetValue(i),"stock")==(c==0 && i<2 ? 2:0),"Opening stock");
        Set(game,"selectedGuest",1); Set(game,"selectedCategory",0); Set(bran,"foodPreference",0);
        Check(Int(items[0].GetValue(2),"stock")==0,"Grilled meat must not be in opening stock");
        int affinity=Int(bran,"affinity");
        Call(game,"ConfirmSale",items[0].GetValue(0));
        Check(Int(game,"guildGold")==50 && Int(bran,"gold")==16,"Sale money conservation");
        Check(Int(bran,"affinity")==affinity+9,"Preferred food affinity");
        Call(game,"ConfirmSale",items[0].GetValue(0));
        Check(Int(bran,"gold")==16,"Food night limit");
        Set(game,"day",2);
        Call(game,"ConfirmSale",items[0].GetValue(0));
        Check(Int(items[0].GetValue(0),"stock")==0,"Old food restocked");

        Set(game,"guildLevel",10); Set(bran,"gold",1000); Set(game,"selectedCategory",1);
        object sword=items[1].GetValue(4); Set(sword,"stock",2);
        Call(game,"ConfirmSale",sword);
        int after=Int(bran,"gold");
        object royal=items[1].GetValue(8); Set(royal,"stock",2);
        Call(game,"ConfirmSale",royal);
        Check(Int(bran,"gold")==after,"Weapon night limit");
        Set(game,"day",3);
        Call(game,"ConfirmSale",items[1].GetValue(0));
        Check(Int(bran,"gold")==after,"Downgrade sold");
        Call(game,"ChangeGuestBaseClass",bran,"도적");
        Check(Get(bran,"equippedWeapon")==null,"Invalid class equipment retained");
        object rogueStats=Call(game,"CalculateHeroStats","도적",Int(bran,"level"));
        Check(Get(bran,"stats").ToString()==rogueStats.ToString(),"Class stats not refreshed");

        int[] levels=(int[])Get(game,"facilityLevels");
        Set(game,"guildLevel",1); Set(game,"guildGold",10000);
        Call(game,"PurchaseFacilityUpgrade",0); Check(levels[0]==1 && Int(game,"guildGold")==9900,"Upgrade cost");
        Call(game,"PurchaseFacilityUpgrade",0); Check(levels[0]==1,"Guild upgrade gate");
        Set(game,"guildLevel",10);
        for(int i=0;i<3;i++) Call(game,"PurchaseFacilityUpgrade",0);
        Check(levels[0]==4 && (int)Call(game,"NightVisitorCapacity")==6,"Expansion max level and six seats");
        int goldAtMax=Int(game,"guildGold");
        Call(game,"PurchaseFacilityUpgrade",0);
        Check(levels[0]==4 && Int(game,"guildGold")==goldAtMax,"Expansion cannot exceed level four");
        Set(game,"guildLevel",10); levels[4]=5;
        Check((int)Call(game,"DailyActionCapacity")==11,"Max AP");
        Set(game,"guildLevel",1); levels[4]=0; levels[3]=1;
        Set(game,"dailyActionsRemaining",2);
        int xp=Int(bran,"experience");
        Call(game,"TrainGuest",bran); Call(game,"TrainGuest",bran);
        Check(Int(game,"dailyActionsRemaining")==1 && Int(bran,"experience")==xp+4,"Training cost and daily limit");
        Check(Math.Abs((float)Get(bran,"trainingRemainder")-.4f)<.001,"Training fractional XP lost");
        Done(game,"Sales, preference, equipment gates, class changes, facility gates, training AP/fractional XP.");
    }

    static void QuestLifecycle()
    {
        var game=Create();
        Call(game,"GenerateDailyQuests");
        IList quests=(IList)Get(game,"quests");
        Check(quests.Count==3,"Initial board count");
        object q=quests[0];
        Set(q,"collected",true); Set(q,"acceptedDay",1); Set(q,"deadlineDays",3); Set(q,"expiresOnDay",4);
        Set(game,"day",2); Call(game,"GenerateDailyQuests");
        Check(quests.Contains(q),"Accepted quest discarded next day");
        Set(game,"day",3); Call(game,"GenerateDailyQuests");
        Check(quests.Contains(q),"Quest expired one day early");
        Set(game,"selectedGuest",1); Set(q,"lastAttemptDay",3);
        Call(game,"RequestQuest",q);
        IList guests=(IList)Get(game,"guests");
        Check(Int(guests[1],"lastQuestRequestDay")==-1,"Same-night retry allowed");
        Set(game,"day",4); Call(game,"GenerateDailyQuests");
        Check(!quests.Contains(q),"Expired quest retained");
        Done(game,"Accepted 3-day quest persists, expires on day4, no same-night retry.");
    }

    static void Presentation()
    {
        GuildHallSceneSetup.PrepareRuntimeArt();
        var camera=new GameObject("Balance camera").AddComponent<Camera>();
        camera.aspect=1672f/941f;
        var bg=new GameObject("Guild Hall Pixel Art").AddComponent<SpriteRenderer>();
        bg.sprite=Resources.Load<Sprite>("GuildHallBackground-EmptyGuests");
        var view=camera.gameObject.AddComponent<GuildHallPresentation>();
        view.Initialize(camera);
        view.ConfigureGuests(new[]{0,1,2,3,4,5,0,1,2,3,4,5});
        for(int seed=0;seed<100;seed++)
        {
            view.Reseat(seed,new[]{7,9},2);
            Check(view.SeatForGuest[7]<2 && view.SeatForGuest[9]<2 && view.SeatForGuest[7]!=view.SeatForGuest[9],"Base seating / dynamic hero index");
        }
        view.Reseat(42,new[]{0,2,4,6,8,10},7);
        Check(view.Portraits.Length==6 && view.Renderers.Length==12,"Art identity coupled to hero identity");
        foreach(var portrait in view.Portraits) Check(portrait.width==512 && portrait.height==512,"Portrait crop changed");
        UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(bg.gameObject);
        results.Add("Presentation: 12 hero identities / 6 full sprites, 100 two-seat arrangements.");
    }
}
