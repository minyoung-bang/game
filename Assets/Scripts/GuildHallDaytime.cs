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
    // Keep the original six indices stable: existing crafting recipes still use them.
    private readonly string[] materialNames =
    {
        "곡물", "산딸기", "고기", "목재", "철광석", "가죽",
        "약초", "버섯", "달빛꽃", "수액", "감자", "사과", "치즈", "구리광석", "석탄", "은광석", "수정", "석재"
    };
    private readonly int[] materials = new int[18];
    private readonly int[] dailySupply = new int[18];
    private readonly int[] forestMaterials = { 1, 3, 6, 7, 8, 9 };
    private readonly int[] farmMaterials = { 0, 2, 5, 10, 11, 12 };
    private readonly int[] mineMaterials = { 4, 13, 14, 15, 16, 17 };
    // Costs for the nine products, in the same order as the evening shop.
    private readonly int[][] recipes =
    {
        new[] {2,0,0,0,0,0}, new[] {0,2,0,0,0,0}, new[] {0,0,2,0,0,0},
        new[] {2,0,1,0,0,0}, new[] {1,2,0,0,0,0}, new[] {0,1,2,0,0,0},
        new[] {3,2,0,0,0,0}, new[] {2,0,3,0,0,0}, new[] {0,3,2,0,0,0},

        new[] {0,0,0,2,1,0}, new[] {0,0,0,2,0,1}, new[] {0,0,0,1,2,0},
        new[] {0,0,0,3,4,0}, new[] {0,0,0,4,0,3}, new[] {0,0,0,0,3,2},
        new[] {0,0,0,5,6,0}, new[] {0,0,0,0,5,4}, new[] {0,0,0,4,4,3},

        new[] {0,0,0,0,0,3}, new[] {0,0,0,2,1,0}, new[] {0,0,0,1,0,2},
        new[] {0,0,0,0,2,5}, new[] {0,0,0,3,3,0}, new[] {0,0,0,2,0,4},
        new[] {0,0,0,0,4,7}, new[] {0,0,0,4,6,0}, new[] {0,0,0,3,3,6}
    };

    private void BeginNextDay()
    {
        ClosePopup();
        day++;
        isDaytime = true;
        daytimeTab = -1;
        int expiredCount = 0;
        foreach (Quest quest in quests)
        {
            if (!quest.collected || quest.completed || quest.expired || day < quest.expiresOnDay) continue;
            quest.expired = true;
            expiredCount++;
        }
        dayNotice = "새로운 하루입니다. 재료가 보충되었습니다." + (expiredCount > 0 ? " 기한이 지난 의뢰 " + expiredCount + "건이 만료되었습니다." : "");
        for (int i = 0; i < dailySupply.Length; i++)
            dailySupply[i] = Random.Range(4, 8);
        trainedToday.Clear();
        presentation.SetHovered(-1);
        presentation.SetGuildmasterHovered(false);
        foreach (SpriteRenderer actor in presentation.Renderers) actor.enabled = false;
    }

    private void BeginEvening()
    {
        isDaytime = false;
        ClosePopup();
        presentation.Reseat(Random.Range(1, int.MaxValue), SelectEveningRoster());
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
        GUI.Label(new Rect(68, 266, 1768, 58), "행동 횟수 제한 없음 · 오늘의 공급량만 제한됩니다. 재료와 재고는 다음 날에도 유지됩니다.", labelStyle);
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
            GUI.Label(new Rect(x + 126, y + 68, 420, 42), "보유 " + materials[i] + "개 · 오늘 공급 " + dailySupply[i] + "개", labelStyle);
            GUI.enabled = dailySupply[i] > 0;
            if (GUI.Button(new Rect(x + 126, y + 132, 420, 56), dailySupply[i] > 0 ? "수집하기 (최대 2개)" : "오늘 공급 소진", buttonStyle))
            {
                int amount = Mathf.Min(2, dailySupply[i]);
                materials[i] += amount;
                dailySupply[i] -= amount;
                dayNotice = materialNames[i] + " " + amount + "개를 수집했습니다.";
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
        int visible = 0;
        for (int i = 0; i < quests.Length; i++)
        {
            Quest quest = quests[i];
            if (quest.expired || quest.completed || (quest.collected && quest.acceptedDay < day)) continue;
            float y = 458 + visible++ * 126;
            GUI.Box(new Rect(68, y, 1768, 110), "", panelStyle);
            GUI.Label(new Rect(90, y + 8, 1240, 38), quest.name + " · 난이도 " + quest.difficulty + " · 보수 " + quest.reward + " G", headingStyle);
            GUI.Label(new Rect(90, y + 49, 1240, 48), quest.detail, labelStyle);
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
        if (visible == 0) GUI.Label(new Rect(90, 470, 1600, 80), "새로운 간단 의뢰가 없습니다. 수락한 의뢰는 밤에 용사에게 요청할 수 있습니다.", labelStyle);
    }
}
