using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime : MonoBehaviour
{
    [Serializable]
    private sealed class Guest
    {
        public string name;
        public string role;
        public int level;
        public int gold;
        public int affinity;
        public bool guildMember;
        public int experience;
        public int trainingExperience;
        public int skillIndex;

        public Guest(string name, string role, int level, int gold, int affinity, bool guildMember)
        {
            this.name = name; this.role = role; this.level = level; this.gold = gold;
            this.affinity = affinity; this.guildMember = guildMember;
        }
    }

    private sealed class ShopItem
    {
        public string name;
        public int price;
        public int icon;
        public int stock = 2;
        public ShopItem(string name, int price, int icon) { this.name = name; this.price = price; this.icon = icon; }
    }

    private sealed class Quest
    {
        public string name, detail;
        public int reward, difficulty;
        public bool collected;
        public int acceptedDay = -1;
        public int expiresOnDay = -1;
        public int deadlineDays = 1;
        public bool completed;
        public bool expired;
        public Quest(string name, string detail, int reward, int difficulty)
        { this.name = name; this.detail = detail; this.reward = reward; this.difficulty = difficulty; }
    }

    private readonly List<Guest> guests = new List<Guest>
    {
        new Guest("루나", "견습 마도사", 2, 18, 2, false), new Guest("브란", "신참 검사", 2, 24, 3, true),
        new Guest("세라", "숲의 정찰자", 3, 31, 1, false), new Guest("에단", "방랑 기사", 3, 35, 3, true),
        new Guest("미라", "약초 수집가", 2, 16, 2, false), new Guest("노아", "여행 상인", 4, 46, 1, false),
    };

    private readonly ShopItem[][] inventory =
    {
        new[] {
            new ShopItem("꿀 바른 빵", 8, 0), new ShopItem("산딸기 수프", 10, 1), new ShopItem("구운 고기", 14, 2),
            new ShopItem("허브 치킨", 24, 9), new ShopItem("버섯 파이", 28, 10), new ShopItem("훈제 연어", 32, 11),
            new ShopItem("왕실 만찬", 55, 18), new ShopItem("마력열매 타르트", 62, 19), new ShopItem("용고기 스테이크", 80, 20)
        },
        new[] {
            new ShopItem("견습자의 검", 20, 3), new ShopItem("사냥꾼의 활", 28, 4), new ShopItem("참나무 지팡이", 24, 5),
            new ShopItem("강철 장검", 55, 12), new ShopItem("장인의 장궁", 68, 13), new ShopItem("마도사의 지팡이", 62, 14),
            new ShopItem("왕가의 검", 120, 21), new ShopItem("용사냥 장궁", 145, 22), new ShopItem("대마법사의 지팡이", 135, 23)
        },
        new[] {
            new ShopItem("가죽 조끼", 18, 6), new ShopItem("작은 방패", 22, 7), new ShopItem("여행자 망토", 16, 8),
            new ShopItem("강화 가죽갑옷", 45, 15), new ShopItem("수호자의 방패", 52, 16), new ShopItem("마법 망토", 48, 17),
            new ShopItem("용비늘 갑옷", 110, 24), new ShopItem("왕실 수호 방패", 125, 25), new ShopItem("별빛 로브", 118, 26)
        },
    };

    private readonly Quest[] quests =
    {
        new Quest("약초 바구니 수집", "마을 북쪽 들판에서 약초를 모아 약제상에게 전달합니다. 방문 용사도 수행할 수 있는 간단 의뢰입니다.", 12, 1),
        new Quest("상인에게 편지 전달", "동쪽 길목의 상인에게 길드장의 편지를 전달합니다. 서두르면 해 질 무렵 돌아올 수 있습니다.", 16, 2),
        new Quest("숲길 순찰", "숲 입구를 살피고 위험한 흔적이 없는지 확인합니다. 조금 까다롭지만 보수가 좋습니다.", 22, 3),
    };

    private readonly string[] dialogue =
    {
        "이 길드의 빵, 냄새부터 정말 좋네요!", "좋은 의뢰가 있으면 언제든 맡겨 줘.",
        "숲에서 돌아오는 길이에요. 잠깐 쉬어도 될까요?", "실력으로 보답하겠습니다. 맡겨만 주십시오.",
        "약초를 구하러 가기 전에 배부터 채워야겠어요.", "물건도 의뢰도, 오늘은 좋은 거래를 해보죠!"
    };
    private int guildGold = 42;
    private int guildExperience;
    private int guildLevel = 1;
    private int totalSales;
    private int successfulRequests;
    private int selectedGuest = -1;
    private int selectedCategory;
    private int selectedQuest = -1;
    private int confirmItem = -1;
    private int hoveredGuest = -1;
    private string notice = "";
    private Texture2D[] guestPortraitTextures;
    private GuildHallPresentation presentation;
    private Font koreanFont;
    private GUIStyle panelStyle, cardStyle, labelStyle, headingStyle, buttonStyle, tabStyle, selectedTabStyle, itemStyle, hoverStyle;
    private bool stylesReady;
    private Vector2 shopScrollPosition;
    private Vector2 craftMaterialScrollPosition;
    private Color itemIconTint = Color.white;

    private void Start()
    {
        LoadInterfaceSettings();
        for (int i = 0; i < dailySupply.Length; i++)
            dailySupply[i] = UnityEngine.Random.Range(4, 8);
        foreach (Quest quest in quests)
            quest.deadlineDays = UnityEngine.Random.Range(1, 4);
        presentation = gameObject.AddComponent<GuildHallPresentation>();
        presentation.Initialize(GetComponent<Camera>());
        guestPortraitTextures = presentation.Portraits;
        presentation.Reseat(UnityEngine.Random.Range(1, int.MaxValue), SelectEveningRoster());
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (presentation == null || !presentation.Ready) return;
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        if (scale <= 0) return;
        float ox = (Screen.width - 1920f * scale) * .5f;
        float oy = (Screen.height - 1080f * scale) * .5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));

        if (battlePreparationOpen)
        {
            presentation.SetHovered(-1);
            presentation.SetGuildmasterHovered(false);
            DrawBattlePreparation();
            GUI.matrix = Matrix4x4.identity;
            return;
        }

        if (isDaytime)
        {
            DrawDaytime();
            GUI.matrix = Matrix4x4.identity;
            return;
        }

        // Kept safely inside the viewport with extra top/left padding.
        GUI.Box(new Rect(34, 34, 392, 128), "", panelStyle);
        GUI.Label(new Rect(62, 48, 336, 48), "DAY " + day.ToString("00") + "  ·  저녁 영업", headingStyle);
        GUI.Label(new Rect(62, 97, 336, 42), "금고  " + guildGold + " G", labelStyle);
        if (!IsModalOpen())
        {
            DrawManagementToolbar();
            if (managementPage != 0)
            {
                presentation.SetHovered(-1);
                presentation.SetGuildmasterHovered(false);
                DrawManagementPage();
                GUI.matrix = Matrix4x4.identity;
                return;
            }
        }
        if (!IsModalOpen() && GUI.Button(new Rect(1590, 964, 296, 76), "하루 종료  →", buttonStyle))
        {
            BeginNextDay();
            GUI.matrix = Matrix4x4.identity;
            return;
        }
        Vector2 virtualMouse = new Vector2((Input.mousePosition.x - ox) / scale,
            (Screen.height - Input.mousePosition.y - oy) / scale);
        if (!IsModalOpen() && DrawGuildmasterHotspot())
        {
            GUI.matrix = Matrix4x4.identity;
            return;
        }
        if (IsModalOpen()) presentation.SetGuildmasterHovered(false);
        hoveredGuest = -1;
        if (!IsModalOpen()) DrawGuestHotspots(virtualMouse);
        else presentation.SetHovered(-1);
        if (IsModalOpen()) DrawGuestPopup();
        GUI.matrix = Matrix4x4.identity;
    }

    private bool IsModalOpen() => selectedGuest >= 0 && selectedGuest < guests.Count;

    private Rect GuestRect(int index)
    {
        Rect r = presentation.ScreenRect(index);
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        float ox = (Screen.width - 1920f * scale) / 2;
        float oy = (Screen.height - 1080f * scale) / 2;
        return new Rect((r.x-ox)/scale,(r.y-oy)/scale,r.width/scale,r.height/scale);
    }

    private void DrawGuestHotspots(Vector2 virtualMouse)
    {
        Event e = Event.current;
        hoveredGuest = presentation.HitTest(Input.mousePosition);
        presentation.SetHovered(hoveredGuest);
        if (hoveredGuest >= 0)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                selectedGuest = hoveredGuest;
                selectedCategory = 0;
                selectedQuest = -1;
                confirmItem = -1;
                notice = "";
                e.Use();
            }
        }
        if (hoveredGuest >= 0)
        {
            Rect r = GuestRect(hoveredGuest);
            float lift = Mathf.Sin(Time.unscaledTime * 4f) * 4f;
            Rect tag = new Rect(r.center.x - 104, r.y - 44 + lift, 208, 38);
            GUI.Box(tag, "", panelStyle);
            GUI.Label(tag, guests[hoveredGuest].name, headingStyle);
        }
    }

    private void DrawGuestPopup()
    {
        if (selectedGuest < 0 || selectedGuest >= guests.Count) return;
        Guest guest = guests[selectedGuest];
        string guestDialogue = selectedGuest < dialogue.Length ? dialogue[selectedGuest] : "오늘은 잠시 쉬어가고 싶어요.";
        GUI.color = new Color(0, 0, 0, .68f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Rect window = new Rect(170, 78, 1580, 924);
        GUI.Box(window, "", panelStyle);
        GUI.Label(new Rect(218, 100, 480, 52), "길드에 찾아온 용사", labelStyle);
        // A sale confirmation is a true modal: do not draw (and therefore do not
        // leave clickable) category, inventory, quest, or close controls beneath it.
        if (confirmItem >= 0)
        {
            DrawSaleConfirmation();
            return;
        }
        if (GUI.Button(new Rect(1680, 92, 42, 42), "×", buttonStyle)) { ClosePopup(); return; }

        Rect portraitPanel = new Rect(214, 164, 420, 760);
        GUI.Box(portraitPanel, "", panelStyle);
        Rect portrait = new Rect(242, 188, 364, 466);
        DrawGuestPortrait(portrait, selectedGuest);
        GUI.Label(new Rect(244, 666, 360, 42), guest.name + "  ·  " + guest.role, headingStyle);
        GUI.Label(new Rect(244, 710, 360, 38), "레벨 " + guest.level + "   " + (guest.guildMember ? "길드 소속" : "방문 용사"), labelStyle);
        GUI.Box(new Rect(238, 766, 372, 126), "", panelStyle);
        GUI.Label(new Rect(256, 780, 338, 96), "“" + guestDialogue + "”", labelStyle);

        Rect right = new Rect(664, 164, 1040, 760);
        GUI.Box(right, "", panelStyle);
        GUI.Label(new Rect(704, 186, 900, 48), "무엇을 도와드릴까요?", headingStyle);
        GUI.Label(new Rect(704, 232, 900, 34), "보유 골드  " + guest.gold + " G     호감도  " + guest.affinity + "/5     길드 금고  " + guildGold + " G", labelStyle);

        string[] tabs = { "음식", "무기", "방어구", "의뢰 요청" };
        for (int i = 0; i < tabs.Length; i++)
        {
            Rect tab = new Rect(700 + i * 228, 284, 216, 58);
            if (GUI.Button(tab, tabs[i], selectedCategory == i ? selectedTabStyle : tabStyle))
            {
                selectedCategory = i;
                selectedQuest = -1;
                confirmItem = -1;
            }
        }

        if (selectedQuest >= 0) DrawQuestDetail();
        else if (selectedCategory == 3) DrawQuestList();
        else DrawItemGrid();

        if (!string.IsNullOrEmpty(notice)) GUI.Label(new Rect(700, 864, 980, 38), notice, labelStyle);
    }

    private void DrawGuestPortrait(Rect rect, int index)
    {
        GUI.Box(rect, "", panelStyle);
        if (guestPortraitTextures == null || index < 0 || index >= guestPortraitTextures.Length) return;
        Texture2D portraitTexture = guestPortraitTextures[index];
        if (portraitTexture == null) return;
        float aspect = (float)portraitTexture.width / portraitTexture.height;
        float maxW = rect.width - 24, maxH = rect.height - 24;
        float drawW = Mathf.Min(maxW, maxH * aspect);
        float drawH = drawW / aspect;
        Rect destination = new Rect(rect.center.x - drawW * .5f, rect.center.y - drawH * .5f, drawW, drawH);
        GUI.DrawTexture(destination, portraitTexture, ScaleMode.ScaleToFit, true);
    }

    private void DrawItemGrid()
    {
        ShopItem[] items = inventory[selectedCategory];
        Rect view = new Rect(694, 354, 980, 492);
        shopScrollPosition = GUI.BeginScrollView(view, shopScrollPosition, new Rect(0, 0, 962, 486), false, true);
        for (int i = 0; i < items.Length; i++)
        {
            float x = (i % 3) * 320, y = (i / 3) * 158;
            Rect card = new Rect(x, y, 302, 150);
            GUI.Box(card, "", cardStyle);
            DrawPixelItemIcon(new Rect(card.x + 8, card.y + 14, 104, 108), items[i].icon);
            GUI.Label(new Rect(card.x + 120, card.y + 24, 174, 58), items[i].name, headingStyle);
            string tier = i < 3 ? "저가" : i < 6 ? "중가" : "고가";
            GUI.Label(new Rect(card.x + 120, card.y + 88, 174, 44), tier + " · " + items[i].price + " G · 재고 " + items[i].stock, labelStyle);
            GUI.enabled = items[i].stock > 0;
            if (GUI.Button(card, GUIContent.none, hoverStyle)) confirmItem = i;
            GUI.enabled = true;
        }
        GUI.EndScrollView();
    }

    private void DrawPixelItemIcon(Rect r, int type)
    {
        int tier = type / 9;
        type %= 9;
        Color previousTint = itemIconTint;
        itemIconTint = tier == 0 ? Color.white : tier == 1 ? new Color(.88f, 1f, .94f) : new Color(1f, .86f, .63f);
        GUI.Box(r, "", panelStyle);
        float cx = r.center.x, cy = r.center.y;
        float scale = Mathf.Min((r.width - 12) / 176f, (r.height - 12) / 176f);
        switch (type)
        {
            case 0: // loaf
                ItemPixelRect(cx, cy, -76, -24, 152, 92, scale, new Color(.55f,.28f,.12f));
                ItemPixelRect(cx, cy, -62, -38, 124, 84, scale, new Color(.92f,.68f,.34f));
                ItemPixelRect(cx, cy, -35, -20, 16, 10, scale, new Color(1f,.86f,.57f));
                ItemPixelRect(cx, cy, 17, -5, 16, 10, scale, new Color(1f,.86f,.57f)); break;
            case 1: // soup bowl
                ItemPixelRect(cx, cy, -84, -15, 168, 18, scale, new Color(.85f,.68f,.39f));
                ItemPixelRect(cx, cy, -66, 3, 132, 60, scale, new Color(.58f,.24f,.34f));
                ItemPixelRect(cx, cy, -50, 18, 100, 14, scale, new Color(.91f,.37f,.49f)); break;
            case 2: // meat
                ItemPixelRect(cx, cy, -50, -36, 100, 88, scale, new Color(.65f,.23f,.17f));
                ItemPixelRect(cx, cy, -29, -50, 58, 20, scale, new Color(.93f,.55f,.31f));
                ItemPixelRect(cx, cy, 35, 22, 38, 16, scale, new Color(.93f,.83f,.66f)); break;
            case 3: case 4: case 5: // sword / bow / staff
                ItemPixelRect(cx, cy, -8, -100, 18, 176, scale, new Color(.76f,.83f,.91f));
                ItemPixelRect(cx, cy, -32, 42, 65, 17, scale, new Color(.58f,.32f,.16f));
                ItemPixelRect(cx, cy, -8, 58, 17, 42, scale, new Color(.86f,.66f,.36f)); break;
            case 6: case 7: case 8: // vest / shield / cloak
                ItemPixelRect(cx, cy, -66, -72, 132, 136, scale, new Color(.36f,.25f,.2f));
                ItemPixelRect(cx, cy, -48, -52, 96, 94, scale, new Color(type == 7 ? .45f : .26f, type == 7 ? .53f : .34f, type == 7 ? .72f : .31f));
                ItemPixelRect(cx, cy, -25, -30, 50, 48, scale, new Color(.78f,.62f,.35f)); break;
        }
        itemIconTint = previousTint;
    }

    private void DrawMaterialIcon(Rect r, int materialIndex)
    {
        Color[] colors =
        {
            new Color(.82f,.64f,.28f), new Color(.76f,.18f,.22f), new Color(.62f,.26f,.2f),
            new Color(.48f,.28f,.14f), new Color(.48f,.52f,.58f), new Color(.52f,.34f,.24f),
            new Color(.28f,.62f,.3f), new Color(.57f,.43f,.65f), new Color(.88f,.7f,.48f),
            new Color(.48f,.68f,.3f), new Color(.92f,.85f,.64f), new Color(.88f,.9f,.78f),
            new Color(.72f,.72f,.78f), new Color(.69f,.43f,.27f), new Color(.22f,.24f,.29f),
            new Color(.64f,.68f,.76f), new Color(.37f,.72f,.84f), new Color(.55f,.57f,.6f)
        };
        int index = Mathf.Clamp(materialIndex, 0, colors.Length - 1);
        GUI.Box(r, "", panelStyle);
        float scale = Mathf.Min((r.width - 8) / 64f, (r.height - 8) / 64f);
        float cx = r.center.x, cy = r.center.y;
        Color baseColor = colors[index];
        ItemPixelRect(cx, cy, -21, -21, 42, 42, scale, baseColor);
        ItemPixelRect(cx, cy, -14, -28, 28, 7, scale, baseColor);
        ItemPixelRect(cx, cy, -28, -14, 7, 28, scale, baseColor);
        ItemPixelRect(cx, cy, 21, -14, 7, 28, scale, baseColor);
        ItemPixelRect(cx, cy, -14, 21, 28, 7, scale, baseColor);
        ItemPixelRect(cx, cy, -10, -10, 8, 8, scale, new Color(1f, .88f, .62f));
        ItemPixelRect(cx, cy, 5, 5, 8, 8, scale, new Color(.18f, .15f, .16f));
        if (index == 4 || index >= 13)
        {
            ItemPixelRect(cx, cy, -7, -28, 14, 56, scale, new Color(.28f,.3f,.35f));
            ItemPixelRect(cx, cy, -28, -7, 56, 14, scale, new Color(.28f,.3f,.35f));
        }
        else if (index == 3 || index == 9)
        {
            ItemPixelRect(cx, cy, -3, -31, 6, 62, scale, new Color(.25f,.48f,.2f));
            ItemPixelRect(cx, cy, -21, -3, 42, 6, scale, new Color(.25f,.48f,.2f));
        }
        else if (index == 0 || index == 2 || index >= 10)
        {
            ItemPixelRect(cx, cy, -14, -3, 28, 6, scale, new Color(.96f,.86f,.65f));
        }
    }

    private void ItemPixelRect(float cx, float cy, float x, float y, float width, float height, float scale, Color color)
    {
        PixelRect(new Rect(cx + x * scale, cy + y * scale, width * scale, height * scale), color);
    }

    private void DrawQuestList()
    {
        GUI.Label(new Rect(710, 360, 940, 36), "길드 소속이 아닌 방문 용사도 수행할 수 있어요.", labelStyle);
        int visible = 0;
        for (int i = 0; i < quests.Length; i++)
        {
            if (!quests[i].collected || quests[i].completed || quests[i].expired || day >= quests[i].expiresOnDay) continue;
            Rect row = new Rect(706, 410 + visible++ * 146, 958, 126);
            GUI.Box(row, "", panelStyle);
            GUI.Label(new Rect(row.x + 26, row.y + 13, 670, 45), quests[i].name, headingStyle);
            int remaining = Mathf.Max(0, quests[i].expiresOnDay - day);
            GUI.Label(new Rect(row.x + 26, row.y + 64, 850, 38), "난이도  " + quests[i].difficulty + "     보수  " + quests[i].reward + " G     기한 " + remaining + "일 남음", labelStyle);
            if (GUI.Button(row, GUIContent.none, hoverStyle)) selectedQuest = i;
        }
        if (visible == 0) GUI.Label(new Rect(710, 420, 940, 110), "요청할 간단 의뢰가 없습니다. 낮에 마을 게시판에서 수락해 주세요.\n전투 의뢰는 바 안쪽 길드장에게서 준비합니다.", labelStyle);
    }

    private void DrawQuestDetail()
    {
        Quest quest = quests[selectedQuest];
        Rect detail = new Rect(720, 364, 930, 446);
        GUI.Box(detail, "", panelStyle);
        GUI.Label(new Rect(758, 382, 760, 56), quest.name, headingStyle);
        if (GUI.Button(new Rect(1574, 372, 48, 48), "×", buttonStyle)) { selectedQuest = -1; return; }
        GUI.Label(new Rect(758, 452, 826, 144), quest.detail, labelStyle);
        int remainingDays = Mathf.Max(0, quest.expiresOnDay - day);
        GUI.Label(new Rect(758, 602, 826, 50), "난이도  " + quest.difficulty + "     성공 보수  " + quest.reward + " G     기한 " + remainingDays + "일 남음", labelStyle);
        GUI.Label(new Rect(758, 650, 826, 54), "성공하면 골드와 경험치, 호감도를 얻을 수 있습니다.", labelStyle);
        if (GUI.Button(new Rect(1080, 724, 520, 64), "이 용사에게 의뢰 요청", buttonStyle)) RequestQuest(quest);
    }

    private void DrawSaleConfirmation()
    {
        if (selectedCategory < 0 || selectedCategory >= 3 ||
            confirmItem < 0 || confirmItem >= inventory[selectedCategory].Length)
        {
            confirmItem = -1;
            return;
        }

        ShopItem item = inventory[selectedCategory][confirmItem];
        Rect dialog = new Rect(600, 370, 720, 324);
        GUI.color = new Color(0, 0, 0, .78f);
        GUI.DrawTexture(new Rect(0, 0, 1920, 1080), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Box(dialog, "", panelStyle);
        GUI.Label(new Rect(646, 416, 628, 72), guests[selectedGuest].name + "에게 " + item.name + "을(를) 판매하시겠습니까?", headingStyle);
        GUI.Label(new Rect(646, 498, 628, 46), "가격  " + item.price + " G     보유 골드  " + guests[selectedGuest].gold + " G", labelStyle);
        if (GUI.Button(new Rect(680, 584, 250, 70), "예", buttonStyle))
        {
            ConfirmSale(item);
            Event.current.Use();
            return;
        }
        if (GUI.Button(new Rect(988, 584, 250, 70), "아니요", tabStyle))
        {
            confirmItem = -1;
            notice = "판매를 취소했어요.";
            Event.current.Use();
        }
    }

    private void ConfirmSale(ShopItem item)
    {
        Guest guest = guests[selectedGuest];
        confirmItem = -1;
        if (item.stock <= 0) { notice = "품절된 상품입니다. 낮에 제작해 주세요."; return; }
        if (guest.gold < item.price)
        {
            notice = "골드가 부족해 구매할 수 없어요.";
            return;
        }
        guest.gold -= item.price;
        item.stock--;
        guildGold += item.price;
        totalSales++;
        guest.affinity = Mathf.Min(5, guest.affinity + 1);
        if (guest.affinity >= 3 && !guest.guildMember)
        {
            TryRecruitGuest(guest);
            notice = dayNotice;
        }
        else notice = guest.name + "에게 " + item.name + "을(를) 판매했어요.";
    }

    private void RequestQuest(Quest quest)
    {
        if (!quest.collected || quest.completed || quest.expired || day >= quest.expiresOnDay) return;
        Guest guest = guests[selectedGuest];
        float chance = Mathf.Clamp(.58f + guest.level * .08f - quest.difficulty * .08f + guest.experience * .01f, .25f, .94f);
        if (UnityEngine.Random.value <= chance)
        {
            quest.completed = true;
            successfulRequests++;
            guildExperience += quest.difficulty * 5;
            while (guildExperience >= guildLevel * 20)
            {
                guildExperience -= guildLevel * 20;
                guildLevel++;
            }
            guest.gold += quest.reward;
            guest.experience++;
            guest.affinity = Mathf.Min(5, guest.affinity + 1);
            if (guest.affinity >= 3 && !guest.guildMember)
            {
                TryRecruitGuest(guest);
                notice = guest.guildMember
                    ? guest.name + "이(가) 의뢰 성공! " + quest.reward + " G를 벌고 길드에 가입했어요."
                    : guest.name + "이(가) 의뢰 성공! 숙소가 가득 차 가입은 보류됐습니다.";
            }
            else notice = guest.name + "이(가) 의뢰 성공! " + quest.reward + " G를 벌었어요.";
        }
        else
        {
            guest.affinity = Mathf.Max(0, guest.affinity - 1);
            notice = guest.name + "이(가) 의뢰에 실패해 호감도가 내려갔어요. 기한이 남아 있으면 다른 용사에게 다시 맡길 수 있어요.";
        }
        selectedQuest = -1;
    }

    private void ClosePopup()
    {
        selectedGuest = -1;
        selectedQuest = -1;
        confirmItem = -1;
        notice = "";
    }

    private void EnsureStyles()
    {
        if (stylesReady) return;
        koreanFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 28);
        Texture2D dark = SolidTexture(new Color(.055f, .066f, .11f, .97f));
        Texture2D tab = SolidTexture(new Color(.13f, .15f, .22f, .98f));
        Texture2D gold = SolidTexture(new Color(.42f, .29f, .13f, 1f));
        Texture2D hoverBg = SolidTexture(new Color(.52f, .37f, .19f, 1f));
        panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = dark, textColor = Color.white }, border = new RectOffset(7,7,7,7) };
        cardStyle = new GUIStyle(panelStyle) { normal = { background = SolidTexture(new Color(.105f,.12f,.18f,.99f)), textColor = Color.white } };
        headingStyle = new GUIStyle(GUI.skin.label) { font = koreanFont, fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = true, normal = { textColor = new Color(1f,.87f,.62f) } };
        labelStyle = new GUIStyle(GUI.skin.label) { font = koreanFont, fontSize = 21, alignment = TextAnchor.MiddleLeft, wordWrap = true, normal = { textColor = new Color(.94f,.92f,.86f) } };
        buttonStyle = new GUIStyle(GUI.skin.button) { font = koreanFont, fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { background = gold, textColor = Color.white }, hover = { background = hoverBg, textColor = Color.white }, active = { background = dark, textColor = Color.white } };
        tabStyle = new GUIStyle(buttonStyle) { normal = { background = tab, textColor = Color.white } };
        selectedTabStyle = new GUIStyle(buttonStyle) { normal = { background = gold, textColor = Color.white } };
        itemStyle = new GUIStyle(GUIStyle.none);
        hoverStyle = new GUIStyle(GUIStyle.none);
        hoverStyle.normal.background = null; hoverStyle.hover.background = null; hoverStyle.active.background = null;
        ApplyTextSize();
        stylesReady = true;
    }

    private static Texture2D SolidTexture(Color color)
    {
        Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
        t.SetPixel(0, 0, color); t.Apply(); return t;
    }

    private void PixelRect(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = new Color(color.r * itemIconTint.r, color.g * itemIconTint.g, color.b * itemIconTint.b, color.a * itemIconTint.a);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static void PixelRect(Rect rect) => GUI.DrawTexture(rect, Texture2D.whiteTexture);
}
