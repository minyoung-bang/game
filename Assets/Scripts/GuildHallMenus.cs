using UnityEngine;

public sealed partial class GuildHallRuntime
{
    // 0: closed, 1: settings, 2: inventory, 3: members, 4: guild information, 5: upgrades.
    private int managementPage;
    private int inventoryPage;
    private int textSizeStep;
    private float gameVolume = 1f;
    private Vector2 inventoryScrollPosition;

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
            Event.current.Use();
        }

        string[] titles = { "설정", "길드 업그레이드", "인벤토리", "길드원", "길드정보" };
        int[] pages = { 1, 5, 2, 3, 4 };
        for (int i = 0; i < titles.Length; i++)
        {
            int page = pages[i];
            if (GUI.Button(new Rect(920 + i * 192, 34, 180, 62), titles[i], managementPage == page ? selectedTabStyle : buttonStyle))
                managementPage = managementPage == page ? 0 : page;
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
        GUI.Label(new Rect(284, 742, 1280, 72), "게임 진행 상황 저장 기능은 추후 추가할 예정입니다.", labelStyle);
    }

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
                GUI.Label(new Rect(x + 26, y + 102, 396, 54), "보유  " + materials[i] + "개" + (isDaytime ? "   ·   오늘 남은 공급 " + dailySupply[i] : ""), labelStyle);
            }
            GUI.EndScrollView();
        }
        else
        {
            ShopItem[] items = inventory[inventoryPage - 1];
            for (int i = 0; i < items.Length; i++)
            {
                float x = 246 + i % 3 * 480;
                float y = 358 + i / 3 * 220;
                GUI.Box(new Rect(x, y, 452, 180), "", panelStyle);
                GUI.Label(new Rect(x + 26, y + 22, 396, 60), items[i].name, headingStyle);
                GUI.Label(new Rect(x + 26, y + 100, 396, 54), "판매 재고 " + items[i].stock + "개   ·   가격 " + items[i].price + " G", labelStyle);
            }
            GUI.Label(new Rect(254, 680, 1350, 80), "재고는 낮에 제작하고, 밤에 판매하면 차감됩니다.", labelStyle);
        }
    }

    private void DrawMembersPage()
    {
        int memberCount = 0;
        for (int i = 0; i < guests.Count; i++)
        {
            Guest guest = guests[i];
            if (!guest.guildMember) continue;
            float x = 246 + (memberCount % 2) * 730;
            float y = 264 + (memberCount / 2) * 214;
            GUI.Box(new Rect(x, y, 696, 190), "", panelStyle);
            DrawGuestPortrait(new Rect(x + 16, y + 16, 144, 158), i);
            GUI.Label(new Rect(x + 178, y + 20, 488, 44), guest.name + " · " + guest.role, headingStyle);
            GUI.Label(new Rect(x + 178, y + 70, 488, 44), "레벨 " + guest.level + "   호감도 " + guest.affinity + "/5", labelStyle);
            GUI.Label(new Rect(x + 178, y + 116, 488, 50), "보유 골드 " + guest.gold + " G   의뢰 경험 " + guest.experience, labelStyle);
            memberCount++;
        }
        if (memberCount == 0) GUI.Label(new Rect(256, 340, 1300, 60), "아직 길드에 소속된 용사가 없습니다.", headingStyle);
        GUI.Label(new Rect(246, 876, 1420, 46), "길드원 " + memberCount + "명 · 방문 용사의 호감도가 3이 되면 영입됩니다.", labelStyle);
    }

    private void DrawGuildInformationPage()
    {
        int members = 0;
        foreach (Guest guest in guests) if (guest.guildMember) members++;
        GUI.Box(new Rect(246, 270, 690, 530), "", panelStyle);
        GUI.Box(new Rect(980, 270, 690, 530), "", panelStyle);
        GUI.Label(new Rect(284, 306, 620, 58), "길드 레벨 " + guildLevel, headingStyle);
        GUI.Label(new Rect(284, 386, 620, 58), "레벨 경험치 " + guildExperience + " / " + guildLevel * 20, labelStyle);
        GUI.Label(new Rect(284, 456, 620, 58), "금고 " + guildGold + " G", labelStyle);
        GUI.Label(new Rect(284, 526, 620, 58), "현재 DAY " + day.ToString("00") + (isDaytime ? " · 낮" : " · 밤"), labelStyle);
        GUI.Label(new Rect(1018, 306, 620, 58), "길드 기록", headingStyle);
        GUI.Label(new Rect(1018, 386, 620, 58), "소속 용사 " + members + "명", labelStyle);
        GUI.Label(new Rect(1018, 456, 620, 58), "총 판매 " + totalSales + "건", labelStyle);
        GUI.Label(new Rect(1018, 526, 620, 58), "간단 의뢰 성공 " + successfulRequests + "건", labelStyle);
        GUI.Label(new Rect(284, 702, 1300, 58), "간단 의뢰에 성공하면 길드 경험치가 올라갑니다.", labelStyle);
        GUI.Label(new Rect(254, 842, 1380, 66), "새 용사·상품·의뢰 해금과 마왕 토벌 목표는 후속 개발 단계입니다.", labelStyle);
    }
}
