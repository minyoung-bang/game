using System.Collections.Generic;
using UnityEngine;
using GuildCombat;

public sealed partial class GuildHallRuntime
{
    private Session combat;
    private BattleRequest combatRequest;
    private readonly List<int> combatParty = new List<int>();
    private readonly Dictionary<int,float> combatQueueY = new Dictionary<int,float>();
    private readonly HashSet<string> observedEnemySkills = new HashSet<string>();
    private readonly HashSet<string> encounteredEnemyKinds = new HashSet<string>();
    private Skill combatSelectedSkill;
    private Unit combatTarget, combatInspected;
    private int combatTab, combatItemIndex=-1, combatActorId=-1, combatWave;
    private float combatEnemyAt;
    private string combatMessage="";
    private Vector2 combatItemScroll;
    private GUIStyle combatSmallStyle;

    private BattleRequest PreparedDeparture()
    {
        foreach(var request in battleRequests)
            if(request.acceptedDay>=0 && request.prepared) return request;
        return null;
    }

    // Called once from the evening's end-day button. Invalid preparation leaves the evening open.
    private bool TryDepartForCombat()
    {
        BattleRequest request=PreparedDeparture();
        if(request==null) return false;
        var party=new List<int>();
        foreach(int id in request.party)
            if(id>=0 && id<guests.Count && guests[id].guildMember && !party.Contains(id)) party.Add(id);
        if(party.Count<1 || party.Count>4)
        { ShowToast("길드원 1~4명을 다시 편성해 주세요."); return true; }
        foreach(int id in party)
        {
            bool usable=false, freeAction=false;
            foreach(string skill in LearnedSkillsFor(guests[id]))
            {
                Skill definition=BasicSkills.Resolve(skill);
                if(definition.Implemented) { usable=true; if(definition.Mana==0) freeAction=true; }
            }
            if(!usable || !freeAction) { ShowToast(guests[id].name+"의 기본 스킬 데이터를 확인해 주세요."); return true; }
        }
        if(guildGold<request.expense) { ShowToast("출전 준비금 "+request.expense+" G가 필요합니다."); return true; }
        var heroes=new List<Unit>();
        for(int position=0;position<party.Count;position++)
        {
            int id=party[position]; Guest g=guests[id]; RefreshHeroStats(g);
            // Mana curves are temporary combat data until the combat balance sheet is established.
            int mana=(g.baseClass=="마법사" ? 20+3*(g.level-1) : 12+2*(g.level-1));
            var unit=new Unit { Id=id, Name=g.name, Job=g.baseClass, Level=g.level, Ally=true, Position=position,
                MaxHp=g.stats.hp, Hp=g.stats.hp, MaxMana=mana, Mana=mana, Speed=g.stats.speed,
                Attack=g.stats.attack+(CanEquip(g,g.equippedWeapon) && g.equippedWeapon!=null ? g.equippedWeapon.attackBonus:0),
                Defense=g.stats.defense+(CanEquip(g,g.equippedArmor) && g.equippedArmor!=null ? g.equippedArmor.defenseBonus:0) };
            foreach(string idSkill in LearnedSkillsFor(g))
                unit.Skills.Add(BasicSkills.Resolve(idSkill));
            heroes.Add(unit);
        }
        combatRequest=request; combatParty.Clear(); combatParty.AddRange(party); combatWave=1;
        combat=new Session(heroes,CreateCombatEnemies());
        guildGold-=request.expense;
        combatMessage="스킬 첫 클릭: 고정 사거리 확인 · 다시 클릭: 사용 · 캐릭터 클릭: 정보 확인";
        combatQueueY.Clear(); combatInspected=null; combatTab=0; ResetCombatSelection();
        combatActorId=-1; combatEnemyAt=Time.unscaledTime+1.2f;
        ClosePopup(); battlePreparationOpen=false; worldMapOpen=false; managementPage=0;
        presentation.SetHovered(-1); presentation.SetGuildmasterHovered(false);
        foreach(var renderer in presentation.Renderers) renderer.enabled=false;
        return true;
    }

    private List<Unit> CreateCombatEnemies()
    {
        if (combatTestMode) return CreateCombatTestEnemies();
        int level=combatRequest.recommendedLevel;
        var enemies=new List<Unit>();
        for(int i=0;i<Mathf.Clamp(combatRequest.partySize,1,4);i++)
        {
            string[] kinds=combatRequest.enemyKinds;
            string kind=kinds.Length==0 ? "미확인 마물" :
                kinds.Length==1 ? kinds[0] :
                (combatWave==combatRequest.waves && i==combatRequest.partySize-1 ? kinds[kinds.Length-1] : kinds[i%kinds.Length]);
            bool elite=kinds.Length>1 && kind==kinds[kinds.Length-1];
            int hp=16+level*5+(combatWave-1)*3+(elite ? 8+level*2 : 0);
            var enemy=new Unit { Id=100+i, Name=kind+" "+(i+1), Job=kind, Level=level, Ally=false,
                Position=i, Hp=hp, MaxHp=hp, Attack=4+level+(elite ? 1 : 0),
                Defense=level/3+(elite ? 1 : 0), Speed=3+(i%2)+level/5 };
            EnemySkills.AddFor(enemy);
            enemies.Add(enemy);
        }
        return enemies;
    }

    private void Update()
    {
        if(combat==null) return;
        for(int i=0;i<combat.Queue.Count;i++)
        {
            int id=combat.Queue[i].Id; float y=142+i*74;
            if(!combatQueueY.ContainsKey(id)) combatQueueY[id]=y+32;
            combatQueueY[id]=Mathf.MoveTowards(combatQueueY[id],y,500*Time.unscaledDeltaTime);
        }
        Unit actor=combat.Active;
        if(actor==null) return;
        if(combatActorId!=actor.Id)
        {
            combatActorId=actor.Id; ResetCombatSelection();
            combatEnemyAt=Time.unscaledTime+1.1f;
        }
        if(!actor.Ally && combatInspected==null && Time.unscaledTime>=combatEnemyAt)
        {
            var targets=combat.Units.FindAll(u=>u.Ally && u.Alive &&
                (u.StealthTurns==0 || !combat.Units.Exists(other=>other.Ally && other.Alive && other.StealthTurns==0)));
            if(actor.Skills.Count>0)
            {
                // Enemy skills are ordered from preferred to fallback. The nearest
                // reachable hero is chosen, making a front-line tank meaningful.
                targets.Sort((a,b)=>a.Position.CompareTo(b.Position));
                foreach(Skill skill in actor.Skills)
                    foreach(Unit target in targets)
                        if(skill.CanReach(actor.Position,target.Position) && combat.EnemyUseSkill(skill,target.Id))
                        {
                            ObserveEnemySkill(skill);
                            CombatActionFinished();
                            return;
                        }
            }
            else if(targets.Count>0 && combat.EnemyAttack(targets[Random.Range(0,targets.Count)].Id)) CombatActionFinished();
        }
    }

    private void ObserveEnemySkill(Skill skill)
    {
        observedEnemySkills.Add(skill.Id);
        if(combatTestMode) return;
        PlayerPrefs.SetInt("GuildHallKnownEnemySkill."+skill.Id,1);
        PlayerPrefs.Save();
    }

    private bool HasObservedEnemySkill(Skill skill)
        => observedEnemySkills.Contains(skill.Id) || PlayerPrefs.GetInt("GuildHallKnownEnemySkill."+skill.Id,0)==1;

    private bool HasEncounteredEnemyKind(string kind)
        => encounteredEnemyKinds.Contains(kind) || PlayerPrefs.GetInt("GuildHallKnownEnemyKind."+kind,0)==1;

    private void RecordEncounteredEnemies()
    {
        foreach(Unit enemy in combat.Units)
        {
            if(enemy.Ally) continue;
            encounteredEnemyKinds.Add(enemy.Job);
            if(!combatTestMode) PlayerPrefs.SetInt("GuildHallKnownEnemyKind."+enemy.Job,1);
        }
        if(!combatTestMode) PlayerPrefs.Save();
    }

    private void ResetCombatSelection()
    { combatSelectedSkill=null; combatTarget=null; combatItemIndex=-1; }

    private void CombatActionFinished()
    {
        combatMessage=combat.LastMessage;
        ResetCombatSelection(); combatActorId=-1; combatEnemyAt=Time.unscaledTime+1.1f;
    }

    private void SelectCombatSkill(Skill skill)
    {
        if(combat.Active==null || !combat.Active.Ally) return;
        if(combatSelectedSkill!=skill)
        {
            combatSelectedSkill=skill; combatItemIndex=-1;
            combatTarget=skill.Self ? combat.Active : combat.Units.Find(u=>combat.ValidTarget(skill,u));
            if(skill.Effect==SkillEffect.Heal)
            {
                foreach(Unit u in combat.Units)
                    if(combat.ValidTarget(skill,u) && (combatTarget==null || u.MaxHp-u.Hp>combatTarget.MaxHp-combatTarget.Hp))
                        combatTarget=u;
            }
            combatMessage=skill.Description+(skill.Effect==SkillEffect.Heal ? " · 아군 정보 창에서 회복 대상 변경 가능" : " · 범위는 스킬에 고정")+" · 다시 누르면 사용";
            return;
        }
        if(!combat.CanUse(skill))
        { combatMessage=!skill.Implemented ? "이 스킬의 전투 효과는 준비 중입니다." : combat.Active.Mana<skill.Mana ? "마나가 부족합니다." : "사용 위치: "+BasicSkills.Positions(skill.UserPositions)+" · 화살표로 이동하세요."; return; }
        if(combatTarget==null || !combat.UseSkill(skill,combatTarget.Id))
        { combatMessage=skill.Effect==SkillEffect.Heal ? "앞에 회복할 아군이 없습니다. 위치를 바꿔 주세요." : "사거리 안에 대상이 없습니다. 화살표로 이동하세요."; return; }
        CombatActionFinished();
    }

    private void SelectCombatItem(int index)
    {
        Unit actor=combat.Active;
        if(actor==null || !actor.Ally || index<0 || index>=inventory[0].Length) return;
        ShopItem item=inventory[0][index];
        if(item.stock<=0 || item.hpRecovery<=0) return;
        if(combatItemIndex!=index)
        {
            combatItemIndex=index; combatSelectedSkill=null; combatTarget=actor;
            combatMessage=item.name+" · 현재 행동 중인 "+actor.Name+"의 체력 "+item.hpRecovery+" 회복 · 다시 누르면 사용하고 턴 종료";
            return;
        }
        if(!combat.UseHealingItem(item.hpRecovery))
        { combatMessage="현재 행동 중인 용사의 체력이 이미 가득 찼습니다."; return; }
        if (!combatTestMode) item.stock--;
        CombatActionFinished();
    }

    private void InspectCombatUnit(Unit unit)
    {
        if(combat.Active!=null && combat.Active.Ally)
        {
            // Unit clicks inspect information; the skill's fixed reach determines its targets.
        }
        combatInspected=unit;
    }

    private void ContinueCombatWave()
    {
        RecordEncounteredEnemies();
        var heroes=combat.Units.FindAll(u=>u.Ally && u.Alive);
        foreach(var hero in heroes)
        {
            hero.Guarding=false; hero.Focused=false; hero.NextAttackBonus=0;
            hero.GuardPercent=hero.GuardHits=hero.GuardTurns=hero.SanctuaryTurns=hero.SanctuaryAuraTurns=hero.SanctuaryAuraAttack=hero.DodgeTurns=0;
            hero.CounterTurns=hero.CounterPower=hero.StealthTurns=hero.MarkTurns=0;
            hero.PoisonStacks=hero.PoisonTurns=hero.PoisonAttack=0;
            hero.Poison.Clear();
            hero.AttackDownPercent=hero.AttackDownTurns=hero.DefenseDownPercent=hero.DefenseDownTurns=0;
            hero.DamageTakenBonus=hero.DamageTakenTurns=hero.SlowPercent=hero.SlowTurns=hero.SlowUntilRound=0;
            hero.SpeedBoostPercent=hero.SpeedBoostTurns=hero.SpeedBoostUntilRound=hero.StunTurns=0;
        }
        combatWave++;
        combat=new Session(heroes,CreateCombatEnemies());
        combatQueueY.Clear(); combatActorId=-1; ResetCombatSelection();
        combatMessage="다음 전투 · 체력과 마나가 이어집니다.";
    }

    private void FinishCombat()
    {
        if(combat==null || combat.Result==Outcome.Fighting) return;
        RecordEncounteredEnemies();
        if (combatTestMode) { ExitCombatTest(); return; }
        if(combat.Result==Outcome.Victory && combatWave<combatRequest.waves) { ContinueCombatWave(); return; }
        bool victory=combat.Result==Outcome.Victory;
        if(victory)
        {
            guildGold+=combatRequest.reward; AddGuildExperience(combatRequest.guildExperience);
            foreach(int id in combatParty) GrantHeroExperience(guests[id],combatRequest.heroExperience);
            combatRequest.acceptedDay=-1; combatRequest.party.Clear();
        }
        combatRequest.prepared=false;
        combat=null; combatRequest=null; combatInspected=null; ResetCombatSelection();
        AdvanceToNextDay();
        ShowToast(victory ? "전투 의뢰 완료 · 보상을 받았습니다." : "전투 패배 · 의뢰를 다시 준비할 수 있습니다.");
    }

    private static Rect CombatUnitRect(bool ally,int position)
        => new Rect(ally ? 760-position*190 : 1040+position*200, 246, 166, 370);

    private void DrawCombatScreen()
    {
        if(combatSmallStyle==null) combatSmallStyle=new GUIStyle(labelStyle) { fontSize=18, alignment=TextAnchor.MiddleCenter };
        DrawCombatBackdrop();
        GUI.Label(new Rect(180,30,1320,48), combatRequest.title+" · "+combatWave+" / "+combatRequest.waves+"전투",headingStyle);
        GUI.Label(new Rect(1560,30,290,48),"ROUND "+combat.Round,headingStyle);
#if UNITY_EDITOR
        if (combatTestMode && GUI.Button(new Rect(1610, 92, 238, 55), "테스트 종료", buttonStyle))
        { ExitCombatTest(); return; }
#endif
        GUI.Label(new Rect(184,91,1120,40),combat.Active==null ? "전투 종료" : combat.Active.Name+"의 턴 · 속도 "+combat.Active.Speed+(combat.Active.Ally?" · 이동 자유 / 스킬·아이템 사용 시 턴 종료":" · 적 행동 중"),labelStyle);
        bool canInput=combatInspected==null && combat.Result==Outcome.Fighting;
        GUI.enabled=canInput;
        DrawCombatQueue();
        GUI.enabled=combat.Result==Outcome.Fighting;
        for(int p=0;p<4;p++) { DrawCombatUnit(true,p); DrawCombatUnit(false,p); }
        GUI.enabled=canInput;
        DrawCombatRange();
        DrawCombatCommandMeter();
        GUI.Label(new Rect(190,707,1630,53),combatMessage,labelStyle);
        DrawCombatControls();
        GUI.enabled=true;
        if(combatInspected!=null) DrawCombatInformation();
        else if(combat.Result!=Outcome.Fighting) DrawCombatResult();
    }

    private void DrawCombatBackdrop()
    {
        PixelRect(new Rect(0,0,1920,1080),new Color(.055f,.10f,.12f));
        for(int layer=0;layer<3;layer++)
        {
            Color c=layer==0 ? new Color(.10f,.19f,.22f) : layer==1 ? new Color(.08f,.15f,.17f) : new Color(.06f,.12f,.13f);
            for(int i=0;i<14;i++)
            {
                float x=i*165+layer*49, y=150+((i*53+layer*29)%140);
                PixelRect(new Rect(x+36,y,26,480-y),c);
                PixelRect(new Rect(x,y+10,106,90),c);
                PixelRect(new Rect(x-20,y+80,146,75),c);
            }
        }
        PixelRect(new Rect(0,570,1920,220),new Color(.15f,.18f,.17f));
        for(int i=0;i<38;i++) PixelRect(new Rect(i*57,610+(i*31)%140,25+(i%3)*8,5),new Color(.22f,.26f,.23f));
        PixelRect(new Rect(0,778,1920,302),new Color(.025f,.035f,.06f));
    }

    private void DrawCombatQueue()
    {
        GUI.Box(new Rect(24,96,124,656),"",panelStyle);
        GUI.Label(new Rect(30,102,110,35),"행동 순서",combatSmallStyle);
        for(int i=0;i<combat.Queue.Count;i++)
        {
            Unit u=combat.Queue[i]; float y=combatQueueY.ContainsKey(u.Id)?combatQueueY[u.Id]:142+i*74;
            Rect r=new Rect(39,y,94,66);
            GUI.Box(r,"",i==0?selectedTabStyle:cardStyle);
            if(u.Ally)
            {
                if (combatTestMode) DrawCombatTestHeroIcon(new Rect(r.x+3,r.y+1,53,54),u);
                else DrawGuestPortrait(new Rect(r.x+3,r.y+1,53,54),u.Id);
            }
            else DrawCombatEnemyIcon(new Rect(r.x+10,r.y+5,45,40),u.Job);
            GUI.Label(new Rect(r.x+52,r.y+8,40,30),u.Speed.ToString(),combatSmallStyle);
            GUI.Label(new Rect(r.x+1,r.y+43,92,22),u.Name,combatSmallStyle);
            if(GUI.Button(r,GUIContent.none,hoverStyle)) InspectCombatUnit(u);
        }
    }

    private void DrawCombatUnit(bool ally,int position)
    {
        Rect r=CombatUnitRect(ally,position);
        Unit u=combat.At(ally,position);
        GUI.Label(new Rect(r.x,r.y-52,r.width,35),(ally?"아군 ":"적군 ")+(position+1),combatSmallStyle);
        if(u==null)
        {
            PixelRect(new Rect(r.x+15,r.yMax-12,r.width-30,3),new Color(.3f,.34f,.32f));
            GUI.Label(new Rect(r.x,r.y+130,r.width,60),"빈 자리",combatSmallStyle); return;
        }
        bool active=u==combat.Active;
        GUI.Box(r,"",active?selectedTabStyle:cardStyle);
        if(u==combatTarget || (combatSelectedSkill!=null && combatSelectedSkill.Area && combat.IsAffectedBy(combatSelectedSkill,u)))
            CombatBorder(r,new Color(1f,.77f,.3f),4);
        GUI.Label(new Rect(r.x+4,r.y+10,r.width-8,48),u.Name,combatSmallStyle);
        GUI.Label(new Rect(r.x+6,r.y+55,r.width-12,28),"Lv."+u.Level+" · 속도 "+u.Speed,combatSmallStyle);
        if(ally)
        {
            if (combatTestMode) DrawCombatTestHeroIcon(new Rect(r.x+3,r.y+83,r.width-6,190),u);
            else DrawGuestPortrait(new Rect(r.x+3,r.y+83,r.width-6,190),u.Id);
        }
        else DrawCombatEnemyIcon(new Rect(r.x+20,r.y+110,r.width-40,140),u.Job);
        if(u.Guarding || u.Focused || u.PoisonTurns>0 || u.SanctuaryTurns>0)
            GUI.Label(new Rect(r.x+3,r.y+260,r.width-6,26),u.PoisonTurns>0?"독 "+u.PoisonStacks:u.SanctuaryTurns>0?"성역":u.Guarding?"방어 중":"은신 중",combatSmallStyle);
        CombatBar(new Rect(r.x+10,r.y+292,r.width-20,25),u.Hp,u.MaxHp,new Color(.68f,.22f,.25f),"HP");
        if(ally) CombatBar(new Rect(r.x+10,r.y+329,r.width-20,25),u.Mana,u.MaxMana,new Color(.22f,.43f,.8f),"MP");
        if(GUI.Button(r,GUIContent.none,hoverStyle)) InspectCombatUnit(u);
    }

    private void DrawCombatEnemyIcon(Rect r,string kind)
    {
        if(kind=="숲 슬라임")
        {
            Color body=new Color(.29f,.70f,.55f), highlight=new Color(.53f,.87f,.68f);
            PixelRect(new Rect(r.x+r.width*.18f,r.y+r.height*.40f,r.width*.64f,r.height*.52f),body);
            PixelRect(new Rect(r.x+r.width*.30f,r.y+r.height*.22f,r.width*.40f,r.height*.26f),body);
            PixelRect(new Rect(r.x+r.width*.38f,r.y+r.height*.14f,r.width*.23f,r.height*.18f),highlight);
            PixelRect(new Rect(r.x+r.width*.30f,r.y+r.height*.55f,r.width*.12f,r.height*.10f),new Color(.10f,.25f,.22f));
            PixelRect(new Rect(r.x+r.width*.60f,r.y+r.height*.55f,r.width*.12f,r.height*.10f),new Color(.10f,.25f,.22f));
            return;
        }
        Color fur=new Color(.43f,.53f,.51f), dark=new Color(.18f,.25f,.26f);
        PixelRect(new Rect(r.x+r.width*.1f,r.y+r.height*.2f,r.width*.8f,r.height*.7f),fur);
        PixelRect(new Rect(r.x,r.y,r.width*.3f,r.height*.45f),dark);
        PixelRect(new Rect(r.x+r.width*.7f,r.y,r.width*.3f,r.height*.45f),dark);
        PixelRect(new Rect(r.x+r.width*.13f,r.y+r.height*.47f,r.width*.24f,r.height*.12f),new Color(.94f,.64f,.26f));
        PixelRect(new Rect(r.x+r.width*.63f,r.y+r.height*.47f,r.width*.24f,r.height*.12f),new Color(.94f,.64f,.26f));
        PixelRect(new Rect(r.x+r.width*.3f,r.y+r.height*.7f,r.width*.4f,r.height*.3f),dark);
    }
    private void CombatBar(Rect r,int value,int maximum,Color color,string label)
    {
        PixelRect(r,new Color(.025f,.03f,.055f));
        PixelRect(new Rect(r.x,r.y,r.width*value/Mathf.Max(1,maximum),r.height),color);
        GUI.Label(r,label+" "+value+" / "+maximum,combatSmallStyle);
    }
    private void CombatBorder(Rect r,Color color,float width)
    {
        PixelRect(new Rect(r.x,r.y,r.width,width),color); PixelRect(new Rect(r.x,r.yMax-width,r.width,width),color);
        PixelRect(new Rect(r.x,r.y,width,r.height),color); PixelRect(new Rect(r.xMax-width,r.y,width,r.height),color);
    }

    private void DrawCombatRange()
    {
        Unit actor=combat.Active;
        if(actor==null || !actor.Ally || (combatSelectedSkill==null && combatItemIndex<0)) return;
        float from=CombatUnitRect(true,actor.Position).center.x;
        Color gold=new Color(.96f,.72f,.3f);
        bool drawn=false;
        foreach(var target in combat.Units)
        {
            bool valid=combatSelectedSkill!=null ? combat.IsAffectedBy(combatSelectedSkill,target) : target==actor;
            if(!valid) continue;
            if((combatSelectedSkill==null || !combatSelectedSkill.Area) && target!=combatTarget) continue;
            drawn=true;
            Rect rect=CombatUnitRect(target.Ally,target.Position); float to=rect.center.x;
            PixelRect(new Rect(rect.x+12,622,rect.width-24,5),gold);
            PixelRect(new Rect(to-2,625,4,13),gold);
            PixelRect(new Rect(Mathf.Min(from,to),635,Mathf.Max(4,Mathf.Abs(to-from)),4),gold);
        }
        if(drawn) PixelRect(new Rect(from-2,620,4,19),gold);
    }

    private void DrawCombatCommandMeter()
    {
        GUI.Label(new Rect(190,650,172,38),"길드장 지휘",combatSmallStyle);
        const float startX=380f, width=130f;
        for(int i=0;i<Session.MaxCommandPoints;i++)
        {
            Rect slot=new Rect(startX+i*width,651,width-5,34);
            PixelRect(slot,new Color(.48f,.35f,.20f));
            PixelRect(new Rect(slot.x+3,slot.y+3,slot.width-6,slot.height-6),
                i<combat.CommandPoints ? new Color(.96f,.70f,.30f) : new Color(.10f,.13f,.16f));
        }
        GUI.Label(new Rect(1690,650,120,38),combat.CommandPoints+" / "+Session.MaxCommandPoints,combatSmallStyle);
    }

    private void DrawCombatControls()
    {
        bool underlying=GUI.enabled;
        GUI.Box(new Rect(24,790,1872,264),"",panelStyle);
        if(GUI.Button(new Rect(42,809,192,66),"스킬",combatTab==0?selectedTabStyle:tabStyle)) { combatTab=0; ResetCombatSelection(); }
        if(GUI.Button(new Rect(42,886,192,66),"아이템",combatTab==1?selectedTabStyle:tabStyle)) { combatTab=1; ResetCombatSelection(); }
        if(GUI.Button(new Rect(42,963,192,66),"길드장 지휘",combatTab==2?selectedTabStyle:tabStyle)) { combatTab=2; ResetCombatSelection(); }
        Unit actor=combat.Active;
        bool player=underlying && actor!=null && actor.Ally;
        if(combatTab==0)
        {
            GUI.Label(new Rect(260,815,276,48),"좌우로 이동",headingStyle);
            GUI.enabled=player && actor.Position<3;
            if(GUI.Button(new Rect(260,888,124,82),"←",buttonStyle)) { combat.Move(1); ResetCombatSelection(); combatMessage=combat.LastMessage; }
            GUI.enabled=player && actor.Position>0;
            if(GUI.Button(new Rect(400,888,124,82),"→",buttonStyle)) { combat.Move(-1); ResetCombatSelection(); combatMessage=combat.LastMessage; }
            GUI.enabled=player;
            if(GUI.Button(new Rect(260,987,264,52),"턴 넘기기",buttonStyle))
            { if(combat.PassTurn()) CombatActionFinished(); GUI.enabled=underlying; return; }
            GUI.enabled=underlying;
            for(int i=0;i<5;i++)
            {
                Rect r=new Rect(550+i*264,821,252,194);
                Skill skill=actor!=null && actor.Ally && i<actor.Skills.Count ? actor.Skills[i] : null;
                GUI.Box(r,"",skill!=null && skill==combatSelectedSkill?selectedTabStyle:cardStyle);
                if(skill==null) { GUI.Label(r,"미습득",combatSmallStyle); continue; }
                GUI.Label(new Rect(r.x+15,r.y+12,r.width-30,58),skill.Name,headingStyle);
                GUI.Label(new Rect(r.x+15,r.y+74,r.width-30,40),"MP "+skill.Mana+(skill.Implemented?"":" · 확인 필요"),combatSmallStyle);
                GUI.Label(new Rect(r.x+15,r.y+116,r.width-30,65),skill==combatSelectedSkill ? "한 번 더 눌러 사용" : "클릭하여 범위 확인",combatSmallStyle);
                GUI.enabled=player;
                if(GUI.Button(r,GUIContent.none,hoverStyle)) { SelectCombatSkill(skill); GUI.enabled=underlying; return; }
                GUI.enabled=underlying;
                if(r.Contains(Event.current.mousePosition)) GUI.Label(new Rect(560,1016,1288,30),skill.Description,combatSmallStyle);
            }
        }
        else if(combatTab==1)
        {
            combatItemScroll=GUI.BeginScrollView(new Rect(556,817,1320,220),combatItemScroll,new Rect(0,0,1288,Mathf.Max(204,inventory[0].Length*92)),false,true);
            int row=0;
            for(int i=0;i<inventory[0].Length;i++)
            {
                ShopItem item=inventory[0][i];
                if(item.hpRecovery<=0 || item.stock<=0) continue;
                Rect r=new Rect(0,row++*92,1278,82);
                GUI.Box(r,"",combatItemIndex==i?selectedTabStyle:cardStyle);
                DrawPixelItemIcon(new Rect(10,r.y+7,72,68),item.icon);
                GUI.Label(new Rect(102,r.y+7,890,64),item.name+" · 길드 재고 "+item.stock+"개 · 현재 용사 체력 "+item.hpRecovery+" 회복",labelStyle);
                GUI.enabled=player;
                if(GUI.Button(new Rect(1010,r.y+10,250,60),combatItemIndex==i?"사용 · 턴 종료":"선택",buttonStyle)) { SelectCombatItem(i); break; }
                GUI.enabled=underlying;
            }
            GUI.enabled=underlying;
            if(row==0) GUI.Label(new Rect(20,30,1200,90),"길드의 회복 음식 재고가 없습니다. 길드 작업장에서 제작할 수 있습니다.",labelStyle);
            GUI.EndScrollView();
        }
        else
        {
            Unit current=combat.Active;
            bool encourageReady=player && combat.CommandPoints>=4 && !combat.HasActiveCommand;
            bool assistAvailable=false;
            foreach(Unit candidate in combat.Queue)
                if(candidate!=current && candidate.Ally && candidate.Alive && candidate.StunTurns==0) assistAvailable=true;
            bool chainReady=player && combat.CommandPoints>=8 && !combat.HasActiveCommand && assistAvailable;
            GUI.Box(new Rect(562,821,610,194),"",cardStyle);
            GUI.Label(new Rect(582,835,560,48),"전투 독려 · 지휘 4",headingStyle);
            GUI.Label(new Rect(582,886,560,54),current==null?"현재 행동 용사 없음":current.Name+"의 다음 공격 피해 +50%",labelStyle);
            GUI.enabled=encourageReady;
            if(GUI.Button(new Rect(582,949,560,52),"사용 · 턴 소모 없음",buttonStyle))
            { if(combat.UseEncouragement()) combatMessage=combat.LastMessage; }
            GUI.enabled=underlying;

            GUI.Box(new Rect(1190,821,610,194),"",cardStyle);
            GUI.Label(new Rect(1210,835,570,48),"연계 공격 · 지휘 8",headingStyle);
            GUI.Label(new Rect(1210,886,560,54),assistAvailable?
                "다음 공격 시 다음 순서 아군이 같은 적 추가 공격":"남은 순서에 행동 가능한 아군이 필요합니다.",combatSmallStyle);
            GUI.enabled=chainReady;
            if(GUI.Button(new Rect(1210,949,560,52),combat.ChainOrderActive?"연계 활성화됨":"사용 · 턴 소모 없음",buttonStyle))
            { if(combat.UseChainOrder()) combatMessage=combat.LastMessage; }
            GUI.enabled=underlying;
            GUI.Label(new Rect(562,1017,1240,28),combat.HasActiveCommand?
                "지휘 효과가 적용 중입니다. 효과가 끝나면 다른 지휘 스킬을 사용할 수 있습니다.":
                "적을 공격하거나 아군이 피격될 때마다 지휘 포인트 +1 · 최대 10",combatSmallStyle);
        }
        GUI.enabled=underlying;
    }

    private void DrawCombatInformation()
    {
        Unit u=combatInspected;
        PixelRect(new Rect(0,0,1920,1080),new Color(0,0,0,.75f));
        GUI.Box(new Rect(490,135,940,810),"",panelStyle);
        GUI.Label(new Rect(540,160,770,66),u.Name+" · Lv."+u.Level,headingStyle);
        if(GUI.Button(new Rect(1335,152,64,56),"×",buttonStyle)) { combatInspected=null; return; }
        GUI.Label(new Rect(540,238,800,58),u.Job+" · "+(u.Ally?"아군 ":"적군 ")+(u.Position+1)+"번 위치",labelStyle);
        GUI.Label(new Rect(540,307,800,58),"체력 "+u.Hp+" / "+u.MaxHp+(u.Ally?" · 마나 "+u.Mana+" / "+u.MaxMana:""),headingStyle);
        GUI.Label(new Rect(540,376,800,58),"공격력 "+u.Attack+" · 방어력 "+u.Defense+" · 속도 "+u.Speed,labelStyle);
        string effects=(u.GuardPercent>0?"피해 감소 "+u.GuardPercent+"%  ":"")+
            (u.StealthTurns>0?"은신  ":"")+(u.PoisonTurns>0?"독 "+u.PoisonStacks+"중첩  ":"")+
            (u.AttackDownTurns>0?"공격 감소  ":"")+(u.DefenseDownTurns>0?"방어 감소  ":"")+
            (u.StunTurns>0?"기절  ":"")+(u.SanctuaryTurns>0?"성역  ":"")+
            (u.DodgeTurns>0?"회피 상승  ":"")+(u.MarkTurns>0?"표식  ":"")+
            (u.DamageTakenTurns>0?"받는 피해 증가  ":"")+
            (u.SlowUntilRound>=combat.Round?"둔화  ":"")+
            (u.SpeedBoostUntilRound>=combat.Round?"속도 상승  ":"");
        GUI.Label(new Rect(540,445,800,58),effects.Length>0?effects:"적용 중인 효과 없음",labelStyle);
        if (combatTestMode && u.Ally)
            GUI.Label(new Rect(540,530,800,138),"개발용 가상 용사 · 장비 없음\n전직별 스킬과 기본 능력치를 시험합니다.",labelStyle);
        else if(u.Ally && u.Id>=0 && u.Id<guests.Count)
        {
            Guest hero=guests[u.Id];
            DrawCombatEquipment(new Rect(540,515,800,106),"착용 무기",hero.equippedWeapon,true);
            DrawCombatEquipment(new Rect(540,635,800,106),"착용 방어구",hero.equippedArmor,false);
        }
        else if(!u.Ally)
        {
            GUI.Label(new Rect(540,515,800,48),"적 기술 · 첫 전투 후 전체 정보 기록",headingStyle);
            if(u.Skills.Count==0)
                GUI.Label(new Rect(540,580,800,70),"이 적의 기술과 사거리 데이터는 아직 준비되지 않았습니다.",labelStyle);
            for(int i=0;i<u.Skills.Count && i<3;i++)
            {
                Skill skill=u.Skills[i];
                Rect card=new Rect(540,570+i*88,800,80);
                GUI.Box(card,"",cardStyle);
                if(HasEncounteredEnemyKind(u.Job) || HasObservedEnemySkill(skill))
                {
                    GUI.Label(new Rect(card.x+16,card.y+5,768,34),
                        skill.Name+" · 사거리 "+BasicSkills.Positions(skill.Ranges),headingStyle);
                    GUI.Label(new Rect(card.x+16,card.y+40,768,34),skill.Description,labelStyle);
                }
                else GUI.Label(new Rect(card.x+16,card.y+16,768,45),"??? · 아직 목격하지 않은 기술",labelStyle);
            }
        }
        if(combatSelectedSkill!=null && combatSelectedSkill.Effect==SkillEffect.Heal &&
            combat.Active!=null && combat.Active.Ally && combat.ValidTarget(combatSelectedSkill,u))
        {
            if(GUI.Button(new Rect(815,766,290,50),"회복 대상으로 지정",buttonStyle))
            { combatTarget=u; combatInspected=null; combatMessage=u.Name+"을(를) 회복 대상으로 지정했습니다."; }
        }
        else GUI.Label(new Rect(540,760,800,48),u==combatTarget && combatSelectedSkill!=null?"현재 스킬 대상입니다.":"",labelStyle);
        if(GUI.Button(new Rect(812,842,296,60),"닫기",buttonStyle)) combatInspected=null;
    }

    private void DrawCombatEquipment(Rect rect,string label,ShopItem item,bool weapon)
    {
        GUI.Box(rect,"",cardStyle);
        if(item!=null) DrawPixelItemIcon(new Rect(rect.x+12,rect.y+9,88,88),item.icon);
        else GUI.Box(new Rect(rect.x+12,rect.y+9,88,88),"—",cardStyle);
        GUI.Label(new Rect(rect.x+122,rect.y+9,650,42),label+" · "+(item==null?"없음":item.name),headingStyle);
        GUI.Label(new Rect(rect.x+122,rect.y+55,650,38),item==null?"아직 구입한 장비가 없습니다.":weapon?"공격력 +"+item.attackBonus:"방어력 +"+item.defenseBonus,labelStyle);
    }

    private void DrawCombatResult()
    {
        bool won=combat.Result==Outcome.Victory;
        bool more=won && combatWave<combatRequest.waves;
        PixelRect(new Rect(0,0,1920,1080),new Color(0,0,0,.76f));
        GUI.Box(new Rect(460,280,1000,530),"",panelStyle);
        GUI.Label(new Rect(520,318,880,72),won?"승리 · 적 전원 처치":"패배 · 아군 전원 사망",headingStyle);
        string summary=combatTestMode ? "개발용 전투입니다. 길드 골드·경험치·의뢰·날짜는 변경되지 않습니다."
            : more ? "다음 전투가 남아 있습니다. 생존한 용사의 체력·마나는 그대로 이어집니다."
            : won ? "길드 금고 +"+combatRequest.reward+" G\n길드 EXP +"+combatRequest.guildExperience+" · 참여 용사 EXP +"+combatRequest.heroExperience+" (길드원 보너스 적용)"
            : "보상을 받지 못했습니다. 다음 날 이후 의뢰를 다시 준비할 수 있습니다.";
        GUI.Label(new Rect(520,420,880,156),summary,labelStyle);
        if(GUI.Button(new Rect(700,662,520,82),combatTestMode?"테스트 종료 · 길드로":more?"다음 전투":"결과 확인 · 다음 날 낮으로",buttonStyle)) FinishCombat();
    }
}
