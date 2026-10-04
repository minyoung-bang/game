using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    private sealed class BattleRequest
    {
        public string title, description, region;
        public int unlockGuildLevel, recommendedLevel, partySize, waves, reward, guildExperience, heroExperience, expense, acceptedDay = -1;
        public bool prepared, persistentRegion;
        public readonly string[] enemyKinds;
        public readonly List<int> party = new List<int>();
        public BattleRequest(string title, string description, int unlock, int level, int partySize, int waves, int reward, int guildExperience, int heroExperience, int expense, string region = "", bool persistentRegion = true, params string[] enemyKinds)
        {
            this.title = title; this.description = description; this.region = region; this.persistentRegion = persistentRegion;
            this.enemyKinds = enemyKinds ?? new string[0];
            unlockGuildLevel = unlock; recommendedLevel = level;
            this.partySize = partySize; this.waves = waves; this.reward = reward; this.guildExperience = guildExperience;
            this.heroExperience = heroExperience; this.expense = expense;
        }
    }

    private readonly BattleRequest[] battleRequests =
    {
        new BattleRequest("깊은 숲의 슬라임 퇴치", "깊은 숲의 길목을 막은 슬라임 무리를 처치해 탐사로를 확보하세요.", 1, 1, 2, 1, 60, 25, 49, 0, "깊은 숲", true, "숲 슬라임"),
        new BattleRequest("돌아오지 않는 조사대", "조사대의 흔적을 찾고 이동로를 막은 마물을 처치하세요.", 2, 3, 2, 2, 90, 35, 62, 0, "안개 늪", true, "독두꺼비", "늪 괴물"),
        new BattleRequest("잠들지 못한 수호자", "조사대를 공격하는 유적 수호자를 잠재우세요.", 3, 5, 3, 2, 125, 50, 78, 0, "고대 폐허", true, "망령", "해골 병사", "유적 수호자"),
        new BattleRequest("끊긴 협곡길", "길목을 점거한 하피 무리를 몰아내 통행로를 다시 여세요.", 4, 7, 3, 2, 165, 65, 96, 0, "바람의 협곡", true, "하피", "암석 도마뱀"),
        new BattleRequest("꺼지지 않는 탑의 빛", "폐탑의 마력 누출 원인을 조사하고 폭주한 장치를 멈추세요.", 5, 9, 3, 3, 210, 80, 116, 0, "폐탑", true, "마도 인형", "폭주한 마력체"),
        new BattleRequest("눈 속의 구조 신호", "실종된 운송대의 신호를 따라가 생존자를 확보하세요.", 6, 11, 4, 3, 260, 100, 140, 0, "얼어붙은 고개", true, "서리 정령", "눈 골렘"),
        new BattleRequest("변경의 봉화", "봉화가 켜지기 전에 정찰대를 저지하고 요새의 위협을 확인하세요.", 8, 15, 4, 3, 360, 140, 195, 0, "마왕군 변경 요새", true, "마왕군 정찰병", "마왕군 지휘관")
    };
    private bool boardBattleTab;
    private bool battlePreparationOpen;
    private int selectedPreparationCategory;
    private int selectedCraftItem = -1;
    private Vector2 craftScrollPosition;

    private void DrawBattleRequestBoard()
    {
        GUI.Label(new Rect(68, 402, 1768, 42), "전투 의뢰 · 수락 후 저녁에 지도를 열어 지역을 선택하고 길드원 1~4명을 편성하세요.", labelStyle);
        bool activeRequestExists = false;
        foreach (BattleRequest accepted in battleRequests)
            if (accepted.acceptedDay >= 0) { activeRequestExists = true; break; }
        Rect requestView = new Rect(68, 458, 1768, 428);
        battleBoardScrollPosition = GUI.BeginScrollView(requestView, battleBoardScrollPosition,
            new Rect(0, 0, 1744, Mathf.Max(428, battleRequests.Length * 126)), false, true);
        int row = 0;
        foreach (BattleRequest request in battleRequests)
        {
            float y = row++ * 126;
            GUI.Box(new Rect(0, y, 1740, 110), "", panelStyle);
            GUI.Label(new Rect(22, y + 6, 1290, 42), request.title + " · 권장 Lv." + request.recommendedLevel + " · 보상 " + request.reward + " G · 길드 Lv." + request.unlockGuildLevel, headingStyle);
            GUI.Label(new Rect(22, y + 53, 1290, 46), request.description + " (" + request.waves + "전투 · 준비금 " + request.expense + " G)", labelStyle);
            bool unlocked = guildLevel >= request.unlockGuildLevel;
            GUI.enabled = unlocked && request.acceptedDay < 0 && !activeRequestExists;
            string buttonText = !unlocked ? "길드 Lv." + request.unlockGuildLevel + " 해금" : request.acceptedDay >= 0 ? "수락 완료 · 진행 중" : activeRequestExists ? "다른 전투 의뢰 진행 중" : "수락";
            if (GUI.Button(new Rect(1340, y + 26, 390, 58), buttonText, buttonStyle))
            {
                if (request.acceptedDay < 0)
                {
                    request.acceptedDay = day;
                    activeRequestExists = true;
                    if (request.persistentRegion && !string.IsNullOrEmpty(request.region)) unlockedWorldRegions.Add(request.region);
                    dayNotice = request.title + " 수락 완료. 저녁에 지도에서 " + request.region + "을(를) 선택하세요.";
                }
            }
            GUI.enabled = true;
        }
        GUI.EndScrollView();
        if (activeRequestExists)
            GUI.Label(new Rect(90, 842, 1600, 48), "전투 의뢰는 한 번에 하나만 수락할 수 있습니다. 진행 중인 의뢰를 마치면 다른 의뢰를 받을 수 있습니다.", labelStyle);
        if (row == 0) GUI.Label(new Rect(90, 470, 1600, 80), "게시판에 새로운 전투 의뢰가 없습니다.", labelStyle);
    }

    private bool DrawGuildmasterHotspot()
    {
        bool hovered = presentation.HitTestGuildmaster(Input.mousePosition);
        presentation.SetGuildmasterHovered(hovered);
        if (!hovered) return false;
        Rect screen = presentation.GuildmasterScreenRect();
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        float ox = (Screen.width - 1920f * scale) * .5f;
        float oy = (Screen.height - 1080f * scale) * .5f;
        Rect tag = new Rect((screen.center.x - ox) / scale - 176, (screen.y - oy) / scale - 52, 352, 46);
        GUI.Box(tag, "아이템 제작", buttonStyle);
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            battlePreparationOpen = true;
            selectedPreparationCategory = 0;
            selectedCraftItem = -1;
            presentation.SetHovered(-1);
            presentation.SetGuildmasterHovered(false);
            Event.current.Use();
            return true;
        }
        return false;
    }

    private void DrawBattlePreparation()
    {
        PixelRect(new Rect(0, 0, 1920, 1080), new Color(.015f, .02f, .04f, .83f));
        GUI.Box(new Rect(160, 100, 1600, 890), "", panelStyle);
        GUI.Label(new Rect(208, 126, 1200, 64), "길드 작업대 · 도적 장비 제작 가능", headingStyle);
        if (GUI.Button(new Rect(1670, 122, 58, 58), "×", buttonStyle) ||
            (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
        {
            if (selectedCraftItem >= 0) { selectedCraftItem = -1; Event.current.Use(); return; }
            battlePreparationOpen = false;
            Event.current.Use();
            return;
        }

        if (selectedCraftItem >= 0)
        {
            DrawCraftConfirmation();
            return;
        }

        string[] categories = { "음식 제작", "무기 제작", "방어구 제작" };
        for (int i = 0; i < categories.Length; i++)
        {
            int category = i;
            if (GUI.Button(new Rect(208 + i * 510, 204, 488, 62), categories[i], selectedPreparationCategory == i ? selectedTabStyle : tabStyle))
            {
                selectedPreparationCategory = category;
                selectedCraftItem = -1;
            }
        }
        DrawProductionCategory();
    }

    private void DrawProductionCategory()
    {
        Rect materialView = new Rect(208, 286, 1504, 80);
        var usedMaterials = new List<int>();
        int recipeOffset = 0;
        for (int c = 0; c < selectedPreparationCategory; c++) recipeOffset += inventory[c].Length;
        for (int m = 0; m < materialNames.Length; m++)
            for (int item = 0; item < inventory[selectedPreparationCategory].Length; item++)
                if (recipes[recipeOffset + item][m] > 0) { usedMaterials.Add(m); break; }
        craftMaterialScrollPosition = GUI.BeginScrollView(materialView, craftMaterialScrollPosition, new Rect(0, 0, usedMaterials.Count * 250, 68), true, false);
        for (int slot = 0; slot < usedMaterials.Count; slot++)
        {
            int i = usedMaterials[slot];
            float x = slot * 250;
            GUI.Box(new Rect(x, 0, 236, 68), "", panelStyle);
            DrawMaterialIcon(new Rect(x + 6, 5, 58, 58), i);
            GUI.Label(new Rect(x + 70, 7, 158, 28), guildLevel >= materialUnlockLevels[i] ? materialNames[i] : "미해금 재료", labelStyle);
            GUI.Label(new Rect(x + 70, 35, 158, 27), "보유 " + materials[i] + "개", headingStyle);
        }
        GUI.EndScrollView();

        ShopItem[] items = inventory[selectedPreparationCategory];
        Rect view = new Rect(190, 386, 1540, 504);
        const float columnStep = 378f;
        const float rowStep = 180f;
        int rowCount = Mathf.CeilToInt(items.Length / 4f);
        craftScrollPosition = GUI.BeginScrollView(view, craftScrollPosition, new Rect(0, 0, 1530, Mathf.Max(504, 18 + rowCount * rowStep)), false, true);
        for (int i = 0; i < items.Length; i++)
        {
            float x = 10 + (i % 4) * columnStep, y = 8 + (i / 4) * rowStep;
            Rect card = new Rect(x, y, 362, 166);
            GUI.Box(card, "", cardStyle);
            DrawPixelItemIcon(new Rect(x + 8, y + 22, 104, 120), items[i].icon);
            GUI.Label(new Rect(x + 120, y + 20, 232, 62), items[i].name, headingStyle);
            string tier = items[i].productionCost == 0 ? "저가" : items[i].productionCost == 1 ? "중가" : "고가";
            string profession = selectedPreparationCategory == 0 ? "" : items[i].allowedJob + " · ";
            string availability = guildLevel >= items[i].unlockGuildLevel ? "재고 " + items[i].stock + "개" : "길드 Lv." + items[i].unlockGuildLevel + " 해금";
            GUI.Label(new Rect(x + 120, y + 88, 232, 54), profession + tier + " · " + items[i].price + " G\n" + availability, labelStyle);
            GUI.enabled = guildLevel >= items[i].unlockGuildLevel;
            if (GUI.Button(card, GUIContent.none, hoverStyle)) selectedCraftItem = i;
            GUI.enabled = true;
        }
        GUI.EndScrollView();
        GUI.Label(new Rect(208, 900, 1480, 44), "아이템을 선택하면 필요한 재료와 보유량을 확인한 뒤 제작할 수 있습니다.", labelStyle);
    }

    private void DrawCraftConfirmation()
    {
        if (selectedPreparationCategory < 0 || selectedPreparationCategory > 2 ||
            selectedCraftItem < 0 || selectedCraftItem >= inventory[selectedPreparationCategory].Length)
        { selectedCraftItem = -1; return; }

        int category = selectedPreparationCategory;
        int itemIndex = selectedCraftItem;
        ShopItem item = inventory[category][itemIndex];
        int recipeIndex = itemIndex;
        for (int c = 0; c < category; c++) recipeIndex += inventory[c].Length;
        int[] cost = recipes[recipeIndex];
        PixelRect(new Rect(160, 100, 1600, 890), new Color(.01f, .015f, .03f, .88f));
        Rect detail = new Rect(390, 300, 1140, 500);
        GUI.Box(detail, "", panelStyle);
        GUI.Label(new Rect(444, 328, 920, 60), item.name + " 제작", headingStyle);
        if (GUI.Button(new Rect(1430, 318, 58, 58), "×", buttonStyle)) { selectedCraftItem = -1; return; }
        GUI.Label(new Rect(444, 402, 1000, 48), "필요 재료 · 필요 수량 / 현재 보유 수량", labelStyle);
        bool canCraft = guildLevel >= item.unlockGuildLevel && guildGold >= item.productionCost;
        int line = 0;
        for (int i = 0; i < cost.Length; i++)
        {
            if (cost[i] <= 0) continue;
            bool enough = materials[i] >= cost[i];
            canCraft &= enough;
            string status = materialNames[i] + "   " + cost[i] + "개 필요 / " + materials[i] + "개 보유";
            GUI.Label(new Rect(470 + line % 2 * 500, 468 + line / 2 * 58, 470, 48), status, headingStyle);
            line++;
        }
        GUI.Label(new Rect(444, 630, 1000, 44), "제작 비용 " + item.productionCost + " G · 제작하면 재료와 골드를 사용하고 판매 재고가 1개 늘어납니다.", labelStyle);
        GUI.enabled = canCraft;
        if (GUI.Button(new Rect(700, 700, 270, 66), canCraft ? "제작하기" : "재료가 부족합니다", buttonStyle))
        {
            for (int i = 0; i < cost.Length; i++) materials[i] -= cost[i];
            guildGold -= item.productionCost;
            item.stock++;
            dayNotice = item.name + " 제작 완료. 판매 재고가 1개 늘었습니다.";
            selectedCraftItem = -1;
        }
        GUI.enabled = true;
        if (GUI.Button(new Rect(1010, 700, 230, 66), "취소", tabStyle)) selectedCraftItem = -1;
    }

    private Vector2 battleBoardScrollPosition;
}
