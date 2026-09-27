using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    private sealed class BattleRequest
    {
        public string title, description;
        public int recommendedLevel, reward, acceptedDay = -1;
        public bool prepared;
        public readonly List<int> party = new List<int>();
        public BattleRequest(string title, string description, int level, int reward)
        { this.title = title; this.description = description; recommendedLevel = level; this.reward = reward; }
    }

    private readonly BattleRequest[] battleRequests =
    {
        new BattleRequest("숲길의 고블린", "상인들의 길을 막은 고블린 무리를 토벌해 주세요.\n목표: 숲길의 적을 처치하고 교역로 확보", 2, 60),
        new BattleRequest("폐광의 수상한 소리", "폐광에 둥지를 튼 마물을 몰아내야 합니다.\n목표: 폐광 내부를 조사하고 마물 토벌", 3, 90),
        new BattleRequest("농장을 습격한 늑대", "밤마다 가축을 노리는 늑대 무리가 나타납니다.\n목표: 농장 주변의 늑대 무리 격퇴", 2, 50)
    };
    private bool boardBattleTab;
    private bool battlePreparationOpen;
    private int selectedBattleRequest = -1;
    private int selectedPreparationCategory;
    private int selectedCraftItem = -1;
    private Vector2 craftScrollPosition;
    private string battleNotice = "";

    private void DrawBattleRequestBoard()
    {
        GUI.Label(new Rect(68, 402, 1768, 42), "전투 의뢰 · 수락 후 밤에 바 안쪽 길드장을 눌러 소속 용사 최대 4명을 편성하세요.", labelStyle);
        bool activeRequestExists = false;
        foreach (BattleRequest accepted in battleRequests)
            if (accepted.acceptedDay >= 0) { activeRequestExists = true; break; }
        int row = 0;
        foreach (BattleRequest request in battleRequests)
        {
            float y = 458 + row++ * 126;
            GUI.Box(new Rect(68, y, 1768, 110), "", panelStyle);
            GUI.Label(new Rect(90, y + 8, 1200, 42), request.title + " · 권장 레벨 " + request.recommendedLevel + " · 보수 " + request.reward + " G", headingStyle);
            GUI.Label(new Rect(90, y + 54, 1200, 44), request.description.Split('\n')[0], labelStyle);
            GUI.enabled = request.acceptedDay < 0 && !activeRequestExists;
            string buttonText = request.acceptedDay >= 0 ? "수락 완료 · 진행 중" : activeRequestExists ? "다른 전투 의뢰 진행 중" : "수락";
            if (GUI.Button(new Rect(1408, y + 26, 404, 58), buttonText, buttonStyle))
            {
                if (request.acceptedDay < 0)
                {
                    request.acceptedDay = day;
                    activeRequestExists = true;
                    dayNotice = request.title + " 수락 완료. 밤에 길드장을 눌러 전투를 준비하세요.";
                }
            }
            GUI.enabled = true;
        }
        if (activeRequestExists)
            GUI.Label(new Rect(90, 842, 1600, 48), "전투 의뢰는 한 번에 하나만 수락할 수 있습니다. 진행 중인 의뢰를 마치면 다른 의뢰를 받을 수 있습니다.", labelStyle);
        if (row == 0) GUI.Label(new Rect(90, 470, 1600, 80), "게시판에 새로운 전투 의뢰가 없습니다. 수락한 의뢰는 길드장이 보관합니다.", labelStyle);
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
        GUI.Box(tag, "제작 · 전투 의뢰 준비", buttonStyle);
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            battlePreparationOpen = true;
            battleNotice = "";
            selectedPreparationCategory = 0;
            selectedCraftItem = -1;
            selectedBattleRequest = -1;
            for (int i = 0; i < battleRequests.Length; i++)
                if (battleRequests[i].acceptedDay >= 0) { selectedBattleRequest = i; break; }
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
        GUI.Label(new Rect(208, 126, 1200, 64), "길드 작업대", headingStyle);
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

        string[] categories = { "음식 제작", "무기 제작", "방어구 제작", "전투 의뢰 준비" };
        for (int i = 0; i < categories.Length; i++)
        {
            int category = i;
            if (GUI.Button(new Rect(208 + i * 382, 204, 360, 62), categories[i], selectedPreparationCategory == i ? selectedTabStyle : tabStyle))
            {
                selectedPreparationCategory = category;
                selectedCraftItem = -1;
            }
        }
        if (selectedPreparationCategory < 3)
        {
            DrawProductionCategory();
            return;
        }
        DrawBattlePartyCategory();
    }

    private void DrawProductionCategory()
    {
        Rect materialView = new Rect(208, 286, 1504, 80);
        craftMaterialScrollPosition = GUI.BeginScrollView(materialView, craftMaterialScrollPosition, new Rect(0, 0, materialNames.Length * 250, 80), true, false);
        for (int i = 0; i < materialNames.Length; i++)
        {
            float x = i * 250;
            GUI.Box(new Rect(x, 0, 236, 68), "", panelStyle);
            DrawMaterialIcon(new Rect(x + 6, 5, 58, 58), i);
            GUI.Label(new Rect(x + 70, 7, 158, 28), materialNames[i], labelStyle);
            GUI.Label(new Rect(x + 70, 35, 158, 27), "보유 " + materials[i] + "개", headingStyle);
        }
        GUI.EndScrollView();

        ShopItem[] items = inventory[selectedPreparationCategory];
        Rect view = new Rect(190, 386, 1540, 504);
        craftScrollPosition = GUI.BeginScrollView(view, craftScrollPosition, new Rect(0, 0, 1530, 574), false, true);
        for (int i = 0; i < items.Length; i++)
        {
            float x = 18 + (i % 3) * 510, y = 12 + (i / 3) * 184;
            Rect card = new Rect(x, y, 478, 170);
            GUI.Box(card, "", cardStyle);
            DrawPixelItemIcon(new Rect(x + 8, y + 15, 146, 140), items[i].icon);
            GUI.Label(new Rect(x + 162, y + 25, 302, 56), items[i].name, headingStyle);
            string tier = i < 3 ? "저가" : i < 6 ? "중가" : "고가";
            GUI.Label(new Rect(x + 162, y + 93, 302, 48), tier + " · " + items[i].price + " G · 재고 " + items[i].stock + "개", labelStyle);
            if (GUI.Button(card, GUIContent.none, hoverStyle)) selectedCraftItem = i;
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
        int[] cost = recipes[category * 9 + itemIndex];
        PixelRect(new Rect(160, 100, 1600, 890), new Color(.01f, .015f, .03f, .88f));
        Rect detail = new Rect(390, 300, 1140, 500);
        GUI.Box(detail, "", panelStyle);
        GUI.Label(new Rect(444, 328, 920, 60), item.name + " 제작", headingStyle);
        if (GUI.Button(new Rect(1430, 318, 58, 58), "×", buttonStyle)) { selectedCraftItem = -1; return; }
        GUI.Label(new Rect(444, 402, 1000, 48), "필요 재료 · 필요 수량 / 현재 보유 수량", labelStyle);
        bool canCraft = true;
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
        GUI.Label(new Rect(444, 630, 1000, 44), "제작하면 재료를 사용하고 판매 재고가 1개 늘어납니다.", labelStyle);
        GUI.enabled = canCraft;
        if (GUI.Button(new Rect(700, 700, 270, 66), canCraft ? "제작하기" : "재료가 부족합니다", buttonStyle))
        {
            for (int i = 0; i < cost.Length; i++) materials[i] -= cost[i];
            item.stock++;
            dayNotice = item.name + " 제작 완료. 판매 재고가 1개 늘었습니다.";
            selectedCraftItem = -1;
        }
        GUI.enabled = true;
        if (GUI.Button(new Rect(1010, 700, 230, 66), "취소", tabStyle)) selectedCraftItem = -1;
    }

    private void DrawBattlePartyCategory()
    {
        int row = 0;
        for (int i = 0; i < battleRequests.Length; i++)
        {
            BattleRequest candidate = battleRequests[i];
            if (candidate.acceptedDay < 0) continue;
            if (GUI.Button(new Rect(208, 286 + row++ * 110, 428, 88), candidate.title + (candidate.prepared ? "\n편성 완료" : "\n편성 대기"), i == selectedBattleRequest ? selectedTabStyle : tabStyle))
            { selectedBattleRequest = i; battleNotice = ""; }
        }
        if (selectedBattleRequest < 0 || selectedBattleRequest >= battleRequests.Length)
        {
            GUI.Label(new Rect(700, 300, 980, 150), "수락한 전투 의뢰가 없습니다.\n낮에 마을의 의뢰 게시판에서 전투 의뢰를 수락하세요.", headingStyle);
            return;
        }

        BattleRequest request = battleRequests[selectedBattleRequest];
        request.party.RemoveAll(index => index < 0 || index >= guests.Count || !guests[index].guildMember);
        GUI.Label(new Rect(700, 286, 990, 52), request.title, headingStyle);
        GUI.Label(new Rect(700, 342, 990, 54), "권장 레벨 " + request.recommendedLevel + " · 성공 보수 " + request.reward + " G · 선택 " + request.party.Count + "/4명", labelStyle);
        GUI.Label(new Rect(700, 402, 990, 48), request.description, labelStyle);
        int memberRow = 0;
        for (int i = 0; i < guests.Count; i++)
        {
            Guest member = guests[i];
            if (!member.guildMember) continue;
            float x = 700 + memberRow % 2 * 494, y = 474 + memberRow / 2 * 126;
            memberRow++;
            bool chosen = request.party.Contains(i);
            GUI.Box(new Rect(x, y, 470, 112), "", chosen ? selectedTabStyle : panelStyle);
            DrawGuestPortrait(new Rect(x + 8, y + 8, 90, 96), i);
            GUI.Label(new Rect(x + 112, y + 8, 340, 42), member.name + " · Lv." + member.level, headingStyle);
            GUI.Label(new Rect(x + 112, y + 53, 340, 44), member.role + (chosen ? " · 선택됨" : ""), labelStyle);
            GUI.enabled = chosen || request.party.Count < 4;
            if (GUI.Button(new Rect(x, y, 470, 112), GUIContent.none, hoverStyle))
            {
                if (chosen) request.party.Remove(i);
                else if (request.party.Count < 4) request.party.Add(i);
                request.prepared = false;
                battleNotice = "편성이 변경되었습니다. 준비 완료를 눌러 확정하세요.";
            }
            GUI.enabled = true;
        }
        if (memberRow == 0) GUI.Label(new Rect(700, 474, 980, 70), "먼저 용사의 호감도를 3까지 올려 길드원으로 영입하세요.", labelStyle);
        GUI.Label(new Rect(700, 846, 990, 54), request.party.Count == 4 ? "최대 4명입니다. 선택한 용사를 해제하면 교체할 수 있어요." : "함께 갈 길드원을 선택하세요. 다시 누르면 선택이 해제됩니다.", labelStyle);
        GUI.enabled = request.party.Count > 0 && request.party.Count <= 4;
        if (GUI.Button(new Rect(1324, 908, 364, 58), request.prepared ? "편성 저장 완료" : "준비 완료", buttonStyle))
        {
            request.prepared = true;
            battleNotice = "의뢰에 선택한 " + request.party.Count + "명의 편성을 저장했습니다.";
        }
        GUI.enabled = true;
        GUI.Label(new Rect(208, 908, 1090, 58), battleNotice, labelStyle);
    }
}
