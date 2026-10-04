using System;
using System.Collections.Generic;

namespace GuildCombat
{
    public enum Outcome { Fighting, Victory, Defeat }
    public enum SkillEffect { Damage, Guard, Focus, RecoverFocus, Heal, Support }

    public sealed class PoisonStack
    {
        public int Attack, Remaining;
        public PoisonStack(int attack) { Attack=attack; Remaining=3; }
    }

    public sealed class Skill
    {
        public string Id, Name, Description;
        public int Mana, Power, Ranges, UserPositions = 15, Hits = 1;
        public SkillEffect Effect;
        public bool Area, AllEnemies, AllAllies, Implemented = true;
        public bool Self => Effect == SkillEffect.Guard || Effect == SkillEffect.Focus || Effect == SkillEffect.RecoverFocus || Effect == SkillEffect.Support;
        public bool CanUseAt(int position) => position >= 0 && position < 4;
        public bool CanReach(int userPosition, int targetPosition) => AllEnemies || (Ranges & (1 << (userPosition + targetPosition))) != 0;
    }

    public sealed class Unit
    {
        public int Id, Position, Level, MaxHp, Hp, MaxMana, Mana, Attack, Defense, Speed, CritChance=10;
        public string Name, Job;
        public bool Ally, Guarding, Focused;
        public int NextAttackBonus, GuardPercent, GuardHits, GuardTurns, SanctuaryTurns, SanctuaryAuraTurns, SanctuaryAuraAttack, DodgeTurns, CounterTurns, CounterPower;
        public int StealthTurns, MarkTurns, MarkedBy, PoisonStacks, PoisonTurns, PoisonAttack;
        public int AttackDownPercent, AttackDownTurns, DefenseDownPercent, DefenseDownTurns;
        public int DamageTakenBonus, DamageTakenTurns, SlowPercent, SlowTurns, SlowUntilRound, SpeedBoostPercent, SpeedBoostTurns, SpeedBoostUntilRound, StunTurns;
        public readonly List<Skill> Skills = new List<Skill>();
        public readonly List<PoisonStack> Poison = new List<PoisonStack>();
        public bool Alive => Hp > 0;
    }

    public static class BasicSkills
    {
        private static Skill Make(string id, int mana, int power, SkillEffect effect, string reach, string description)
        {
            var s = new Skill { Id=id, Name=id.Substring(id.LastIndexOf(':')+1), Mana=mana, Power=power, Effect=effect, Description=description };
            if (!string.IsNullOrEmpty(reach))
                foreach (string part in reach.Split(',')) s.Ranges |= 1 << (int.Parse(part)-1);
            return s;
        }
        public static Skill Resolve(string id)
        {
            string name = id.Substring(id.LastIndexOf(':')+1);
            Skill s;
            switch (name)
            {
                case "베기": s=Make(id,0,100,SkillEffect.Damage,"1","거리 1 · 공격력 100% 피해"); break;
                case "방어 태세": s=Make(id,0,0,SkillEffect.Guard,"","다음 자기 턴까지 받는 피해 50% 감소"); break;
                case "급소 노리기": s=Make(id,0,115,SkillEffect.Damage,"2","거리 2 · 공격력 115% 피해"); break;
                case "은신": s=Make(id,2,0,SkillEffect.Focus,"","2턴간 은신 · 다음 공격 피해 +20%"); break;
                case "정밀 사격": s=Make(id,0,110,SkillEffect.Damage,"3","거리 3 · 공격력 110% 피해"); break;
                case "응급처치": s=Make(id,0,40,SkillEffect.Heal,"","앞쪽 아군 · 공격력 40% 체력 회복"); break;
                case "마력탄": s=Make(id,2,130,SkillEffect.Damage,"2","거리 2 · 공격력 130% 피해"); break;
                case "마력 집중": s=Make(id,0,0,SkillEffect.RecoverFocus,"","자신과 앞쪽 아군 마나 회복 · 맨 앞이면 자신 40%"); break;
                case "방패 강타": s=Make(id,2,110,SkillEffect.Damage,"1","거리 1 · 110% 피해 · 기절 70%"); break;
                case "철벽 진형": s=Make(id,5,0,SkillEffect.Support,"","아군 전체 다음 피격 피해 30% 감소"); s.AllAllies=true; break;
                case "왕실 방패술": s=Make(id,5,120,SkillEffect.Damage,"1","거리 1 · 120% 피해 · 자신 피해 40% 감소"); break;
                case "광전사의 일격": s=Make(id,2,160,SkillEffect.Damage,"1","거리 1 · 160% 피해 · 자신의 최대 체력 5% 소모"); break;
                case "분노 폭발": s=Make(id,5,110,SkillEffect.Damage,"1,2","거리 1·2 광역 · 체력 절반 이하면 피해 +30%"); break;
                case "살육 본능": s=Make(id,6,200,SkillEffect.Damage,"1","거리 1 · 처치하면 최대 체력·마나 15% 회복"); break;
                case "마력 베기": s=Make(id,3,130,SkillEffect.Damage,"2","거리 2 · 방어력 30% 무시"); break;
                case "번개 돌진": s=Make(id,5,150,SkillEffect.Damage,"2","거리 2 · 속도 30% 감소, 2턴"); break;
                case "차원 참격": s=Make(id,7,180,SkillEffect.Damage,"3","거리 3 · 방어력 50% 무시"); break;
                case "약점 간파": s=Make(id,3,70,SkillEffect.Damage,"3","거리 3 · 방어력 25% 감소, 2턴"); break;
                case "연막탄": s=Make(id,5,0,SkillEffect.Support,"","아군 전체 회피율 +40%, 1턴"); s.AllAllies=true; break;
                case "최루탄": s=Make(id,5,40,SkillEffect.Damage,"2,3,4","거리 2·3·4 광역 · 40% 피해 · 적 공격력 40% 감소, 2턴"); break;
                case "그림자 찌르기": s=Make(id,3,140,SkillEffect.Damage,"2","거리 2 · 은신 시 피해 +40% · 한 칸 전진"); break;
                case "연속 암습": s=Make(id,4,75,SkillEffect.Damage,"2","거리 2 · 75% 피해 두 번"); s.Hits=2; break;
                case "죽음의 표식": s=Make(id,5,80,SkillEffect.Damage,"3","거리 3 · 다음 자신의 공격 피해 +50%"); break;
                case "맹독 바르기": s=Make(id,2,80,SkillEffect.Damage,"2","거리 2 · 독 1중첩, 3턴"); break;
                case "신경독": s=Make(id,4,80,SkillEffect.Damage,"4","거리 4 · 독 1중첩 · 속도 30% 감소"); break;
                case "맹독의 늪": s=Make(id,7,50,SkillEffect.Damage,"2,3,4","거리 2·3·4 광역 · 독 2중첩 · 공격력 30% 감소"); break;
                case "관통 화살": s=Make(id,3,130,SkillEffect.Damage,"3","거리 3 · 방어력 35% 무시"); break;
                case "약점 사격": s=Make(id,4,150,SkillEffect.Damage,"4","거리 4 · 방어력 감소 대상에게 피해 +30%"); break;
                case "필중의 일격": s=Make(id,6,160,SkillEffect.Damage,"5","거리 5 · 확정 치명타 1.5배"); break;
                case "속사": s=Make(id,3,65,SkillEffect.Damage,"3","거리 3 · 65% 피해 두 번"); s.Hits=2; break;
                case "화살비": s=Make(id,4,85,SkillEffect.Damage,"4,5","거리 4·5 광역 · 85% 피해"); break;
                case "천 개의 화살": s=Make(id,7,30,SkillEffect.Damage,"3,4,5","거리 3·4·5 광역 · 30% 피해 세 번"); s.Hits=3; break;
                case "마력 화살": s=Make(id,3,100,SkillEffect.Damage,"3,4","거리 3·4 · 적 두 명에게 100% 피해"); break;
                case "치유의 화살": s=Make(id,5,120,SkillEffect.Heal,"","앞쪽 아군 · 공격력 120% 체력 회복"); break;
                case "마력 폭우": s=Make(id,7,100,SkillEffect.Damage,"5,6","거리 5·6 광역 · 아군 최대 마나 10% 회복"); break;
                case "화염 작렬": s=Make(id,4,120,SkillEffect.Damage,"3","거리 3 · 양옆 위치에 절반 피해"); break;
                case "폭풍": s=Make(id,6,70,SkillEffect.Damage,"1,2,3,4","거리 1~4 광역 · 아군 속도 증가"); break;
                case "해일": s=Make(id,7,60,SkillEffect.Damage,"","적 전체 공격 · 기절 60%"); s.AllEnemies=true; break;
                case "치유의 빛": s=Make(id,3,150,SkillEffect.Heal,"","앞쪽 아군 또는 자신 · 공격력 150% 회복"); break;
                case "회복의 파동": s=Make(id,7,90,SkillEffect.Heal,"","아군 전체 공격력 90% 회복"); s.AllAllies=true; break;
                case "성역": s=Make(id,7,0,SkillEffect.Support,"","아군 전체 3턴간 받는 피해 20% 감소"); s.AllAllies=true; break;
                case "암흑 물질": s=Make(id,3,150,SkillEffect.Damage,"3","거리 3 · 자신에게도 피해"); break;
                case "영혼 흡수": s=Make(id,5,150,SkillEffect.Damage,"3","거리 3 · 실제 피해의 40% 체력 회복"); break;
                case "저주의 폭풍": s=Make(id,8,0,SkillEffect.Damage,"4,5","거리 4·5 광역 · 적과 자신 받는 피해 +100%, 1턴"); break;
                default: return new Skill { Id=id, Name=name, Implemented=false, Description="현재 스킬 표에 없는 기술입니다." };
            }
            s.Area=s.AllEnemies || s.AllAllies || s.Effect==SkillEffect.RecoverFocus ||
                "분노 폭발,최루탄,맹독의 늪,화살비,천 개의 화살,마력 폭우,폭풍,저주의 폭풍,마력 화살".Contains(name);
            return s;
        }
        public static string Positions(int mask)
        { var list=new List<string>(); for(int i=0;i<7;i++) if((mask&(1<<i))!=0) list.Add((i+1).ToString()); return string.Join("·",list.ToArray()); }
    }

    public static class EnemySkills
    {
        private static int Distances(int first, int last)
        {
            int mask=0;
            for(int distance=first;distance<=last;distance++) mask|=1<<(distance-1);
            return mask;
        }

        private static void AddStrike(Unit enemy, string name, string description, int first, int last, int power)
        {
            enemy.Skills.Add(new Skill { Id=enemy.Job+":"+name, Name=name, Description=description,
                Effect=SkillEffect.Damage, Ranges=Distances(first,last), Power=power });
        }

        // The first enemy prototype has a dangerous short-range attack and a weaker
        // fallback, so leaving the front slot empty cannot stall the battle.
        public static void AddFor(Unit enemy)
        {
            if (enemy.Job == "숲 슬라임")
            {
                enemy.Skills.Add(new Skill { Id="숲 슬라임:점액 타격", Name="점액 타격",
                    Description="앞쪽 용사에게 점액을 던집니다.", Effect=SkillEffect.Damage,
                    Ranges=1 << 0, Power=100 });
                int splashRanges=0;
                for (int distance=2;distance<=7;distance++) splashRanges|=1 << (distance-1);
                enemy.Skills.Add(new Skill { Id="숲 슬라임:점액 튀기기", Name="점액 튀기기",
                    Description="멀리 있는 용사에게 약한 점액 공격을 합니다.", Effect=SkillEffect.Damage,
                    Ranges=splashRanges, Power=70 });
                return;
            }
            switch(enemy.Job)
            {
                case "야생 늑대": AddStrike(enemy,"물어뜯기","거리 1 · 강한 피해",1,1,120); break;
                case "독두꺼비": AddStrike(enemy,"독침","거리 2 · 피해와 독",2,2,85); break;
                case "늪 괴물": AddStrike(enemy,"진흙 손아귀","거리 1 · 피해와 둔화",1,1,110); break;
                case "망령": AddStrike(enemy,"영혼 할퀴기","거리 2 · 유적의 망령이 공격",2,2,105); break;
                case "해골 병사": AddStrike(enemy,"녹슨 검","거리 1 · 공격",1,1,115); break;
                case "유적 수호자": AddStrike(enemy,"수호자의 일격","거리 1 · 강한 공격",1,1,135); break;
                case "하피": AddStrike(enemy,"급습","거리 3 · 후열 공격",3,3,110); break;
                case "암석 도마뱀": AddStrike(enemy,"암석 돌진","거리 1 · 강한 공격",1,1,125); break;
                case "마도 인형": AddStrike(enemy,"마력 광선","거리 2 · 마력 공격",2,2,120); break;
                case "폭주한 마력체": AddStrike(enemy,"마력 폭발","거리 3 · 강한 마력 공격",3,3,145); break;
                case "서리 정령": AddStrike(enemy,"서리 가시","거리 2 · 피해와 둔화",2,2,105); break;
                case "눈 골렘": AddStrike(enemy,"빙결 주먹","거리 1 · 강한 공격",1,1,140); break;
                case "마왕군 정찰병": AddStrike(enemy,"기습 베기","거리 2 · 전열 뒤 공격",2,2,125); break;
                case "마왕군 지휘관": AddStrike(enemy,"지휘관의 검","거리 1 · 강한 공격",1,1,155); break;
            }
            // A weaker fallback prevents a fixed-range enemy from losing every turn
            // when no hero occupies its preferred distance.
            AddStrike(enemy,"견제","거리 1~7 · 약한 공격",1,7,65);
        }
    }

    public sealed class Session
    {
        public readonly List<Unit> Units=new List<Unit>();
        private readonly List<Unit> queue=new List<Unit>();
        private readonly Random random=new Random();
        public IReadOnlyList<Unit> Queue=>queue;
        public Unit Active=>Result==Outcome.Fighting && queue.Count>0 ? queue[0] : null;
        public Outcome Result { get; private set; }
        public int Round { get; private set; }
        public int CommandPoints { get; private set; }
        public const int MaxCommandPoints=10;
        private bool chainOrderActive;
        public bool ChainOrderActive=>chainOrderActive;
        public bool HasActiveCommand=>chainOrderActive || Units.Exists(u=>u.Ally && u.Alive && u.NextAttackBonus>0);
        public string LastMessage { get; private set; }

        public Session(IEnumerable<Unit> heroes,IEnumerable<Unit> enemies)
        {
            Units.AddRange(heroes); Units.AddRange(enemies);
            var ids=new HashSet<int>(); var slots=new HashSet<int>(); int allies=0, foes=0;
            foreach(Unit u in Units)
            {
                if(u.Position<0 || u.Position>3 || !ids.Add(u.Id) || !slots.Add(u.Position+(u.Ally?0:4)) ||
                    u.MaxHp<=0 || u.Hp<=0 || u.Hp>u.MaxHp || u.Mana<0 || u.Mana>u.MaxMana)
                    throw new ArgumentException("Invalid combat formation or unit.");
                if(u.Ally) allies++; else { foes++; u.MaxMana=u.Mana=0; }
            }
            if(allies<1 || allies>4 || foes<1 || foes>4) throw new ArgumentException("Combat requires 1..4 units per side.");
            BeginRound();
        }
        public Unit At(bool ally,int position)=>Units.Find(u=>u.Ally==ally && u.Position==position && u.Alive);
        public Unit Find(int id)=>Units.Find(u=>u.Id==id);
        private void BeginRound()
        {
            Round++; queue.Clear(); queue.AddRange(Units.FindAll(u=>u.Alive));
            queue.Sort((a,b)=>EffectiveSpeed(a)!=EffectiveSpeed(b) ? EffectiveSpeed(b).CompareTo(EffectiveSpeed(a)) :
                a.Ally!=b.Ally ? (a.Ally?-1:1) : a.Id.CompareTo(b.Id));
            BeginTurn();
        }
        private int EffectiveSpeed(Unit u)=>Math.Max(1,u.Speed*(100-(Round<=u.SlowUntilRound?u.SlowPercent:0)+(Round<=u.SpeedBoostUntilRound?u.SpeedBoostPercent:0))/100);
        private void BeginTurn()
        {
            Unit a=Active;
            if(a!=null && a.StunTurns>0) { a.StunTurns--; LastMessage=a.Name+" · 기절로 행동 불가"; EndTurn(); }
        }
        public bool Move(int direction)
        {
            Unit a=Active; if(a==null || !a.Ally || (direction!=1 && direction!=-1)) return false;
            int p=a.Position+direction; if(p<0 || p>3) return false;
            Unit b=At(true,p); if(b!=null) b.Position=a.Position; a.Position=p;
            LastMessage=a.Name+" → 아군 "+(p+1)+"번 위치 (턴 유지)"; return true;
        }
        public bool PassTurn()
        { Unit a=Active; if(a==null || !a.Ally) return false; CancelChain(); LastMessage=a.Name+" · 턴 넘기기"; EndTurn(); return true; }
        public bool ValidTarget(Skill s,Unit t)
        {
            Unit a=Active; if(a==null || t==null || !t.Alive) return false;
            if(s.Effect==SkillEffect.Heal)
            {
                if(t.Ally!=a.Ally) return false;
                if(s.AllAllies) return true;
                Unit front=NearestFrontAlly(a);
                return t==front || (s.Name=="치유의 빛" && front==null && t==a);
            }
            if(s.Effect==SkillEffect.RecoverFocus)
                return t.Ally==a.Ally && (t==a || t.Position<a.Position);
            if(s.AllAllies && s.Effect==SkillEffect.Support)
                return t.Ally==a.Ally;
            if(s.Self) return t==a;
            if(t.Ally==a.Ally || !s.CanReach(a.Position,t.Position)) return false;
            if(t.StealthTurns>0 && !s.Area && Units.Exists(u=>u.Ally==t.Ally && u.Alive && u.StealthTurns==0)) return false;
            return true;
        }
        private Unit NearestFrontAlly(Unit actor)
        {
            Unit nearest=null;
            foreach(Unit unit in Units)
                if(unit.Ally==actor.Ally && unit.Alive && unit.Position<actor.Position &&
                    (nearest==null || unit.Position>nearest.Position)) nearest=unit;
            return nearest;
        }
        public bool IsAffectedBy(Skill skill,Unit unit)
        {
            if(ValidTarget(skill,unit)) return true;
            Unit actor=Active;
            return actor!=null && unit!=null && unit.Alive && unit.Ally==actor.Ally &&
                (skill.Name=="마력 폭우" || skill.Name=="폭풍");
        }
        public bool CanUse(Skill s)
        { Unit a=Active; return a!=null && a.Ally && a.Skills.Contains(s) && s.Implemented && a.Mana>=s.Mana; }
        public bool UseSkill(Skill s,int targetId)
        {
            Unit a=Active,t=Find(targetId); if(!CanUse(s) || !ValidTarget(s,t)) return false;
            a.Mana-=s.Mana;
            if(s.Effect==SkillEffect.Damage)
            {
                var targets=new List<Unit>();
                if(s.Area)
                {
                    if(s.Name=="마력 화살")
                    {
                        targets.Add(t);
                        Unit second=Units.Find(u=>u!=t && ValidTarget(s,u));
                        if(second!=null) targets.Add(second);
                    }
                    else foreach(Unit u in Units) if(ValidTarget(s,u)) targets.Add(u);
                }
                else targets.Add(t);
                Unit assistTarget=t;
                if(s.Area) { assistTarget=null; foreach(Unit u in targets) if(assistTarget==null || u.Position<assistTarget.Position) assistTarget=u; }
                int total=0;
                foreach(Unit enemy in targets)
                {
                    if(!enemy.Alive) continue;
                    int applied=0;
                    if(s.Power>0)
                        for(int hit=0;hit<s.Hits && enemy.Alive;hit++) applied+=Damage(a,enemy,s.Power,Bonus(a,s,enemy),IgnoreDefense(s),s.Name=="필중의 일격");
                    total+=applied;
                    if(applied>0 || s.Power==0) ApplyAttackEffect(a,enemy,s,applied);
                }
                if(s.Name=="광전사의 일격") a.Hp=Math.Max(1,a.Hp-Math.Max(1,a.MaxHp*5/100));
                if(s.Name=="그림자 찌르기" && a.Position>0) Move(-1);
                if(s.Name=="왕실 방패술") { a.GuardPercent=40; a.GuardTurns=2; a.CounterPower=80; a.CounterTurns=2; }
                if(s.Name=="마력 폭우")
                    foreach(Unit u in Units) if(u.Ally==a.Ally && u.Alive)
                        u.Mana=Math.Min(u.MaxMana,u.Mana+Math.Max(1,u.MaxMana/10));
                if(s.Name=="폭풍")
                    foreach(Unit u in Units) if(u.Ally==a.Ally && u.Alive)
                    { u.SpeedBoostPercent=20; u.SpeedBoostUntilRound=Math.Max(u.SpeedBoostUntilRound,Round+2); }
                if(s.Name=="저주의 폭풍") { a.DamageTakenBonus=100; a.DamageTakenTurns=2; }
                bool attackAction=s.Power>0;
                if(attackAction)
                { a.Focused=false; a.StealthTurns=0; a.NextAttackBonus=0; GainCommandPoint(); }
                else CancelChain();
                LastMessage=a.Name+" · "+s.Name+(attackAction?" → 총 "+total+" 피해":" → 상태 효과 적용");
                if(chainOrderActive && attackAction)
                {
                    chainOrderActive=false;
                    Unit assistant=queue.Find(u=>u!=a && u.Ally && u.Alive && u.StunTurns==0);
                    if(assistant!=null && assistTarget!=null && assistTarget.Alive)
                    {
                        int power=assistant.Job=="마법사"?130:assistant.Job=="궁수"?110:assistant.Job=="도적"?115:100;
                        int extra=Damage(assistant,assistTarget,power,0,0,false);
                        LastMessage+=" · 연계: "+assistant.Name+" +"+extra+" 피해";
                    }
                    else LastMessage+=" · 연계 공격 대상 없음";
                }
            }
            else
            {
                CancelChain();
                if(s.Effect==SkillEffect.Guard) { a.Guarding=true; a.GuardPercent=50; a.GuardTurns=2; }
                if(s.Effect==SkillEffect.Focus) { a.Focused=true; a.StealthTurns=2; }
                if(s.Effect==SkillEffect.Heal)
                    foreach(Unit u in Units) if(u.Ally==a.Ally && u.Alive && (s.AllAllies || u==t)) Heal(u,a.Attack*s.Power/100);
                if(s.Effect==SkillEffect.RecoverFocus)
                {
                    var recipients=Units.FindAll(u=>u.Ally==a.Ally && u.Alive && u.Position<=a.Position);
                    int pool=a.MaxMana*40/100;
                    int share=recipients.Count>0?Math.Max(1,pool/recipients.Count):0;
                    foreach(Unit u in recipients) u.Mana=Math.Min(u.MaxMana,u.Mana+share);
                }
                if(s.Name=="철벽 진형") foreach(Unit u in Units) if(u.Ally==a.Ally && u.Alive)
                { u.GuardPercent=Math.Max(u.GuardPercent,30); u.GuardHits=1; u.GuardTurns=2; }
                if(s.Name=="성역")
                {
                    foreach(Unit u in Units) if(u.Ally==a.Ally && u.Alive) u.SanctuaryTurns=u==a?4:3;
                    a.SanctuaryAuraAttack=a.Attack; a.SanctuaryAuraTurns=3;
                }
                if(s.Name=="연막탄") foreach(Unit u in Units) if(u.Ally==a.Ally && u.Alive) u.DodgeTurns=2;
                LastMessage=a.Name+" · "+s.Name;
            }
            EndTurn(); return true;
        }
        private static int IgnoreDefense(Skill s)
        { return s.Name=="마력 베기"?30:s.Name=="차원 참격"?50:s.Name=="관통 화살"?35:0; }
        private static int Bonus(Unit a,Skill s,Unit t)
        {
            int n=a.NextAttackBonus+(a.Focused?20:0);
            if(s.Name=="분노 폭발" && a.Hp*2<=a.MaxHp) n+=30;
            if(s.Name=="그림자 찌르기" && a.StealthTurns>0) n+=40;
            if(s.Name=="약점 사격" && t.DefenseDownTurns>0) n+=30;
            if(t.MarkTurns>0 && t.MarkedBy==a.Id) { n+=50; t.MarkTurns=0; }
            return n;
        }
        private void ApplyAttackEffect(Unit a,Unit t,Skill s,int applied)
        {
            if(s.Name=="방패 강타" && random.Next(100)<70) t.StunTurns=1;
            if(s.Name=="살육 본능" && !t.Alive) { Heal(a,a.MaxHp*15/100); a.Mana=Math.Min(a.MaxMana,a.Mana+a.MaxMana*15/100); }
            if(s.Name=="번개 돌진" || s.Name=="신경독")
            { t.SlowPercent=Math.Max(t.SlowPercent,30); t.SlowUntilRound=Math.Max(t.SlowUntilRound,Round+2); }
            if(s.Name=="약점 간파") { t.DefenseDownPercent=Math.Max(t.DefenseDownPercent,25); t.DefenseDownTurns=Math.Max(t.DefenseDownTurns,2); }
            if(s.Name=="최루탄" || s.Name=="맹독의 늪")
            { t.AttackDownPercent=Math.Max(t.AttackDownPercent,s.Name=="최루탄"?40:30); t.AttackDownTurns=Math.Max(t.AttackDownTurns,2); }
            if(s.Name=="맹독 바르기" || s.Name=="신경독" || s.Name=="맹독의 늪")
            {
                int stacks=s.Name=="맹독의 늪"?2:1;
                for(int i=0;i<stacks;i++)
                {
                    if(t.Poison.Count<3) t.Poison.Add(new PoisonStack(a.Attack));
                    else
                    {
                        int weakest=0;
                        for(int j=1;j<t.Poison.Count;j++)
                            if(t.Poison[j].Remaining<t.Poison[weakest].Remaining) weakest=j;
                        t.Poison[weakest]=new PoisonStack(a.Attack);
                    }
                }
                t.PoisonStacks=t.Poison.Count; t.PoisonTurns=3;
            }
            if(s.Name=="죽음의 표식") { t.MarkedBy=a.Id; t.MarkTurns=2; }
            if(s.Name=="화염 작렬")
                foreach(Unit u in Units) if(u!=t && u.Ally==t.Ally && u.Alive && Math.Abs(u.Position-t.Position)==1)
                    Damage(a,u,s.Power/2,0,0,false);
            if(s.Name=="해일" && random.Next(100)<60) t.StunTurns=1;
            if(s.Name=="암흑 물질") a.Hp=Math.Max(1,a.Hp-Math.Max(1,applied*20/100));
            if(s.Name=="영혼 흡수") Heal(a,applied*40/100);
            if(s.Name=="저주의 폭풍") { t.DamageTakenBonus=100; t.DamageTakenTurns=1; }
        }
        private static int Heal(Unit u,int amount)
        { int gained=Math.Min(Math.Max(0,amount),u.MaxHp-u.Hp); u.Hp+=gained; return gained; }
        public bool UseHealingItem(int healing)
        {
            Unit a=Active; if(a==null || !a.Ally || a.Hp>=a.MaxHp || healing<=0) return false;
            CancelChain(); int gained=Heal(a,healing); LastMessage=a.Name+" · 체력 +"+gained; EndTurn(); return true;
        }
        public bool EnemyAttack(int targetId)
        {
            Unit a=Active,t=Find(targetId); if(a==null || a.Ally || t==null || !t.Ally || !t.Alive) return false;
            if(t.StealthTurns>0 && Units.Exists(u=>u.Ally && u.Alive && u.StealthTurns==0)) return false;
            int damage=Damage(a,t,100,0,0,false); if(damage>0) GainCommandPoint();
            if(t.Alive && t.CounterTurns>0) { Damage(t,a,t.CounterPower,0,0,false); t.CounterTurns=0; }
            LastMessage=a.Name+" → "+t.Name+" -"+damage+" HP"; EndTurn(); return true;
        }
        public bool EnemyUseSkill(Skill skill,int targetId)
        {
            Unit a=Active,t=Find(targetId);
            if(a==null || a.Ally || skill==null || !a.Skills.Contains(skill) ||
                skill.Effect!=SkillEffect.Damage || t==null || !t.Ally || !t.Alive ||
                !skill.CanReach(a.Position,t.Position) ||
                (t.StealthTurns>0 && Units.Exists(u=>u.Ally && u.Alive && u.StealthTurns==0))) return false;
            int damage=Damage(a,t,skill.Power,0,0,false);
            if(damage>0) GainCommandPoint();
            if(damage>0 && t.Alive)
            {
                if(skill.Name=="독침" && t.Poison.Count<3)
                {
                    t.Poison.Add(new PoisonStack(a.Attack));
                    t.PoisonStacks=t.Poison.Count;
                    t.PoisonTurns=3;
                }
                if(skill.Name=="진흙 손아귀" || skill.Name=="서리 가시")
                {
                    t.SlowPercent=Math.Max(t.SlowPercent,30);
                    t.SlowUntilRound=Math.Max(t.SlowUntilRound,Round+2);
                }
            }
            if(t.Alive && t.CounterTurns>0) { Damage(t,a,t.CounterPower,0,0,false); t.CounterTurns=0; }
            LastMessage=a.Name+" · "+skill.Name+" → "+t.Name+" -"+damage+" HP";
            EndTurn(); return true;
        }
        private int Damage(Unit a,Unit t,int power,int bonus,int ignore,bool critical)
        {
            if(t.DodgeTurns>0 && random.Next(100)<40) return 0;
            int atk=a.Attack*(100-(a.AttackDownTurns>0?a.AttackDownPercent:0))/100;
            int raw=atk*power*(100+bonus)/10000;
            if(critical || random.Next(100)<a.CritChance) raw=raw*150/100;
            int defense=t.Defense*(100-(t.DefenseDownTurns>0?t.DefenseDownPercent:0))*(100-ignore)/10000;
            int damage=Math.Max(1,raw-defense);
            if(t.GuardPercent>0) damage=Math.Max(1,damage*(100-t.GuardPercent)/100);
            if(t.SanctuaryTurns>0) damage=Math.Max(1,damage*80/100);
            if(t.DamageTakenTurns>0) damage=Math.Max(1,damage*(100+t.DamageTakenBonus)/100);
            int applied=Math.Min(damage,t.Hp); t.Hp-=applied;
            if(t.GuardHits>0 && --t.GuardHits==0 && !t.Guarding) t.GuardPercent=0;
            return applied;
        }
        private void GainCommandPoint() { CommandPoints=Math.Min(MaxCommandPoints,CommandPoints+1); }
        private void CancelChain() { if(chainOrderActive) { chainOrderActive=false; CommandPoints=Math.Min(MaxCommandPoints,CommandPoints+8); } }
        public bool UseEncouragement()
        {
            Unit a=Active; if(a==null || !a.Ally || CommandPoints<4 || HasActiveCommand) return false;
            CommandPoints-=4; a.NextAttackBonus=50; LastMessage=a.Name+" 다음 공격 +50% · 지휘 -4"; return true;
        }
        public bool UseChainOrder()
        {
            if(Active==null || !Active.Ally || CommandPoints<8 || HasActiveCommand ||
                !queue.Exists(u=>u!=Active && u.Ally && u.Alive && u.StunTurns==0)) return false;
            CommandPoints-=8; chainOrderActive=true;
            LastMessage="연계 활성화 · 다음 공격에 다음 순서 아군이 추가 공격 · 지휘 -8"; return true;
        }
        private void EndTurn()
        {
            Unit actor=queue[0];
            if(actor.SanctuaryAuraTurns>0 && actor.Alive)
            {
                foreach(Unit foe in Units) if(foe.Ally!=actor.Ally && foe.Alive)
                    foe.Hp=Math.Max(0,foe.Hp-Math.Max(1,actor.SanctuaryAuraAttack/10));
                actor.SanctuaryAuraTurns--;
            }
            if(actor.Poison.Count>0 && actor.Alive)
            {
                int poisonDamage=0, remaining=0;
                foreach(PoisonStack stack in actor.Poison)
                {
                    poisonDamage+=Math.Max(1,stack.Attack*15/100);
                    stack.Remaining--;
                    remaining=Math.Max(remaining,stack.Remaining);
                }
                actor.Hp=Math.Max(0,actor.Hp-poisonDamage);
                actor.Poison.RemoveAll(stack=>stack.Remaining<=0);
                actor.PoisonStacks=actor.Poison.Count; actor.PoisonTurns=remaining;
            }
            if(actor.GuardTurns>0 && --actor.GuardTurns==0) { actor.Guarding=false; actor.GuardHits=0; actor.GuardPercent=0; }
            if(actor.StealthTurns>0 && --actor.StealthTurns==0) actor.Focused=false;
            if(actor.MarkTurns>0) actor.MarkTurns--;
            if(actor.AttackDownTurns>0) actor.AttackDownTurns--;
            if(actor.DefenseDownTurns>0) actor.DefenseDownTurns--;
            if(actor.DamageTakenTurns>0) actor.DamageTakenTurns--;
            if(actor.SlowTurns>0) actor.SlowTurns--;
            if(actor.SanctuaryTurns>0) actor.SanctuaryTurns--;
            if(actor.DodgeTurns>0) actor.DodgeTurns--;
            if(actor.CounterTurns>0) actor.CounterTurns--;
            if(actor.SpeedBoostTurns>0) actor.SpeedBoostTurns--;
            queue.RemoveAt(0); queue.RemoveAll(u=>!u.Alive);
            var enemies=Units.FindAll(u=>!u.Ally && u.Alive); enemies.Sort((a,b)=>a.Position.CompareTo(b.Position));
            for(int i=0;i<enemies.Count;i++) enemies[i].Position=i;
            if(!Units.Exists(u=>u.Ally && u.Alive)) Result=Outcome.Defeat;
            else if(!Units.Exists(u=>!u.Ally && u.Alive)) Result=Outcome.Victory;
            if(Result!=Outcome.Fighting) { queue.Clear(); return; }
            if(queue.Count==0) BeginRound(); else BeginTurn();
        }
    }
}
