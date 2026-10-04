using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime : MonoBehaviour
{
    [Serializable]
    private sealed class Guest
    {
        public string name;
        public int level;
        public int gold;
        public int affinity;
        public bool guildMember;
        public int experience;
        public int trainingExperience;
        public HeroStats stats;
        public int lastFoodPurchaseDay = -1;
        public int lastQuestRequestDay = -1;
        public int lastWeaponPurchaseDay = -1, lastArmorPurchaseDay = -1;
        public int portraitIndex, foodPreference, lastVisitNight = -1;
        public int preferredReturnOrder = -1;
        public bool hasVisited;
        public float trainingRemainder;
        public string baseClass;
        public string advancement = "";
        public ShopItem equippedWeapon, equippedArmor, carriedFood;
        public readonly List<string> equippedSkills = new List<string>();

        public Guest(string name, int level, int gold, int affinity, bool guildMember, string baseClass)
        {
            this.name = name; this.level = level; this.gold = gold;
            this.affinity = AffinityThresholdForStage(affinity); this.guildMember = guildMember; this.baseClass = baseClass;
            stats = CalculateHeroStats(baseClass, level);
        }
    }

    [Serializable]
    private struct HeroStats
    {
        public int hp, attack, defense, speed;
        public HeroStats(int hp, int attack, int defense, int speed)
        { this.hp = hp; this.attack = attack; this.defense = defense; this.speed = speed; }
        public override string ToString() => "HP " + hp + " · 공격 " + attack + " · 방어 " + defense + " · 속도 " + speed;
    }

    private sealed class ShopItem
    {
        public string name;
        public int price;
        public int icon;
        public int stock = 2;
        public int unlockGuildLevel, productionCost, hpRecovery, attackBonus, defenseBonus;
        public string allowedJob;
        public ShopItem(string name, int price, int icon, int unlockGuildLevel = 1, int productionCost = 0,
            string allowedJob = "전 직업", int hpRecovery = 0, int attackBonus = 0, int defenseBonus = 0)
        {
            this.name = name; this.price = price; this.icon = icon; this.unlockGuildLevel = unlockGuildLevel;
            this.productionCost = productionCost; this.allowedJob = allowedJob; this.hpRecovery = hpRecovery;
            this.attackBonus = attackBonus; this.defenseBonus = defenseBonus;
        }
    }

    private sealed class Quest
    {
        public string name, detail;
        public int reward, difficulty, unlockGuildLevel, recommendedLevel, heroExperienceReward, guildExperienceReward, guildCommission;
        public bool collected;
        public int acceptedDay = -1;
        public int expiresOnDay = -1;
        public int deadlineDays = 1;
        public bool completed;
        public bool expired;
        public int lastAttemptDay = -1;
        public Quest(string name, string detail, int unlockGuildLevel, int recommendedLevel, int reward, int heroExperienceReward, int guildExperienceReward)
        {
            this.name = name; this.detail = detail; this.unlockGuildLevel = unlockGuildLevel; this.recommendedLevel = recommendedLevel;
            this.reward = reward; this.heroExperienceReward = heroExperienceReward; this.guildExperienceReward = guildExperienceReward;
            difficulty = recommendedLevel;
            guildCommission = Mathf.RoundToInt(reward * .15f);
        }
    }

    private readonly List<Guest> guests = new List<Guest>
    {
        new Guest("루나", 1, 18, 0, false, "마법사"), new Guest("브란", 1, 25, 0, false, "전사"),
        new Guest("세라", 1, 31, 0, false, "도적"), new Guest("에단", 1, 35, 0, false, "궁수"),
        new Guest("미라", 1, 16, 0, false, "전사"), new Guest("노아", 1, 46, 0, false, "마법사"),
    };

    private readonly ShopItem[][] inventory =
    {
        new[] {
            new ShopItem("꿀 바른 빵", 8, 0, 1, 0, "전 직업", 8), new ShopItem("산딸기 수프", 10, 1, 2, 0, "전 직업", 12), new ShopItem("구운 고기", 14, 2, 1, 0, "전 직업", 16),
            new ShopItem("허브 치킨", 24, 9, 3, 1, "전 직업", 24), new ShopItem("버섯 파이", 28, 10, 4, 1, "전 직업", 28), new ShopItem("훈제 연어", 32, 11, 4, 1, "전 직업", 32),
            new ShopItem("왕실 만찬", 65, 18, 8, 2, "전 직업", 40), new ShopItem("마력열매 타르트", 62, 19, 9, 2, "전 직업", 48), new ShopItem("용고기 스테이크", 80, 20, 9, 2, "전 직업", 60)
        },
        new[] {
            new ShopItem("견습자의 검", 20, 3, 1, 0, "전사", 0, 3), new ShopItem("초보 도적의 단검", 22, 27, 1, 0, "도적", 0, 3), new ShopItem("사냥꾼의 활", 28, 4, 1, 0, "궁수", 0, 3), new ShopItem("참나무 지팡이", 24, 5, 1, 0, "마법사", 0, 3),
            new ShopItem("강철 장검", 55, 12, 3, 1, "전사", 0, 7), new ShopItem("그림자 단검", 58, 30, 3, 1, "도적", 0, 7), new ShopItem("장인의 장궁", 68, 13, 4, 1, "궁수", 0, 7), new ShopItem("마도사의 지팡이", 62, 14, 7, 1, "마법사", 0, 8),
            new ShopItem("왕가의 검", 120, 21, 8, 2, "전사", 0, 13), new ShopItem("암살자의 칠흑 단검", 125, 33, 8, 2, "도적", 0, 13), new ShopItem("용사냥 장궁", 145, 22, 8, 2, "궁수", 0, 13), new ShopItem("대마법사의 지팡이", 135, 23, 9, 2, "마법사", 0, 15)
        },
        new[] {
            new ShopItem("가죽 조끼", 18, 6, 1, 0, "전사", 0, 0, 2), new ShopItem("그림자 가죽옷", 20, 36, 1, 0, "도적", 0, 0, 2), new ShopItem("여행자 망토", 16, 8, 1, 0, "궁수", 0, 0, 1), new ShopItem("수습 마법사의 로브", 19, 8, 1, 0, "마법사", 0, 0, 1),
            new ShopItem("강화 가죽갑옷", 45, 15, 3, 1, "전사", 0, 0, 4), new ShopItem("암살자의 경량 갑옷", 47, 39, 3, 1, "도적", 0, 0, 4), new ShopItem("숲 사냥꾼 갑옷", 49, 15, 4, 1, "궁수", 0, 0, 3), new ShopItem("마법 망토", 48, 17, 7, 1, "마법사", 0, 0, 3),
            new ShopItem("용비늘 갑옷", 110, 24, 8, 2, "전사", 0, 0, 7), new ShopItem("밤그림자 잠행복", 116, 42, 8, 2, "도적", 0, 0, 7), new ShopItem("폭풍매 사냥복", 120, 24, 8, 2, "궁수", 0, 0, 6), new ShopItem("별빛 로브", 118, 26, 9, 2, "마법사", 0, 0, 6)
        },
    };

    private static readonly Quest[] questCatalog =
    {
        new Quest("약초 바구니 수집", "마을 북쪽 들판에서 약초를 모아 약제상에게 전달합니다.", 1, 1, 14, 16, 7),
        new Quest("상인에게 편지 전달", "동쪽 길목의 상인에게 길드장의 편지를 전달합니다.", 1, 2, 20, 22, 9),
        new Quest("숲길 순찰", "숲 입구를 살피고 위험한 흔적이 없는지 확인합니다.", 1, 3, 26, 28, 11),
        new Quest("수확물 운반", "농장에서 수확한 곡물과 식재료를 마을 창고까지 옮겨 주세요.", 2, 4, 32, 34, 13),
        new Quest("광산 보급품 배달", "광부들이 기다리는 광산 입구까지 식량과 도구를 전달합니다.", 2, 5, 38, 40, 15),
        new Quest("약초밭 보호", "약초밭에 나타난 야생동물을 쫓아내고 작물을 지켜 주세요.", 3, 6, 44, 46, 17),
        new Quest("산길 지도 작성", "상단이 이용할 수 있도록 산길의 갈림길과 위험 구간을 기록합니다.", 3, 7, 50, 52, 19),
        new Quest("대상단 길 안내", "마을에 도착한 대상단을 다음 거점까지 안전하게 안내합니다.", 4, 8, 56, 58, 21),
        new Quest("폐허 조사", "오래된 유적의 입구와 주변 흔적을 조사해 보고해 주세요.", 4, 9, 62, 64, 23),
        new Quest("국경 정찰", "국경 근처의 이동 흔적을 확인하고 초소에 보고합니다.", 5, 10, 68, 70, 25),
        new Quest("유적 기록 회수", "유적 내부에 남은 기록물을 찾아 학자에게 전달합니다.", 6, 12, 80, 82, 29),
        new Quest("마력 오염 조사", "마력이 뒤틀린 지역을 조사하고 오염의 원인을 기록해 주세요.", 7, 14, 92, 94, 33),
        new Quest("왕실 전령 호위", "왕실의 전령이 국경 도시까지 무사히 갈 수 있도록 호위합니다.", 8, 16, 104, 106, 37),
        new Quest("마왕군 동향 파악", "마왕군의 이동 상황을 확인해 길드에 보고해 주세요.", 9, 18, 116, 118, 41),
        new Quest("결전 보급로 확보", "결전 준비를 위해 전방 보급로와 안전한 이동 경로를 확보합니다.", 10, 20, 128, 130, 45),
    };
    private readonly List<Quest> quests = new List<Quest>();
    private Vector2 questBoardScrollPosition;
    private static readonly int[] GuildLevelExperience = { 60, 100, 150, 220, 300, 400, 520, 660, 820 };
    private static readonly int[] AffinityPointThresholds = { 0, 20, 50, 90, 150, 230 };

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
    private int recruitmentPopupGuest = -1;
    private string toastMessage = "";
    private float toastExpiresAt;
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
        ApplyBalanceV05();
        foreach (Guest guest in guests)
            foreach (string skill in BasicSkillsFor(guest.baseClass))
                guest.equippedSkills.Add(BasicSkillId(guest, skill));
        dailyActionsRemaining = DailyActionCapacity();
        GenerateDailyQuests();
        presentation = gameObject.AddComponent<GuildHallPresentation>();
        presentation.Initialize(GetComponent<Camera>());
        guestPortraitTextures = presentation.Portraits;
        SeatOpeningRoster();
    }

    private static int AffinityThresholdForStage(int stage)
        => AffinityPointThresholds[Mathf.Clamp(stage, 0, AffinityPointThresholds.Length - 1)];

    private static int AffinityStage(Guest guest)
    {
        int stage = 0;
        for (int i = 1; i < AffinityPointThresholds.Length; i++)
            if (guest.affinity >= AffinityPointThresholds[i]) stage = i;
        return stage;
    }

    private static int HeroExperienceRequired(int level)
        => level >= 20 ? 0 : 20 + 8 * (level - 1) + 2 * (level - 1) * (level - 1);

    private static HeroStats CalculateHeroStats(string job, int level)
    {
        if (!BalanceStats.TryGetValue(job, out HeroStats[] rows))
            throw new ArgumentException("Unknown hero class: " + job);
        return rows[Mathf.Clamp(level - 1, 0, rows.Length - 1)];
    }

    private static void RefreshHeroStats(Guest guest)
    {
        if (guest != null) guest.stats = CalculateHeroStats(guest.baseClass, guest.level);
    }

    // Future class-change UI should call this rather than assigning baseClass directly.
    private void ChangeGuestBaseClass(Guest guest, string newClass)
    {
        if (guest == null || (newClass != "전사" && newClass != "도적" && newClass != "궁수" && newClass != "마법사")) return;
        if (guest.baseClass == newClass) return;
        guest.baseClass = newClass;
        if (!CanEquip(guest, guest.equippedWeapon)) guest.equippedWeapon = null;
        if (!CanEquip(guest, guest.equippedArmor)) guest.equippedArmor = null;
        guest.advancement = "";
        guest.equippedSkills.Clear();
        foreach (string skill in BasicSkillsFor(newClass)) guest.equippedSkills.Add(BasicSkillId(guest, skill));
        RefreshHeroStats(guest);
    }

    private static string HeroStatsLabel(Guest guest) => guest.stats.ToString();

    private static float QuestSuccessChance(Guest guest, Quest quest)
    {
        int gearScore = (guest.equippedWeapon != null ? guest.equippedWeapon.attackBonus : 0)
                      + (guest.equippedArmor != null ? guest.equippedArmor.defenseBonus : 0);
        float gearBonus = Mathf.Min(.10f, gearScore * .005f);
        return Mathf.Clamp(.75f + (guest.level - quest.recommendedLevel) * .06f + gearBonus, .20f, .95f);
    }

    private int GuildExperienceRequired(int level)
        => level <= 0 || level > GuildLevelExperience.Length ? 0 : GuildLevelExperience[level - 1];

    private void AddGuildExperience(int amount)
    {
        guildExperience += Mathf.Max(0, amount);
        while (guildLevel < 10 && guildExperience >= GuildExperienceRequired(guildLevel))
        {
            guildExperience -= GuildExperienceRequired(guildLevel);
            guildLevel++;
        }
    }

    private void GrantHeroExperience(Guest guest, int amount)
    {
        int gained = guest.guildMember ? Mathf.RoundToInt(amount * 1.15f) : amount;
        guest.experience += gained;
        guest.trainingExperience += gained;
        while (guest.level < 20 && guest.trainingExperience >= HeroExperienceRequired(guest.level))
        {
            guest.trainingExperience -= HeroExperienceRequired(guest.level);
            guest.level++;
        }
        RefreshHeroStats(guest);
    }

    private int DailyQuestBoardCount(int level)
    {
        int[] counts = { 3, 4, 4, 5, 5, 6, 6, 6, 6, 6 };
        return counts[Mathf.Clamp(level - 1, 0, counts.Length - 1)];
    }

    private void GenerateDailyQuests()
    {
        quests.RemoveAll(q => !q.collected || q.completed || q.expired || day >= q.expiresOnDay);
        var available = new List<Quest>();
        foreach (Quest quest in questCatalog)
            if (quest.unlockGuildLevel <= guildLevel && !quests.Exists(q => q.name == quest.name)) available.Add(quest);

        int count = Mathf.Min(DailyQuestBoardCount(guildLevel), available.Count);
        for (int i = 0; i < count; i++)
        {
            int pick = UnityEngine.Random.Range(0, available.Count);
            Quest template = available[pick];
            available.RemoveAt(pick);
            Quest posted = new Quest(template.name, template.detail, template.unlockGuildLevel, template.recommendedLevel,
                template.reward, template.heroExperienceReward, template.guildExperienceReward);
            float roll = UnityEngine.Random.value;
            posted.deadlineDays = roll < .3f ? 1 : roll < .7f ? 2 : 3;
            quests.Add(posted);
        }
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

        DrawMainInterface(ox, oy, scale);
        GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));
        if (recruitmentPopupGuest >= 0) DrawRecruitmentPopup();
        DrawToast();
        GUI.matrix = Matrix4x4.identity;
    }

    private void DrawMainInterface(float ox, float oy, float scale)
    {
        if (combat != null) { DrawCombatScreen(); return; }
        if (combatTestSetupOpen) { DrawCombatTestSetup(); return; }
        if (recruitmentPopupGuest >= 0)
        {
            presentation.SetHovered(-1);
            presentation.SetGuildmasterHovered(false);
            return;
        }
        if (battlePreparationOpen)
        {
            presentation.SetHovered(-1);
            presentation.SetGuildmasterHovered(false);
            DrawBattlePreparation();
            GUI.matrix = Matrix4x4.identity;
            return;
        }

        if (worldMapOpen)
        {
            presentation.SetHovered(-1);
            presentation.SetGuildmasterHovered(false);
            DrawWorldMap();
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
            if (worldMapOpen)
            {
                DrawWorldMap();
                GUI.matrix = Matrix4x4.identity;
                return;
            }
            if (managementPage != 0)
            {
                presentation.SetHovered(-1);
                presentation.SetGuildmasterHovered(false);
                DrawManagementPage();
                GUI.matrix = Matrix4x4.identity;
                return;
            }
        }
        if (!IsModalOpen() && GUI.Button(new Rect(1590, 964, 296, 76), PreparedDeparture()!=null ? "하루 종료 · 출전 →" : "하루 종료  →", buttonStyle))
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

    private void ShowToast(string message)
    {
        toastMessage = message;
        toastExpiresAt = Time.unscaledTime + 3.4f;
    }

    private void DrawToast()
    {
        if (string.IsNullOrEmpty(toastMessage)) return;
        if (Time.unscaledTime >= toastExpiresAt) { toastMessage = ""; return; }
        GUI.Box(new Rect(500, 24, 920, 92), "", panelStyle);
        PixelRect(new Rect(500, 24, 8, 92), new Color(1f, .68f, .25f));
        GUI.Label(new Rect(528, 32, 864, 76), toastMessage, headingStyle);
    }

    private void DrawRecruitmentPopup()
    {
        if (recruitmentPopupGuest < 0 || recruitmentPopupGuest >= guests.Count) { recruitmentPopupGuest = -1; return; }
        PixelRect(new Rect(0, 0, 1920, 1080), new Color(0, 0, 0, .72f));
        Rect panel = new Rect(510, 286, 900, 508);
        GUI.Box(panel, "", panelStyle);
        GUI.Label(new Rect(570, 326, 780, 58), "새로운 길드원이 되었습니다!", headingStyle);
        DrawGuestPortrait(new Rect(572, 414, 260, 286), recruitmentPopupGuest);
        Guest guest = guests[recruitmentPopupGuest];
        GUI.Label(new Rect(876, 438, 470, 54), guest.name + " · " + guest.baseClass, headingStyle);
        GUI.Label(new Rect(876, 504, 470, 142), "호감도 2단계에 도달해 길드에 가입했습니다.\n이제 길드원으로 함께 활동합니다.", labelStyle);
        bool close = GUI.Button(new Rect(876, 682, 300, 66), "확인", buttonStyle);
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape))
        {
            Event.current.Use();
            close = true;
        }
        if (close) recruitmentPopupGuest = -1;
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
        GUI.Label(new Rect(244, 666, 360, 42), guest.name + "  ·  " + guest.baseClass, headingStyle);
        GUI.Label(new Rect(244, 710, 360, 38), "레벨 " + guest.level + "   " + (guest.guildMember ? "길드 소속" : "방문 용사"), labelStyle);
        GUI.Label(new Rect(244, 746, 372, 66), HeroStatsLabel(guest), labelStyle);
        GUI.Box(new Rect(238, 816, 372, 84), "", panelStyle);
        GUI.Label(new Rect(256, 824, 338, 68), "“" + guestDialogue + "”", labelStyle);

        Rect right = new Rect(664, 164, 1040, 760);
        GUI.Box(right, "", panelStyle);
        GUI.Label(new Rect(704, 186, 900, 48), "선호 음식 · " + new[] { "빵·과일", "고기", "채소·버섯" }[guest.foodPreference], headingStyle);
        GUI.Label(new Rect(704, 232, 900, 34), "보유 골드  " + guest.gold + " G     호감도  " + AffinityStage(guest) + "/5     길드 금고  " + guildGold + " G", labelStyle);

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
        if (index < 0 || index >= guests.Count) return;
        index = guests[index].portraitIndex;
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
        int rows = Mathf.CeilToInt(items.Length / 4f);
        shopScrollPosition = GUI.BeginScrollView(view, shopScrollPosition, new Rect(0, 0, 962, Mathf.Max(486, rows * 158)), false, true);
        for (int i = 0; i < items.Length; i++)
        {
            float x = (i % 4) * 240, y = (i / 4) * 158;
            Rect card = new Rect(x, y, 230, 150);
            GUI.Box(card, "", cardStyle);
            DrawPixelItemIcon(new Rect(card.x + 6, card.y + 24, 74, 96), items[i].icon);
            GUI.Label(new Rect(card.x + 86, card.y + 20, 138, 62), items[i].name, headingStyle);
            int tierIndex = items[i].productionCost;
            string tier = tierIndex == 0 ? "저가" : tierIndex == 1 ? "중가" : "고가";
            string availability = guildLevel >= items[i].unlockGuildLevel ? "재고 " + items[i].stock : "길드 Lv." + items[i].unlockGuildLevel + " 해금";
            GUI.Label(new Rect(card.x + 86, card.y + 88, 138, 50), tier + " · " + items[i].price + " G\n" + availability, labelStyle);
            GUI.enabled = items[i].stock > 0 && guildLevel >= items[i].unlockGuildLevel;
            if (GUI.Button(card, GUIContent.none, hoverStyle)) confirmItem = i;
            GUI.enabled = true;
        }
        GUI.EndScrollView();
    }

    private void DrawPixelItemIcon(Rect r, int type)
    {
        int tier;
        if (type >= 27 && type <= 35) { tier = (type - 27) / 3; type = 9; }
        else if (type >= 36 && type <= 44) { tier = (type - 36) / 3; type = 10; }
        else { tier = type / 9; type %= 9; }
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
            case 9: // dagger
                ItemPixelRect(cx, cy, -8, -94, 16, 112, scale, new Color(.79f,.85f,.93f));
                ItemPixelRect(cx, cy, -25, 14, 50, 13, scale, new Color(.83f,.66f,.38f));
                ItemPixelRect(cx, cy, -8, 27, 16, 54, scale, new Color(.34f,.2f,.15f)); break;
            case 10: // rogue leather armor
                ItemPixelRect(cx, cy, -58, -70, 116, 132, scale, new Color(.2f,.16f,.22f));
                ItemPixelRect(cx, cy, -42, -52, 84, 96, scale, new Color(.32f,.24f,.34f));
                ItemPixelRect(cx, cy, -12, -48, 24, 90, scale, new Color(.73f,.52f,.27f));
                ItemPixelRect(cx, cy, -46, 16, 92, 14, scale, new Color(.19f,.16f,.2f)); break;
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
            new Color(.64f,.68f,.76f), new Color(.37f,.72f,.84f), new Color(.55f,.57f,.6f),
            new Color(.72f,.22f,.18f), new Color(.49f,.16f,.62f), new Color(.72f,.61f,.32f),
            new Color(.93f,.42f,.12f), new Color(.23f,.2f,.34f), new Color(.36f,.78f,.9f)
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
        if (index == 4 || (index >= 13 && index < 18))
        {
            ItemPixelRect(cx, cy, -7, -28, 14, 56, scale, new Color(.28f,.3f,.35f));
            ItemPixelRect(cx, cy, -28, -7, 56, 14, scale, new Color(.28f,.3f,.35f));
        }
        else if (index == 3 || index == 9)
        {
            ItemPixelRect(cx, cy, -3, -31, 6, 62, scale, new Color(.25f,.48f,.2f));
            ItemPixelRect(cx, cy, -21, -3, 42, 6, scale, new Color(.25f,.48f,.2f));
        }
        else if (index == 0 || index == 2 || (index >= 10 && index < 18))
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
        for (int i = 0; i < quests.Count; i++)
        {
            if (!quests[i].collected || quests[i].completed || quests[i].expired || day >= quests[i].expiresOnDay) continue;
            Rect row = new Rect(706, 410 + visible++ * 146, 958, 126);
            GUI.Box(row, "", panelStyle);
            GUI.Label(new Rect(row.x + 26, row.y + 13, 670, 45), quests[i].name, headingStyle);
            int remaining = Mathf.Max(0, quests[i].expiresOnDay - day);
            GUI.Label(new Rect(row.x + 26, row.y + 64, 850, 38), "권장 Lv." + quests[i].recommendedLevel + "   용사 보상 " + quests[i].reward + " G   기한 " + remaining + "일 남음", labelStyle);
            if (GUI.Button(row, GUIContent.none, hoverStyle)) selectedQuest = i;
        }
        if (visible == 0) GUI.Label(new Rect(710, 420, 940, 110), "요청할 간단 의뢰가 없습니다. 낮에 마을 게시판에서 수락해 주세요.\n전투 의뢰는 저녁의 지도에서 준비합니다.", labelStyle);
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
        float chance = QuestSuccessChance(guests[selectedGuest], quest);
        GUI.Label(new Rect(758, 602, 826, 50), "권장 Lv." + quest.recommendedLevel + "   성공률 " + Mathf.RoundToInt(chance * 100) + "%   남은 기한 " + remainingDays + "일", labelStyle);
        GUI.Label(new Rect(758, 650, 826, 54), "성공 보상: 용사 " + quest.reward + " G + " + quest.heroExperienceReward + " EXP · 길드 수수료 " + quest.guildCommission + " G · 길드 " + quest.guildExperienceReward + " EXP", labelStyle);
        GUI.enabled = guests[selectedGuest].lastQuestRequestDay != day && quest.lastAttemptDay != day;
        if (GUI.Button(new Rect(1080, 724, 520, 64), GUI.enabled ? "이 용사에게 의뢰 요청" : "오늘 의뢰 수행 완료", buttonStyle)) RequestQuest(quest);
        GUI.enabled = true;
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
        string purchaseLimit = selectedCategory == 0
            ? "음식 1개/밤"
            : "장비 예산 " + Mathf.FloorToInt(guests[selectedGuest].gold * .8f) + " G · " + item.allowedJob;
        GUI.Label(new Rect(646, 498, 628, 46), "가격  " + item.price + " G     보유 골드  " + guests[selectedGuest].gold + " G · " + purchaseLimit, labelStyle);
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
        if (guildLevel < item.unlockGuildLevel) { notice = "길드 레벨 " + item.unlockGuildLevel + "부터 판매할 수 있는 상품입니다."; return; }
        if (selectedCategory > 0 && !CanEquip(guest, item))
        { notice = guest.name + "의 직업은 이 장비를 사용할 수 없습니다."; return; }
        if (selectedCategory == 0 && guest.lastFoodPurchaseDay == day)
        { notice = "음식은 용사 한 명에게 하루 한 개만 판매할 수 있습니다."; return; }
        if (selectedCategory > 0 && item.price > Mathf.FloorToInt(guest.gold * .8f))
        { notice = "장비 구매 예산은 현재 보유 골드의 80%까지입니다."; return; }
        if (selectedCategory == 1 && (guest.lastWeaponPurchaseDay == day || (guest.equippedWeapon != null && item.attackBonus <= guest.equippedWeapon.attackBonus)))
        { notice = "무기는 밤마다 한 번, 기존 장비보다 성능이 좋을 때 구매합니다."; return; }
        if (selectedCategory == 2 && (guest.lastArmorPurchaseDay == day || (guest.equippedArmor != null && item.defenseBonus <= guest.equippedArmor.defenseBonus)))
        { notice = "방어구는 밤마다 한 번, 기존 장비보다 성능이 좋을 때 구매합니다."; return; }
        if (item.stock <= 0) { notice = "품절된 상품입니다. 길드 작업장에서 제작해 주세요."; return; }
        if (guest.gold < item.price)
        {
            notice = "골드가 부족해 구매할 수 없어요.";
            return;
        }
        guest.gold -= item.price;
        item.stock--;
        guildGold += item.price;
        totalSales++;
        if (selectedCategory == 0)
        {
            guest.lastFoodPurchaseDay = day;
            guest.carriedFood = item;
        }
        else if (selectedCategory == 1)
        {
            guest.lastWeaponPurchaseDay = day;
            guest.equippedWeapon = item;
        }
        else
        {
            guest.lastArmorPurchaseDay = day;
            guest.equippedArmor = item;
        }
        int previousAffinity = guest.affinity;
        int affection = 3 + (selectedCategory == 0 && FoodPreferenceGroup(item) == guest.foodPreference ? 6 : 0);
        guest.affinity = Mathf.Min(AffinityPointThresholds[5], guest.affinity + affection);
        int affinityGained = guest.affinity - previousAffinity;
        bool favoriteFood = selectedCategory == 0 && FoodPreferenceGroup(item) == guest.foodPreference;
        if (favoriteFood && guest.preferredReturnOrder < 0) guest.preferredReturnOrder = returnPrioritySequence++;
        ShowToast(guest.name + "에게 " + item.name + " 판매  ·  길드 금고 +" + item.price + " G  ·  용사 골드 -" + item.price + " G  ·  호감도 +" + affinityGained +
            (favoriteFood ? "  ·  다음 밤 재방문 확정" : ""));
        if (AffinityStage(guest) >= 2 && !guest.guildMember)
        {
            TryRecruitGuest(guest);
            notice = dayNotice;
        }
        else notice = guest.name + "에게 " + item.name + "을(를) 판매했어요.";
    }

    private void RequestQuest(Quest quest)
    {
        if (!quest.collected || quest.completed || quest.expired || day >= quest.expiresOnDay) return;
        if (quest.lastAttemptDay == day) { notice = "실패한 의뢰는 다음 날 다시 시도할 수 있습니다."; return; }
        Guest guest = guests[selectedGuest];
        if (guest.lastQuestRequestDay == day) { notice = "용사 한 명은 밤마다 간단 의뢰를 한 건만 수행할 수 있습니다."; return; }
        guest.lastQuestRequestDay = day;
        quest.lastAttemptDay = day;
        float chance = QuestSuccessChance(guest, quest);
        if (UnityEngine.Random.value <= chance)
        {
            int previousAffinity = guest.affinity;
            int previousHeroExperience = guest.experience;
            quest.completed = true;
            successfulRequests++;
            guildGold += quest.guildCommission;
            AddGuildExperience(quest.guildExperienceReward);
            guest.gold += quest.reward;
            GrantHeroExperience(guest, quest.heroExperienceReward);
            guest.affinity = Mathf.Min(AffinityPointThresholds[5], guest.affinity + 8);
            int affinityGained = guest.affinity - previousAffinity;
            int heroExperienceGained = guest.experience - previousHeroExperience;
            ShowToast(guest.name + " · 용사 골드 +" + quest.reward + " G / EXP +" + heroExperienceGained + "  ·  길드 금고 +" + quest.guildCommission + " G / 길드 EXP +" + quest.guildExperienceReward + "  ·  호감도 +" + affinityGained);
            if (AffinityStage(guest) >= 2 && !guest.guildMember)
            {
                TryRecruitGuest(guest);
                notice = guest.guildMember
                    ? guest.name + "이(가) 의뢰 성공! 용사 " + quest.reward + " G · 길드 수수료 " + quest.guildCommission + " G를 얻고 길드에 가입했어요."
                    : guest.name + "이(가) 의뢰 성공! 숙소가 가득 차 가입은 보류됐습니다.";
            }
            else notice = guest.name + "이(가) 의뢰 성공! 용사 " + quest.reward + " G · 길드 수수료 " + quest.guildCommission + " G · 경험치 " + quest.heroExperienceReward + "을 얻었습니다.";
        }
        else
        {
            int previousAffinity = guest.affinity;
            guest.affinity = Mathf.Max(0, guest.affinity - 5);
            ShowToast(guest.name + " 의뢰 실패  ·  호감도 " + (guest.affinity - previousAffinity));
            notice = guest.name + "이(가) 의뢰에 실패해 호감도가 내려갔어요. 기한이 남아 있으면 다음 날 다시 맡길 수 있어요.";
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
