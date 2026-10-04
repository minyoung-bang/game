using UnityEngine;

public sealed partial class GuildHallRuntime
{
    // 0: closed, 1: settings, 2: inventory, 3: members, 4: guild information, 5: upgrades.
    private int managementPage;
    private int inventoryPage;
    private int textSizeStep;
    private float gameVolume = 1f;
    private Vector2 inventoryScrollPosition;
#if UNITY_EDITOR
    private bool developmentToolsOpen;
#endif

    private void LoadInterfaceSettings()
    {
        textSizeStep = Mathf.Clamp(PlayerPrefs.GetInt("GuildHallTextSize", 0), 0, 2);
        gameVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("GuildHallVolume", 1f));
        AudioListener.volume = gameVolume;
    }

    private void ApplyTextSize()
    {
        if (headingStyle == null) return;
        headingStyle.fontSize = 26 + textSizeStep * 3;
        labelStyle.fontSize = 21 + textSizeStep * 3;
        buttonStyle.fontSize = 22 + textSizeStep * 2;
        tabStyle.fontSize = buttonStyle.fontSize;
        selectedTabStyle.fontSize = buttonStyle.fontSize;
    }

    private void DrawManagementToolbar()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && managementPage != 0)
        {
            managementPage = 0;
#if UNITY_EDITOR
            developmentToolsOpen = false;
#endif
            Event.current.Use();
        }

        if (!isDaytime && GUI.Button(new Rect(728, 34, 180, 62), "지도", buttonStyle))
        {
            managementPage = 0;
            worldMapOpen = true;
            selectedWorldRegion = -1;
            selectedWorldMember = -1;
            worldMapNotice = "지역을 선택하거나 길드원을 눌러 정보를 확인하세요.";
        }

        string[] titles = { "설정", "길드 업그레이드", "인벤토리", "길드원", "길드정보" };
        int[] pages = { 1, 5, 2, 3, 4 };
        for (int i = 0; i < titles.Length; i++)
        {
            int page = pages[i];
            if (GUI.Button(new Rect(920 + i * 192, 34, 180, 62), titles[i], managementPage == page ? selectedTabStyle : buttonStyle))
            {
                managementPage = managementPage == page ? 0 : page;
#if UNITY_EDITOR
                developmentToolsOpen = false;
#endif
            }
        }
    }

    private void DrawManagementPage()
    {
        PixelRect(new Rect(0, 112, 1920, 968), new Color(.025f, .03f, .06f, .96f));
        GUI.Box(new Rect(190, 136, 1540, 812), "", panelStyle);
        string[] titles = { "", "설정", "인벤토리", "길드원", "길드정보", "길드 업그레이드" };
        GUI.Label(new Rect(244, 158, 1200, 70), titles[managementPage], headingStyle);
        if (GUI.Button(new Rect(1620, 158, 72, 62), "×", buttonStyle))
        {
            managementPage = 0;
#if UNITY_EDITOR
            developmentToolsOpen = false;
#endif
            return;
        }
        if (managementPage == 1) DrawSettingsPage();
        else if (managementPage == 2) DrawInventoryPage();
        else if (managementPage == 3) DrawMembersPage();
        else if (managementPage == 4) DrawGuildInformationPage();
        else DrawGuildUpgradePage();
    }

    private void DrawSettingsPage()
    {
#if UNITY_EDITOR
        if (developmentToolsOpen) { DrawDevelopmentToolsPage(); return; }
#endif
        GUI.Box(new Rect(246, 260, 1428, 594), "", panelStyle);
        GUI.Label(new Rect(284, 292, 940, 50), "화면", headingStyle);
        if (GUI.Button(new Rect(1220, 288, 396, 60), Screen.fullScreen ? "전체 화면 끄기" : "전체 화면 켜기", buttonStyle))
            Screen.fullScreen = !Screen.fullScreen;

        GUI.Label(new Rect(284, 402, 800, 50), "글자 크기 · " + (textSizeStep == 0 ? "기본" : textSizeStep == 1 ? "크게" : "아주 크게"), headingStyle);
        if (GUI.Button(new Rect(1208, 398, 180, 60), "−", buttonStyle)) SetTextSize(textSizeStep - 1);
        if (GUI.Button(new Rect(1436, 398, 180, 60), "+", buttonStyle)) SetTextSize(textSizeStep + 1);

        GUI.Label(new Rect(284, 516, 810, 50), "게임 음량 · " + Mathf.RoundToInt(gameVolume * 100) + "%", headingStyle);
        float nextVolume = GUI.HorizontalSlider(new Rect(1120, 537, 490, 30), gameVolume, 0f, 1f);
        if (Mathf.Abs(nextVolume - gameVolume) > .001f)
        {
            gameVolume = nextVolume;
            AudioListener.volume = gameVolume;
            PlayerPrefs.SetFloat("GuildHallVolume", gameVolume);
            PlayerPrefs.Save();
        }

        GUI.Label(new Rect(284, 644, 1280, 72), "설정은 다음 실행에도 유지됩니다. 음악과 효과음은 아직 제작 중입니다.", labelStyle);
        GUI.Label(new Rect(284, 742, 850, 72), "게임 진행 상황 저장 기능은 추후 추가할 예정입니다.", labelStyle);
#if UNITY_EDITOR
        if (GUI.Button(new Rect(1190, 736, 420, 72), "개발용 테스트 도구", buttonStyle))
            developmentToolsOpen = true;
#endif
    }

#if UNITY_EDITOR
    private void DrawDevelopmentToolsPage()
    {
        GUI.Box(new Rect(246, 260, 1428, 594), "", panelStyle);
        GUI.Label(new Rect(284, 280, 1320, 48), "개발용 테스트 · 에디터에서만 표시", headingStyle);
        GUI.Label(new Rect(284, 345, 900, 54), "길드 Lv." + guildLevel + "/10 · 경험치 " + guildExperience + "/" + GuildExperienceRequired(guildLevel), headingStyle);
        GUI.enabled = guildLevel < 10;
        if (GUI.Button(new Rect(1260, 336, 350, 62), "길드 레벨 +1", buttonStyle))
        {
            AddGuildExperience(GuildExperienceRequired(guildLevel) - guildExperience);
            ShowToast("테스트 · 길드 레벨 " + guildLevel + " 달성");
        }
        GUI.enabled = true;

        GUI.Label(new Rect(284, 414, 1050, 42), "용사 호감도 · 버튼마다 1레벨 증가 (최대 5)", headingStyle);
        for (int i = 0; i < guests.Count; i++)
        {
            Guest guest = guests[i];
            int stage = AffinityStage(guest);
            float y = 462 + i * 58;
            GUI.Box(new Rect(284, y, 1326, 52), "", cardStyle);
            GUI.Label(new Rect(304, y + 7, 940, 40), guest.name + " · " + guest.baseClass + " · 호감도 " + stage + "/5 · " +
                (guest.guildMember ? "길드원" : guest.hasVisited ? "방문 이력 있음" : "미방문"), labelStyle);
            GUI.enabled = stage < 5;
            if (GUI.Button(new Rect(1260, y + 3, 340, 46), "호감도 +1레벨", buttonStyle))
            {
                guest.affinity = AffinityThresholdForStage(stage + 1);
                if (guest.hasVisited && !guest.guildMember && AffinityStage(guest) >= 2)
                    TryRecruitGuest(guest);
                ShowToast("테스트 · " + guest.name + " 호감도 " + AffinityStage(guest) + "레벨");
            }
            GUI.enabled = true;
        }
        GUI.Label(new Rect(284, 814, 1280, 34), "미방문 용사는 방문한 뒤, 호감도 2레벨 이상이면 숙소 정원에 따라 영입됩니다.", labelStyle);
        if (GUI.Button(new Rect(284, 864, 270, 58), "설정으로 돌아가기", tabStyle)) developmentToolsOpen = false;
        if (GUI.Button(new Rect(1190, 864, 420, 58), "개발용 전투 테스트", buttonStyle))
        {
            developmentToolsOpen = false;
            managementPage = 0;
            combatTestSetupOpen = true;
        }
    }
#endif

    private void SetTextSize(int value)
    {
        int next = Mathf.Clamp(value, 0, 2);
        if (next == textSizeStep) return;
        textSizeStep = next;
        ApplyTextSize();
        PlayerPrefs.SetInt("GuildHallTextSize", textSizeStep);
        PlayerPrefs.Save();
    }

    private void DrawInventoryPage()
    {
        string[] tabs = { "재료", "음식", "무기", "방어구" };
        for (int i = 0; i < tabs.Length; i++)
            if (GUI.Button(new Rect(246 + i * 352, 256, 332, 62), tabs[i], inventoryPage == i ? selectedTabStyle : tabStyle))
                inventoryPage = i;

        if (inventoryPage == 0)
        {
            Rect view = new Rect(228, 338, 1480, 600);
            inventoryScrollPosition = GUI.BeginScrollView(view, inventoryScrollPosition, new Rect(0, 0, 1440, 6 * 220));
            for (int i = 0; i < materialNames.Length; i++)
            {
                float x = 12 + i % 3 * 480;
                float y = 12 + i / 3 * 220;
                GUI.Box(new Rect(x, y, 452, 180), "", panelStyle);
                GUI.Label(new Rect(x + 26, y + 24, 396, 60), materialNames[i], headingStyle);
                string materialStatus = guildLevel >= materialUnlockLevels[i]
                    ? "보유  " + materials[i] + "개"
                    : "보유  " + materials[i] + "개 · 길드 레벨 " + materialUnlockLevels[i] + " 해금";
                GUI.Label(new Rect(x + 26, y + 102, 396, 54), materialStatus, labelStyle);
            }
            GUI.EndScrollView();
        }
        else
        {
            ShopItem[] items = inventory[inventoryPage - 1];
            Rect view = new Rect(228, 338, 1480, 600);
            inventoryScrollPosition = GUI.BeginScrollView(view, inventoryScrollPosition, new Rect(0, 0, 1440, Mathf.Max(600, Mathf.CeilToInt(items.Length / 4f) * 190)));
            for (int i = 0; i < items.Length; i++)
            {
                float x = 8 + i % 4 * 356;
                float y = 8 + i / 4 * 190;
                GUI.Box(new Rect(x, y, 340, 172), "", panelStyle);
                DrawPixelItemIcon(new Rect(x + 10, y + 26, 100, 116), items[i].icon);
                GUI.Label(new Rect(x + 122, y + 26, 206, 52), items[i].name, headingStyle);
                GUI.Label(new Rect(x + 122, y + 86, 206, 58), "판매 재고 " + items[i].stock + "개\n가격 " + items[i].price + " G", labelStyle);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(254, 680, 1350, 80), "재고는 밤에 길드 작업장에서 제작하고, 판매하면 차감됩니다. 새날에 자동 보충되지 않습니다.", labelStyle);
        }
    }

    private Vector2 memberListScroll;
    private void DrawMembersPage()
    {
        int memberCount = 0;
        memberListScroll = GUI.BeginScrollView(new Rect(246,264,1440,592),memberListScroll,
            new Rect(0,0,1410,Mathf.Max(592,Mathf.CeilToInt(GuildMemberCount()/2f)*214)),false,true);
        for (int i = 0; i < guests.Count; i++)
        {
            Guest guest = guests[i];
            if (!guest.guildMember) continue;
            float x = (memberCount % 2) * 710;
            float y = (memberCount / 2) * 214;
            GUI.Box(new Rect(x, y, 696, 190), "", panelStyle);
            DrawGuestPortrait(new Rect(x + 16, y + 16, 144, 158), i);
            GUI.Label(new Rect(x + 178, y + 20, 488, 44), guest.name + " · " + guest.baseClass, headingStyle);
            GUI.Label(new Rect(x + 178, y + 70, 488, 38), "레벨 " + guest.level + "   호감도 " + AffinityStage(guest) + "/5", labelStyle);
            GUI.Label(new Rect(x + 178, y + 108, 488, 36), "보유 골드 " + guest.gold + " G   경험치 " + guest.trainingExperience + "/" + HeroExperienceRequired(guest.level), labelStyle);
            GUI.Label(new Rect(x + 178, y + 148, 488, 34), HeroStatsLabel(guest), labelStyle);
            memberCount++;
        }
        GUI.EndScrollView();
        if (memberCount == 0) GUI.Label(new Rect(256, 340, 1300, 60), "아직 길드에 소속된 용사가 없습니다.", headingStyle);
        GUI.Label(new Rect(246, 876, 1420, 46), "길드원 " + memberCount + "명 · 호감도 2단계(누적 50점)에 도달하면 영입됩니다.", labelStyle);
    }

    private void DrawGuildInformationPage()
    {
        int members = 0;
        foreach (Guest guest in guests) if (guest.guildMember) members++;
        GUI.Box(new Rect(246, 270, 690, 530), "", panelStyle);
        GUI.Box(new Rect(980, 270, 690, 530), "", panelStyle);
        GUI.Label(new Rect(284, 306, 620, 58), "길드 레벨 " + guildLevel, headingStyle);
        GUI.Label(new Rect(284, 386, 620, 58), "레벨 경험치 " + guildExperience + " / " + GuildExperienceRequired(guildLevel), labelStyle);
        GUI.Label(new Rect(284, 456, 620, 58), "금고 " + guildGold + " G", labelStyle);
        GUI.Label(new Rect(284, 526, 620, 58), "현재 DAY " + day.ToString("00") + (isDaytime ? " · 낮" : " · 밤"), labelStyle);
        GUI.Label(new Rect(1018, 306, 620, 58), "길드 기록", headingStyle);
        GUI.Label(new Rect(1018, 386, 620, 58), "소속 용사 " + members + "명", labelStyle);
        GUI.Label(new Rect(1018, 456, 620, 58), "총 판매 " + totalSales + "건", labelStyle);
        GUI.Label(new Rect(1018, 526, 620, 58), "간단 의뢰 성공 " + successfulRequests + "건", labelStyle);
        GUI.Label(new Rect(284, 702, 1300, 58), "간단 의뢰에 성공하면 길드 경험치가 올라갑니다.", labelStyle);
        GUI.Label(new Rect(254, 842, 1380, 66), "좌석 " + NightVisitorCapacity() + "석 · 숙소 " + GuildMemberCount() + "/" + GuildMemberCapacity() + "명 · 평균 방문 수요 " + ((5 + guildLevel) * VisitorChance()).ToString("0.##") + "명 (좌석 한도 적용 전)", labelStyle);
    }
}
