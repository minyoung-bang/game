using System.Collections.Generic;
using UnityEngine;
using GuildCombat;

public sealed partial class GuildHallRuntime
{
    private sealed class CombatTestChoice
    {
        public int job, branch, level;
        public CombatTestChoice(int job, int branch, int level)
        { this.job = job; this.branch = branch; this.level = level; }
    }

    private readonly List<CombatTestChoice> combatTestParty = new List<CombatTestChoice>();
    private bool combatTestSetupOpen, combatTestMode;
    private int combatTestDefaultLevel = 15, combatTestEnemyLevel = 10, combatTestEnemyCount = 2;
    private bool combatTestWolfEnemy;
    private string combatTestNotice = "전직을 눌러 최대 4명을 편성하세요. 선택 순서가 전투 위치입니다.";

    private void ToggleCombatTestChoice(int job, int branch)
    {
        int current = combatTestParty.FindIndex(c => c.job == job && c.branch == branch);
        if (current >= 0)
        {
            combatTestParty.RemoveAt(current);
            combatTestNotice = "편성에서 제외했습니다. 남은 용사의 위치가 앞으로 당겨집니다.";
        }
        else if (combatTestParty.Count < 4)
        {
            combatTestParty.Add(new CombatTestChoice(job, branch, combatTestDefaultLevel));
            combatTestNotice = CareerTracks[job][branch].name + " 편성 완료 · 아군 " + combatTestParty.Count + "번 위치";
        }
        else combatTestNotice = "전투에는 최대 4명까지만 편성할 수 있습니다. 다른 전직을 먼저 제외하세요.";
    }

    private void DrawCombatTestSetup()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        { combatTestSetupOpen = false; Event.current.Use(); return; }

        presentation.SetHovered(-1);
        presentation.SetGuildmasterHovered(false);
        PixelRect(new Rect(0, 0, 1920, 1080), new Color(.035f, .055f, .085f));
        GUI.Label(new Rect(110, 36, 1220, 68), "개발용 전투 테스트 · 전직 편성", headingStyle);
        GUI.Label(new Rect(110, 102, 1610, 42), "실제 용사·의뢰·금고·날짜는 바뀌지 않습니다. 전직 12종 중 1~4명을 선택하세요.", labelStyle);
        if (GUI.Button(new Rect(1660, 46, 156, 64), "돌아가기", buttonStyle))
        { combatTestSetupOpen = false; return; }

        GUI.Label(new Rect(110, 151, 350, 44), "새 용사 기본 레벨", labelStyle);
        int[] levels = { 3, 9, 15, 20 };
        for (int i = 0; i < levels.Length; i++)
        {
            int value = levels[i];
            if (GUI.Button(new Rect(438 + i * 126, 146, 115, 52), "Lv." + value,
                combatTestDefaultLevel == value ? selectedTabStyle : buttonStyle))
            {
                combatTestDefaultLevel = value;
                foreach (CombatTestChoice choice in combatTestParty) choice.level = value;
            }
        }
        GUI.Label(new Rect(970, 151, 785, 44), "전직 스킬 해금: Lv.3 / 9 / 15", labelStyle);

        const float startX = 105f, gap = 425f, cardWidth = 395f;
        for (int job = 0; job < ClassNames.Length; job++)
        {
            float x = startX + gap * job;
            PixelRect(new Rect(x, 222, cardWidth, 7), ClassColors[job]);
            GUI.Label(new Rect(x + 8, 237, cardWidth - 16, 45), ClassNames[job], headingStyle);
            for (int branch = 0; branch < CareerTracks[job].Length; branch++)
            {
                CareerTrack track = CareerTracks[job][branch];
                int slot = combatTestParty.FindIndex(c => c.job == job && c.branch == branch);
                Rect card = new Rect(x, 290 + branch * 112, cardWidth, 98);
                GUI.Box(card, "", slot >= 0 ? selectedTabStyle : cardStyle);
                GUI.Label(new Rect(x + 16, card.y + 8, cardWidth - 32, 38),
                    track.name + (slot >= 0 ? " · 아군 " + (slot + 1) + "번" : ""), headingStyle);
                GUI.Label(new Rect(x + 16, card.y + 53, cardWidth - 32, 33), track.style, combatSmallStyle ?? labelStyle);
                if (GUI.Button(card, GUIContent.none, hoverStyle)) ToggleCombatTestChoice(job, branch);
            }
        }

        GUI.Label(new Rect(110, 636, 1400, 48), "출전 편성 · " + combatTestParty.Count + " / 4명", headingStyle);
        for (int slot = 0; slot < 4; slot++)
        {
            float x = startX + gap * slot;
            GUI.Box(new Rect(x, 690, cardWidth, 144), "", cardStyle);
            if (slot >= combatTestParty.Count)
            {
                GUI.Label(new Rect(x + 14, 730, cardWidth - 28, 50), "아군 " + (slot + 1) + " · 빈 자리", labelStyle);
                continue;
            }
            CombatTestChoice choice = combatTestParty[slot];
            GUI.Label(new Rect(x + 14, 699, cardWidth - 28, 43),
                (slot + 1) + "번 · " + ClassNames[choice.job] + " / " + CareerTracks[choice.job][choice.branch].name, headingStyle);
            GUI.Label(new Rect(x + 14, 757, 115, 45), "Lv." + choice.level, labelStyle);
            if (GUI.Button(new Rect(x + 151, 753, 72, 54), "−", buttonStyle)) choice.level = Mathf.Max(3, choice.level - 1);
            if (GUI.Button(new Rect(x + 230, 753, 72, 54), "+", buttonStyle)) choice.level = Mathf.Min(20, choice.level + 1);
            if (GUI.Button(new Rect(x + 310, 753, 72, 54), "빼기", buttonStyle))
            { combatTestParty.RemoveAt(slot); break; }
        }

        GUI.Label(new Rect(110, 864, 155, 46), "적 수", labelStyle);
        if (GUI.Button(new Rect(260, 858, 66, 54), "−", buttonStyle)) combatTestEnemyCount = Mathf.Max(1, combatTestEnemyCount - 1);
        GUI.Label(new Rect(335, 864, 68, 46), combatTestEnemyCount.ToString(), labelStyle);
        if (GUI.Button(new Rect(399, 858, 66, 54), "+", buttonStyle)) combatTestEnemyCount = Mathf.Min(4, combatTestEnemyCount + 1);
        GUI.Label(new Rect(525, 864, 165, 46), "적 레벨", labelStyle);
        if (GUI.Button(new Rect(695, 858, 66, 54), "−", buttonStyle)) combatTestEnemyLevel = Mathf.Max(1, combatTestEnemyLevel - 1);
        GUI.Label(new Rect(770, 864, 90, 46), "Lv." + combatTestEnemyLevel, labelStyle);
        if (GUI.Button(new Rect(865, 858, 66, 54), "+", buttonStyle)) combatTestEnemyLevel = Mathf.Min(20, combatTestEnemyLevel + 1);
        if (GUI.Button(new Rect(1040, 858, 380, 54),
            "적 종류 · " + (combatTestWolfEnemy ? "야생 늑대" : "훈련 마물"), buttonStyle))
            combatTestWolfEnemy = !combatTestWolfEnemy;

        GUI.Label(new Rect(110, 946, 1160, 55), combatTestNotice, labelStyle);
        GUI.enabled = combatTestParty.Count > 0;
        if (GUI.Button(new Rect(1450, 928, 366, 84), "테스트 전투 시작 →", buttonStyle)) StartCombatTest();
        GUI.enabled = true;
    }

    private void StartCombatTest()
    {
        if (combatTestParty.Count == 0) return;
        var heroes = new List<Unit>();
        for (int position = 0; position < combatTestParty.Count; position++)
        {
            CombatTestChoice choice = combatTestParty[position];
            string job = ClassNames[choice.job];
            string branch = CareerTracks[choice.job][choice.branch].name;
            var virtualGuest = new Guest(branch, choice.level, 0, 0, true, job) { advancement = branch };
            HeroStats stats = CalculateHeroStats(job, choice.level);
            int mana = job == "마법사" ? 20 + 3 * (choice.level - 1) : 12 + 2 * (choice.level - 1);
            var hero = new Unit { Id = position, Name = branch, Job = job, Level = choice.level, Ally = true, Position = position,
                MaxHp = stats.hp, Hp = stats.hp, MaxMana = mana, Mana = mana, Attack = stats.attack,
                Defense = stats.defense, Speed = stats.speed };
            foreach (string skillId in LearnedSkillsFor(virtualGuest)) hero.Skills.Add(BasicSkills.Resolve(skillId));
            heroes.Add(hero);
        }

        combatTestMode = true;
        combatRequest = new BattleRequest("전직 스킬 테스트", "개발용 가상 전투", 1, combatTestEnemyLevel,
            combatTestEnemyCount, 1, 0, 0, 0, 0);
        combatWave = 1;
        combat = new Session(heroes, CreateCombatEnemies());
        combatTestSetupOpen = false;
        combatQueueY.Clear(); combatInspected = null; combatTab = 0; ResetCombatSelection();
        combatActorId = -1; combatEnemyAt = Time.unscaledTime + 1.2f;
        combatMessage = "개발용 전투 · 전직 스킬을 시험하세요. 종료해도 게임 진행은 그대로입니다.";
        foreach (SpriteRenderer renderer in presentation.Renderers) renderer.enabled = false;
    }

    private List<Unit> CreateCombatTestEnemies()
    {
        int level = combatTestEnemyLevel;
        var enemies = new List<Unit>();
        for (int i = 0; i < combatTestEnemyCount; i++)
        {
            int hp = 16 + level * 5;
            string kind = combatTestWolfEnemy ? "야생 늑대" : "훈련 마물";
            var enemy = new Unit { Id = 100 + i, Name = kind + " " + (i + 1), Job = kind, Level = level,
                Ally = false, Position = i, MaxHp = hp, Hp = hp, Attack = 4 + level,
                Defense = level / 3, Speed = 3 + i % 2 + level / 5 };
            EnemySkills.AddFor(enemy);
            enemies.Add(enemy);
        }
        return enemies;
    }

    private void DrawCombatTestHeroIcon(Rect rect, Unit unit)
    {
        int index = ClassIndex(unit.Job);
        PixelRect(rect, new Color(.08f, .10f, .16f));
        Color color = ClassColors[index];
        float cx = rect.center.x, size = Mathf.Min(rect.width, rect.height);
        PixelRect(new Rect(cx - size * .14f, rect.y + size * .15f, size * .28f, size * .27f), new Color(.96f, .83f, .69f));
        PixelRect(new Rect(cx - size * .24f, rect.y + size * .42f, size * .48f, size * .38f), color);
        PixelRect(new Rect(cx - size * .3f, rect.y + size * .75f, size * .6f, size * .09f), new Color(.28f, .32f, .43f));
    }

    private void ExitCombatTest()
    {
        combat = null; combatRequest = null; combatInspected = null; combatTestMode = false;
        combatParty.Clear(); combatQueueY.Clear(); ResetCombatSelection(); combatActorId = -1;
        for (int i = 0; i < presentation.Renderers.Length; i++)
            presentation.Renderers[i].enabled = presentation.SeatForGuest != null &&
                i < presentation.SeatForGuest.Length && presentation.SeatForGuest[i] >= 0;
        ShowToast("전투 테스트를 종료했습니다. 게임 진행 상황은 변경되지 않았습니다.");
    }
}
