using UnityEngine;

public sealed partial class GuildHallRuntime
{
    private int day = 1;
    private bool isDaytime;
    private int daytimeTab;
    private Texture2D daytimeMap;
    private readonly Rect[] mapRegions =
    {
        new Rect(165, 168, 400, 330), new Rect(165, 628, 400, 310),
        new Rect(752, 354, 416, 336), new Rect(1394, 168, 400, 330),
        new Rect(1336, 628, 400, 310), new Rect(650, 132, 450, 215)
    };
    private string dayNotice = "";
    private readonly string[] materialNames =
    {
        "곡물", "산딸기", "고기", "목재", "철광석", "가죽",
        "약초", "버섯", "달빛꽃", "수액", "감자", "사과", "치즈", "구리광석", "석탄", "은광석", "수정", "석재",
        "용의 비늘", "마왕의 정수", "고대 유물 조각", "불사조 재", "그림자 천", "별의 파편"
    };
    private readonly int[] materials = new int[24];
    private int dailyActionsRemaining;
    private readonly int[] materialUnlockLevels = { 1,1,1,1,1,1, 2,3,7,4,2,3,4,2,3,6,7,1, 6,8,7,7,8,9 };
    private readonly int[] materialMinYield = { 2,2,1,2,1,1, 1,1,1,1,2,1,1,1,1,1,1,2, 0,0,0,0,0,0 };
    private readonly int[] materialMaxYield = { 3,3,2,3,2,2, 2,2,2,2,3,2,2,2,2,2,2,3, 0,0,0,0,0,0 };
    private readonly int[] forestMaterials = { 1, 3, 6, 7, 8, 9 };
    private readonly int[] farmMaterials = { 0, 2, 5, 10, 11, 12 };
    private readonly int[] mineMaterials = { 4, 13, 14, 15, 16, 17 };
    // Recipes use the balance draft's 18 gatherable and 6 battle-reward materials.
    private static int[] Recipe(params int[] materialAndCountPairs)
    {
        int[] recipe = new int[24];
        for (int i = 0; i + 1 < materialAndCountPairs.Length; i += 2)
            recipe[materialAndCountPairs[i]] = materialAndCountPairs[i + 1];
        return recipe;
    }
    // Inventory order: food (9), weapon (12), armor (12); class gear is Fighter/Thief/Archer/Mage.
    private int[][] recipes =
    {
        Recipe(0,2), Recipe(1,2,10,1), Recipe(2,2), Recipe(2,2,6,1,10,1), Recipe(0,2,7,2,12,1), Recipe(2,2,11,2,14,1),
        Recipe(2,3,12,2,7,2,21,1), Recipe(0,2,1,3,8,1,23,1), Recipe(2,4,12,2,6,2,18,1),
        Recipe(3,1,4,2), Recipe(3,1,5,1,4,1), Recipe(3,2,5,1), Recipe(3,2,1,1),
        Recipe(4,4,14,2,3,1), Recipe(3,2,4,2,5,2), Recipe(3,4,5,2,9,1), Recipe(3,2,16,1,8,1),
        Recipe(4,4,15,2,14,2,18,1), Recipe(3,4,15,2,5,3,9,2,20,1), Recipe(3,4,5,3,9,2,20,1), Recipe(3,3,16,2,8,2,15,1,19,1),
        Recipe(5,3), Recipe(5,3,6,1), Recipe(5,2,1,1), Recipe(5,2,1,1),
        Recipe(5,4,4,2,13,1), Recipe(5,4,6,2,4,1), Recipe(5,4,4,2,13,1), Recipe(5,3,16,1,8,1),
        Recipe(4,4,15,3,5,3,14,2,22,1), Recipe(5,5,6,3,18,1), Recipe(4,4,15,3,5,3,14,2,22,1), Recipe(5,4,16,2,8,2,23,1)
    };

    private void BeginNextDay()
    {
        if (TryDepartForCombat()) return;
        AdvanceToNextDay();
    }

    private void AdvanceToNextDay()
    {
        ClosePopup();
        day++;
        isDaytime = true;
        daytimeTab = -1;
        dailyActionsRemaining = DailyActionCapacity();
        int expiredCount = 0;
        foreach (Quest quest in quests)
        {
            if (!quest.collected || quest.completed || quest.expired || day < quest.expiresOnDay) continue;
            quest.expired = true;
            expiredCount++;
        }
        GenerateDailyQuests();
        dayNotice = "새로운 하루입니다. 재료가 보충되었습니다." + (expiredCount > 0 ? " 기한이 지난 의뢰 " + expiredCount + "건이 만료되었습니다." : "");
        trainedToday.Clear();
        presentation.SetHovered(-1);
        presentation.SetGuildmasterHovered(false);
        foreach (SpriteRenderer actor in presentation.Renderers) actor.enabled = false;
    }

    private void BeginEvening()
    {
        isDaytime = false;
        ClosePopup();
        SeatEveningRoster();
    }

    private void DrawDaytime()
    {
        if (daytimeMap == null)
        {
            daytimeMap = Resources.Load<Texture2D>("GuildDayMap-v2");
            if (daytimeMap != null) daytimeMap.filterMode = FilterMode.Point;
        }
        // Opaque full-screen destination, never an overlay on the tavern.
        PixelRect(new Rect(0, 0, 1920, 1080), new Color(.09f, .17f, .15f));
        if (daytimeMap != null) GUI.DrawTexture(new Rect(0, 0, 1920, 1080), daytimeMap, ScaleMode.StretchToFill);
        if (managingSkillsGuest != null)
        {
            DrawSkillTreeWindow(managingSkillsGuest);
            return;
        }
        GUI.Box(new Rect(34, 28, 550, 106), "", panelStyle);
        GUI.Label(new Rect(58, 38, 500, 42), "DAY " + day.ToString("00") + " · 낮의 길드 영지", headingStyle);
        GUI.Label(new Rect(58, 80, 500, 38), "금고 " + guildGold + " G · 방문할 장소를 선택하세요", labelStyle);
        DrawManagementToolbar();
        if (managementPage != 0)
        {
            DrawManagementPage();
            return;
        }

        if (daytimeTab < 0)
        {
            string[] names = { "숲", "농장", "길드", "광산", "마을", "훈련장" };
            string[] hints = { "숲 재료 6종 채집", "농장 재료 6종 수집", "낮 종료 · 저녁 영업 시작", "광산 재료 6종 채굴", "주민 의뢰 게시판", "길드 소속 용사 훈련 · 스킬 관리" };
            for (int i = 0; i < mapRegions.Length; i++)
            {
                Rect region = mapRegions[i];
                bool hover = region.Contains(Event.current.mousePosition);
                Rect label = new Rect(region.x, region.yMax - 58, region.width, 58);
                GUI.Box(label, names[i], hover ? selectedTabStyle : tabStyle);
                if (hover)
                {
                    GUI.Box(new Rect(region.x - 20, region.yMax + 5, region.width + 40, 44), "", panelStyle);
                    GUI.Label(new Rect(region.x - 8, region.yMax + 5, region.width + 16, 44), hints[i], labelStyle);
                }
                if (GUI.Button(region, GUIContent.none, hoverStyle))
                {
                    if (i == 2) BeginEvening();
                    else daytimeTab = i;
                    return;
                }
            }
            GUI.Box(new Rect(34, 988, 1398, 66), "", panelStyle);
            GUI.Label(new Rect(54, 997, 1358, 48), dayNotice, labelStyle);
            return;
        }

        // Location activities replace the map with their own full-page content.
        PixelRect(new Rect(0, 152, 1920, 928), new Color(.055f, .066f, .11f, 1));
        string title = daytimeTab == 0 ? "숲 · 채집과 벌목" : daytimeTab == 1 ? "농장 · 식재료와 가죽" : daytimeTab == 3 ? "광산 · 광물 채굴" : daytimeTab == 4 ? "마을 · 의뢰 게시판" : "훈련장 · 길드원 육성";
        GUI.Label(new Rect(68, 190, 1250, 58), title, headingStyle);
        GUI.Label(new Rect(68, 266, 1768, 58), "오늘 남은 행동 " + dailyActionsRemaining + "/" + DailyActionCapacity() + "회 (길드 레벨 기본 " + (2 + (guildLevel - 1) / 2) + " + 행동력 시설 " + facilityLevels[4] + ") · 수집/훈련 1회마다 1회 사용", labelStyle);
        if (GUI.Button(new Rect(1510, 184, 330, 64), "← 영지 지도로", buttonStyle)) { daytimeTab = -1; return; }
        if (daytimeTab == 4) DrawRequestCollection();
        else if (daytimeTab == 5) DrawTrainingHall();
        else DrawGathering();
        GUI.Label(new Rect(68, 930, 1768, 70), dayNotice, labelStyle);
    }

    private void DrawGathering()
    {
        int[] available = daytimeTab == 0 ? forestMaterials : daytimeTab == 1 ? farmMaterials : mineMaterials;
        for (int slot = 0; slot < available.Length; slot++)
        {
            int i = available[slot];
            float x = 68 + slot % 3 * 596, y = 390 + slot / 3 * 246;
            GUI.Box(new Rect(x, y, 574, 220), "", panelStyle);
            DrawMaterialIcon(new Rect(x + 18, y + 62, 92, 92), i);
            GUI.Label(new Rect(x + 126, y + 18, 420, 48), materialNames[i], headingStyle);
            bool unlocked = guildLevel >= materialUnlockLevels[i];
            GUI.Label(new Rect(x + 126, y + 68, 420, 42), unlocked ? "보유 " + materials[i] + "개 · 1회 " + materialMinYield[i] + "~" + materialMaxYield[i] + "개" : "길드 레벨 " + materialUnlockLevels[i] + " 해금", labelStyle);
            GUI.enabled = unlocked && dailyActionsRemaining > 0;
            string gatherLabel = !unlocked ? "아직 해금되지 않았습니다" : dailyActionsRemaining > 0 ? "수집하기 · 행동 1회" : "오늘 행동 횟수 소진";
            if (GUI.Button(new Rect(x + 126, y + 132, 420, 56), gatherLabel, buttonStyle))
            {
                int amount = Random.Range(materialMinYield[i], materialMaxYield[i] + 1);
                materials[i] += amount;
                dailyActionsRemaining--;
                dayNotice = materialNames[i] + " " + amount + "개를 얻었습니다. 남은 행동 " + dailyActionsRemaining + "회.";
                ShowToast(materialNames[i] + " " + amount + "개 획득  ·  행동력 -1  ·  남은 행동 " + dailyActionsRemaining + "회");
            }
            GUI.enabled = true;
        }
    }

    private void DrawRequestCollection()
    {
        if (GUI.Button(new Rect(68, 344, 340, 52), "간단 의뢰", boardBattleTab ? tabStyle : selectedTabStyle)) boardBattleTab = false;
        if (GUI.Button(new Rect(432, 344, 340, 52), "전투 의뢰", boardBattleTab ? selectedTabStyle : tabStyle)) boardBattleTab = true;
        if (boardBattleTab) { DrawBattleRequestBoard(); return; }
        GUI.Label(new Rect(68, 402, 1768, 42), "각 의뢰의 기한은 1~3일 중 무작위로 정해집니다. 기한 안에 용사에게 맡기지 않으면 만료됩니다.", labelStyle);
        int availableCount = 0;
        foreach (Quest quest in quests)
            if (!quest.expired && !quest.completed && !(quest.collected && quest.acceptedDay < day)) availableCount++;
        Rect questView = new Rect(68, 458, 1768, 428);
        questBoardScrollPosition = GUI.BeginScrollView(questView, questBoardScrollPosition,
            new Rect(0, 0, 1840, Mathf.Max(428, availableCount * 126)), false, true);
        int visible = 0;
        for (int i = 0; i < quests.Count; i++)
        {
            Quest quest = quests[i];
            if (quest.expired || quest.completed || (quest.collected && quest.acceptedDay < day)) continue;
            float y = visible++ * 126;
            GUI.Box(new Rect(68, y, 1768, 110), "", panelStyle);
            GUI.Label(new Rect(90, y + 8, 1240, 38), quest.name + " · 권장 Lv." + quest.recommendedLevel + " · 용사 보수 " + quest.reward + " G", headingStyle);
            GUI.Label(new Rect(90, y + 49, 1240, 48), quest.detail + "   |   성공 시 길드 수수료 " + quest.guildCommission + " G · 용사 EXP " + quest.heroExperienceReward + " · 길드 EXP " + quest.guildExperienceReward, labelStyle);
            int remaining = quest.collected ? Mathf.Max(0, quest.expiresOnDay - day) : quest.deadlineDays;
            GUI.Label(new Rect(962, y + 29, 400, 54), "기한 " + remaining + "일", labelStyle);
            GUI.enabled = !quest.collected;
            if (GUI.Button(new Rect(1408, y + 26, 404, 58), quest.collected ? "수락 완료" : "수락", buttonStyle))
            {
                quest.collected = true;
                quest.acceptedDay = day;
                quest.expiresOnDay = day + quest.deadlineDays;
                dayNotice = quest.name + " 의뢰를 접수했습니다.";
            }
            GUI.enabled = true;
        }
        GUI.EndScrollView();
        if (visible == 0) GUI.Label(new Rect(90, 470, 1600, 80), "새로운 간단 의뢰가 없습니다. 수락한 의뢰는 밤에 용사에게 요청할 수 있습니다.", labelStyle);
    }
}
