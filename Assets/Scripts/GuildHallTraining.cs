using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    private sealed class CareerTrack
    {
        public readonly string name;
        public readonly string style;
        public readonly string[] skills;

        public CareerTrack(string name, string style, params string[] skills)
        { this.name = name; this.style = style; this.skills = skills; }
    }

    private static readonly int[] SkillUnlockLevels = { 3, 9, 15 };

    private static readonly CareerTrack[][] CareerTracks =
    {
        new[]
        {
            new CareerTrack("기사", "방어와 아군 보호에 특화", "방패 강타", "철벽 진형", "왕실 방패술"),
            new CareerTrack("버서커", "위험을 감수하는 강력한 근접 공격에 특화", "광전사의 일격", "분노 폭발", "살육 본능"),
            new CareerTrack("마검사", "검술과 마법을 결합한 공격에 특화", "마력 베기", "번개 돌진", "차원 참격"),
        },
        new[]
        {
            new CareerTrack("시프", "교란과 기회 포착에 특화", "약점 간파", "연막탄", "최루탄"),
            new CareerTrack("암살자", "은신 후 단일 대상 공격에 특화", "그림자 찌르기", "연속 암습", "죽음의 표식"),
            new CareerTrack("독술사", "독 중첩과 지속 피해에 특화", "맹독 바르기", "신경독", "맹독의 늪"),
        },
        new[]
        {
            new CareerTrack("신궁", "정확한 단발 사격에 특화", "관통 화살", "약점 사격", "필중의 일격"),
            new CareerTrack("보우마스터", "연속 사격과 범위 공격에 특화", "속사", "화살비", "천 개의 화살"),
            new CareerTrack("매직아처", "마법 사격과 지원에 특화", "마력 화살", "치유의 화살", "마력 폭우"),
        },
        new[]
        {
            new CareerTrack("원소술사", "원소 범위 공격에 특화", "화염 작렬", "폭풍", "해일"),
            new CareerTrack("프리스트", "회복과 보호에 특화", "치유의 빛", "회복의 파동", "성역"),
            new CareerTrack("흑마법사", "위험을 감수한 고위력 마법에 특화", "암흑 물질", "영혼 흡수", "저주의 폭풍"),
        }
    };

    private static readonly string[][] BasicSkillSets =
    {
        new[] { "베기", "방어 태세" },
        new[] { "급소 노리기", "은신" },
        new[] { "정밀 사격", "응급처치" },
        new[] { "마력탄", "마력 집중" }
    };

    private static readonly string[] ClassNames = { "전사", "도적", "궁수", "마법사" };
    private static readonly Color[] ClassColors =
    {
        new Color(.77f, .37f, .22f), new Color(.56f, .37f, .76f),
        new Color(.28f, .62f, .39f), new Color(.28f, .49f, .82f)
    };

    private int trainingTab;
    private readonly HashSet<Guest> trainedToday = new HashSet<Guest>();
    private Vector2 trainingScrollPosition;
    private Vector2 skillTreeScrollPosition;
    private Guest managingSkillsGuest;
    private string skillWindowNotice = "";
    private string hoveredSkillDescription = "스킬 위에 마우스를 올리면 설명이 표시됩니다.";

    private static int ClassIndex(string baseClass)
    {
        for (int i = 0; i < ClassNames.Length; i++)
            if (ClassNames[i] == baseClass) return i;
        return 0;
    }

    private static string[] BasicSkillsFor(string baseClass) => BasicSkillSets[ClassIndex(baseClass)];
    private static CareerTrack[] TracksFor(string baseClass) => CareerTracks[ClassIndex(baseClass)];
    private static string BasicSkillId(Guest guest, string skill) => guest.baseClass + ":기본:" + skill;
    private static string CareerSkillId(Guest guest, CareerTrack track, string skill) => guest.baseClass + ":" + track.name + ":" + skill;

    private static List<string> LearnedSkillsFor(Guest guest)
    {
        var learned = new List<string>();
        foreach (string skill in BasicSkillsFor(guest.baseClass)) learned.Add(BasicSkillId(guest, skill));
        foreach (CareerTrack track in TracksFor(guest.baseClass))
            if (track.name == guest.advancement)
                for (int i=0;i<track.skills.Length;i++)
                    if (guest.level >= SkillUnlockLevels[i]) learned.Add(CareerSkillId(guest, track, track.skills[i]));
        return learned;
    }

    private void DrawTrainingHall()
    {
        if (managingSkillsGuest != null)
        {
            DrawSkillTreeWindow(managingSkillsGuest);
            return;
        }

        if (GUI.Button(new Rect(68, 344, 420, 56), "용사 훈련", trainingTab == 0 ? selectedTabStyle : tabStyle)) trainingTab = 0;
        if (GUI.Button(new Rect(504, 344, 420, 56), "스킬 관리", trainingTab == 1 ? selectedTabStyle : tabStyle)) trainingTab = 1;
        GUI.Label(new Rect(68, 408, 1768, 54), trainingTab == 0
            ? "길드 소속 용사만 이용할 수 있습니다. 용사별 하루 1회 훈련에 행동 포인트 1회를 쓰며, 경험치가 쌓이면 레벨이 오릅니다."
            : "기본 스킬 2개와 레벨에 따라 습득한 전직 스킬을 모두 전투에서 사용할 수 있습니다.", labelStyle);

        Rect view = new Rect(68, 476, 1768, 420);
        int memberCount = GuildMemberCount();
        trainingScrollPosition = GUI.BeginScrollView(view, trainingScrollPosition, new Rect(0, 0, 1728, Mathf.Max(420, memberCount * 132)), false, true);
        int row = 0;
        for (int i = 0; i < guests.Count; i++)
        {
            Guest guest = guests[i];
            if (!guest.guildMember) continue;
            float y = row++ * 132;
            Rect card = new Rect(0, y, 1696, 116);
            GUI.Box(card, "", panelStyle);
            GUI.Label(new Rect(24, y + 12, 500, 42), guest.name + " · " + guest.baseClass, headingStyle);

            if (trainingTab == 0)
            {
                int threshold = TrainingExperienceToNextLevel(guest.level);
                GUI.Label(new Rect(24, y + 52, 1080, 34), "레벨 " + guest.level + "   다음 레벨까지 " + guest.trainingExperience + "/" + threshold + " EXP", labelStyle);
                GUI.Label(new Rect(24, y + 82, 1080, 30), HeroStatsLabel(guest), labelStyle);
                bool trained = trainedToday.Contains(guest);
                bool hasActions = dailyActionsRemaining > 0;
                GUI.enabled = !trained && guest.level < 20 && hasActions;
                string button = guest.level >= 20 ? "최고 레벨" : trained ? "오늘 훈련 완료" : !hasActions ? "행동 포인트 부족" : "훈련 · 행동 1회 · +" + TrainingExperienceReward() + " EXP";
                if (GUI.Button(new Rect(1248, y + 26, 416, 64), button, buttonStyle)) TrainGuest(guest);
                GUI.enabled = true;
            }
            else
            {
                string branchLabel = string.IsNullOrEmpty(guest.advancement) ? (guest.level < 3 ? "레벨 3에 전직" : "전직 계열 선택 가능") : guest.advancement;
                GUI.Label(new Rect(24, y + 58, 820, 42), guest.baseClass + " · " + branchLabel + " · 사용 가능 " + LearnedSkillsFor(guest).Count + "개", labelStyle);
                if (GUI.Button(new Rect(1248, y + 26, 416, 64), "기술 변경  →", buttonStyle))
                {
                    managingSkillsGuest = guest;
                    skillTreeScrollPosition = Vector2.zero;
                    skillWindowNotice = "";
                    hoveredSkillDescription = "스킬 위에 마우스를 올리면 설명이 표시됩니다.";
                }
                GUI.enabled = true;
            }
        }
        GUI.EndScrollView();
        if (memberCount == 0) GUI.Label(new Rect(92, 526, 1540, 80), "길드 소속 용사가 없습니다. 밤에 방문 용사를 영입한 뒤 이용해 주세요.", headingStyle);
        GUI.Label(new Rect(68, 912, 1680, 44), trainingTab == 0
            ? "훈련 효율: 시설 레벨 " + facilityLevels[3] + " · 1회 훈련 " + TrainingExperienceReward() + " EXP · 행동력 시설 Lv." + facilityLevels[4]
            : "레벨 3부터 전직 계열 선택 · 전직 스킬은 레벨 3, 9, 15에 해금됩니다.", labelStyle);
    }

    private void DrawSkillTreeWindow(Guest guest)
    {
        if (guest == null || !guest.guildMember)
        {
            managingSkillsGuest = null;
            return;
        }
        PixelRect(new Rect(0, 0, 1920, 1080), new Color(.015f, .02f, .04f, .88f));
        Rect window = new Rect(70, 112, 1780, 844);
        GUI.Box(window, "", panelStyle);
        GUI.Label(new Rect(106, 132, 1300, 54), guest.name + " · " + guest.baseClass + " 기술 트리", headingStyle);
        GUI.Label(new Rect(106, 188, 1490, 42), "레벨 " + guest.level + " · 전직 " + (string.IsNullOrEmpty(guest.advancement) ? "미선택" : guest.advancement) + " · 사용 가능 " + LearnedSkillsFor(guest).Count + "개", labelStyle);
        if (GUI.Button(new Rect(1760, 128, 56, 54), "×", buttonStyle) ||
            (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
        {
            managingSkillsGuest = null;
            Event.current.Use();
            return;
        }

        hoveredSkillDescription = "스킬 위에 마우스를 올리면 설명이 표시됩니다.";
        DrawBasicSkillPanel(guest);
        CareerTrack[] tracks = TracksFor(guest.baseClass);
        Rect treeView = new Rect(376, 258, 1438, 562);
        const float treeWidth = 1438;
        skillTreeScrollPosition = GUI.BeginScrollView(treeView, skillTreeScrollPosition, new Rect(0, 0, treeWidth, 550), true, false);
        for (int slot = 0; slot < SkillUnlockLevels.Length; slot++)
        {
            float x = 176 + slot * 222;
            GUI.Label(new Rect(x, 4, 190, 32), "레벨 " + SkillUnlockLevels[slot], labelStyle);
        }
        for (int branch = 0; branch < tracks.Length; branch++)
        {
            CareerTrack track = tracks[branch];
            float rowY = 42 + branch * 174;
            bool selectedTrack = guest.advancement == track.name;
            bool canChooseTrack = guest.level >= 3 && string.IsNullOrEmpty(guest.advancement);
            Rect branchButton = new Rect(8, rowY + 38, 148, 84);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = selectedTrack || canChooseTrack;
            if (GUI.Button(branchButton, selectedTrack ? "✓ " + track.name : track.name, selectedTrack ? selectedTabStyle : tabStyle))
            {
                if (canChooseTrack)
                {
                    guest.advancement = track.name;
                    skillWindowNotice = guest.name + "이(가) " + track.name + " 계열을 선택했습니다. 해금된 스킬은 자동으로 전투에 적용됩니다.";
                }
            }
            GUI.enabled = previousEnabled;
            if (guest.level < 3 && string.IsNullOrEmpty(guest.advancement))
                GUI.Label(new Rect(8, rowY + 122, 148, 30), "Lv.3 선택", labelStyle);
            else if (!selectedTrack && !string.IsNullOrEmpty(guest.advancement))
                GUI.Label(new Rect(8, rowY + 122, 148, 30), "미선택 계열", labelStyle);

            for (int slot = 0; slot < track.skills.Length; slot++)
            {
                float x = 176 + slot * 222;
                Rect skillRect = new Rect(x, rowY + 20, 190, 132);
                if (slot < track.skills.Length - 1)
                    PixelRect(new Rect(skillRect.xMax, rowY + 82, 32, 4), new Color(.46f, .42f, .34f));
                int unlockLevel = SkillUnlockLevels[slot];
                bool learned = selectedTrack && guest.level >= unlockLevel;
                string skillId = CareerSkillId(guest, track, track.skills[slot]);
                bool equipped = learned;
                bool hovered = skillRect.Contains(Event.current.mousePosition);
                DrawSkillCard(skillRect, track.skills[slot], unlockLevel, learned, equipped, branch);
                if (hovered)
                {
                    hoveredSkillDescription = BuildSkillDescription(track, track.skills[slot], slot, unlockLevel, selectedTrack, learned, equipped);
                }
            }
        }
        GUI.EndScrollView();

        GUI.Box(new Rect(104, 838, 1710, 92), "", panelStyle);
        GUI.Label(new Rect(128, 846, 1660, 74), hoveredSkillDescription, labelStyle);
        GUI.Label(new Rect(106, 930, 1710, 30),
            string.IsNullOrEmpty(skillWindowNotice)
                ? "기본 스킬은 항상 습득 상태입니다. 전직 계열은 최초 선택 후 고정되며, 잠긴 스킬은 회색으로 표시됩니다."
                : skillWindowNotice,
            labelStyle);
    }

    private void DrawBasicSkillPanel(Guest guest)
    {
        int classIndex = ClassIndex(guest.baseClass);
        Color tint = ClassColors[classIndex];
        Rect panel = new Rect(94, 258, 258, 562);
        GUI.Box(panel, "", panelStyle);
        GUI.Label(new Rect(112, 276, 222, 50), guest.baseClass + " 기본", headingStyle);
        string[] skills = BasicSkillsFor(guest.baseClass);
        for (int i = 0; i < skills.Length; i++)
        {
            float y = 344 + i * 214;
            string id = BasicSkillId(guest, skills[i]);
            bool equipped = true;
            Rect card = new Rect(110, y, 226, 184);
            DrawSkillCard(card, skills[i], 1, true, equipped, classIndex, "기본");
            if (card.Contains(Event.current.mousePosition))
            {
                hoveredSkillDescription = skills[i] + " · 기본 스킬\n" + GuildCombat.BasicSkills.Resolve(id).Description + "\n항상 전투에서 사용 가능합니다.";
            }
        }
    }

    private void DrawSkillCard(Rect rect, string name, int unlockLevel, bool learned, bool equipped, int colorIndex, string prefix = "")
    {
        Color fill = learned ? ClassColors[Mathf.Clamp(colorIndex, 0, ClassColors.Length - 1)] : new Color(.24f, .25f, .28f);
        fill = Color.Lerp(fill, new Color(.08f, .1f, .15f), learned ? .42f : .1f);
        PixelRect(rect, fill);
        if (equipped)
        {
            Color border = new Color(1f, .78f, .25f);
            PixelRect(new Rect(rect.x, rect.y, rect.width, 5), border);
            PixelRect(new Rect(rect.x, rect.yMax - 5, rect.width, 5), border);
            PixelRect(new Rect(rect.x, rect.y, 5, rect.height), border);
            PixelRect(new Rect(rect.xMax - 5, rect.y, 5, rect.height), border);
        }
        GUI.color = learned ? Color.white : new Color(.58f, .59f, .62f);
        bool largeCard = rect.height > 160;
        if (!string.IsNullOrEmpty(prefix))
            GUI.Label(new Rect(rect.x + 12, rect.y + 8, rect.width - 24, 34), prefix, labelStyle);
        GUI.Label(new Rect(rect.x + 12, rect.y + (largeCard ? 44 : 12), rect.width - 24, largeCard ? 58 : 58), name, headingStyle);
        GUI.Label(new Rect(rect.x + 12, rect.yMax - (largeCard ? 52 : 42), rect.width - 24, largeCard ? 44 : 36), learned ? "습득 · 사용 가능" : "레벨 " + unlockLevel + " 해금", labelStyle);
        GUI.color = Color.white;
    }

    private string BuildSkillDescription(CareerTrack track, string skill, int slot, int unlockLevel, bool selectedTrack, bool learned, bool equipped)
    {
        string state = !selectedTrack ? (string.IsNullOrEmpty(managingSkillsGuest.advancement) ? "전직 계열 선택 후 해금됩니다." : "선택하지 않은 계열의 스킬입니다.")
            : learned ? "습득했습니다. 전투에서 바로 사용할 수 있습니다."
            : "레벨 " + unlockLevel + "에 해금됩니다.";
        return track.name + " · " + skill + "\n" + GuildCombat.BasicSkills.Resolve(managingSkillsGuest.baseClass + ":" + track.name + ":" + skill).Description + "\n" + state;
    }

    private static string BasicSkillDescription(int classIndex, int skillIndex)
    {
        string[][] descriptions =
        {
            new[] { "검으로 적 하나를 공격합니다.", "방어를 굳혀 받는 피해를 줄입니다." },
            new[] { "빈틈을 노려 적 하나를 공격합니다.", "빠르게 움직여 다음 공격을 준비합니다." },
            new[] { "활로 적 하나를 정확히 공격합니다.", "적과 거리를 벌려 안전을 확보합니다." },
            new[] { "마력을 모아 적 하나를 공격합니다.", "집중해 다음 마법의 위력을 높입니다." }
        };
        return descriptions[classIndex][skillIndex];
    }

    private int TrainingExperienceToNextLevel(int level) => HeroExperienceRequired(level);

    private void TrainGuest(Guest guest)
    {
        if (guest == null || !guest.guildMember || guest.level >= 20 || trainedToday.Contains(guest) || dailyActionsRemaining <= 0) return;
        trainedToday.Add(guest);
        dailyActionsRemaining--;
        guest.trainingRemainder += TrainingExperienceReward();
        int earnedExperience = Mathf.FloorToInt(guest.trainingRemainder + .0001f);
        guest.trainingRemainder -= earnedExperience;
        guest.experience += earnedExperience;
        guest.trainingExperience += earnedExperience;
        int levelsGained = 0;
        while (guest.level < 20 && guest.trainingExperience >= TrainingExperienceToNextLevel(guest.level))
        {
            guest.trainingExperience -= TrainingExperienceToNextLevel(guest.level);
            guest.level++;
            levelsGained++;
        }
        RefreshHeroStats(guest);
        ShowToast(guest.name + " 훈련 완료  ·  경험치 +" + earnedExperience + " EXP" +
            (levelsGained > 0 ? "  ·  레벨 " + guest.level + " 달성!" : "") + "  ·  행동력 -1");
        dayNotice = levelsGained > 0
            ? guest.name + "이(가) 훈련으로 레벨 " + guest.level + "이(가) 되었습니다!"
            : guest.name + "이(가) 훈련 경험치 " + earnedExperience + "을(를) 얻었습니다. 남은 행동 " + dailyActionsRemaining + "회.";
    }
}
