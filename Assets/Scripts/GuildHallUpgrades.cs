using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    // Upgrade costs are the gold price of moving from the current index to the next level.
    // Kept separate from effects so a balance pass can tune either without changing the UI.
    private readonly int[] facilityLevels = new int[5];
    private readonly int[] facilityMaxLevels = { 4, 5, 5, 5, 5 };
    private readonly string[] facilityNames = { "길드 확장", "숙소 확장", "길드 홍보", "훈련장 확장", "행동력 확장" };
    private readonly int[][] facilityCosts =
    {
        new[] { 100, 200, 350, 550 },
        new[] { 80, 150, 260, 420, 650 },
        new[] { 100, 220, 380, 600, 900 },
        new[] { 90, 180, 320, 520, 800 },
        new[] { 120, 250, 430, 680, 1000 }
    };

    private int GuildMemberCount()
    {
        int count = 0;
        foreach (Guest guest in guests) if (guest.guildMember) count++;
        return count;
    }

    private int NightVisitorCapacity(int level = -1) => 2 + Mathf.Clamp(level < 0 ? facilityLevels[0] : level, 0, 4);
    private int GuildMemberCapacity(int level = -1) => 4 + 2 * (level < 0 ? facilityLevels[1] : level);
    private float VisitorChance(int level = -1) => .25f + (level < 0 ? facilityLevels[2] : level) * .03f;
    private float TrainingExperienceReward(int level = -1) => 4f * (1f + (level < 0 ? facilityLevels[3] : level) * .10f);
    private int DailyActionCapacity() => 2 + (guildLevel - 1) / 2 + facilityLevels[4];


    private void DrawGuildUpgradePage()
    {
        GUI.Label(new Rect(246, 218, 1420, 46), "골드를 투자해 길드 시설을 키웁니다. 길드 확장은 최대 4레벨(6석), 다른 시설은 최대 5레벨입니다.", labelStyle);
        GUI.Label(new Rect(246, 266, 1420, 42), "길드 금고  " + guildGold + " G     현재 소속 용사 " + GuildMemberCount() + "/" + GuildMemberCapacity() + "명", headingStyle);

        for (int i = 0; i < facilityNames.Length; i++)
        {
            float x = i == 4 ? 612 : i % 2 == 0 ? 246 : 976;
            float y = i == 4 ? 752 : 316 + (i / 2) * 218;
            Rect card = new Rect(x, y, 696, 206);
            GUI.Box(card, "", panelStyle);
            int level = facilityLevels[i];
            int maxLevel = facilityMaxLevels[i];
            GUI.Label(new Rect(x + 28, y + 10, 638, 46), facilityNames[i] + "  ·  Lv. " + level + "/" + maxLevel, headingStyle);
            GUI.Label(new Rect(x + 28, y + 56, 638, 48), FacilityEffectText(i, level), labelStyle);
            string nextEffect = level >= maxLevel ? "최고 레벨입니다." : "다음 레벨 효과: " + FacilityEffectText(i, level + 1);
            GUI.Label(new Rect(x + 28, y + 105, 638, 44), nextEffect, labelStyle);

            if (level >= maxLevel)
            {
                GUI.enabled = false;
                GUI.Button(new Rect(x + 412, y + 154, 248, 44), "최대 레벨", buttonStyle);
                GUI.enabled = true;
            }
            else
            {
                int cost = facilityCosts[i][level];
                GUI.Label(new Rect(x + 28, y + 154, 360, 44), "업그레이드 비용  " + cost + " G", labelStyle);
                GUI.enabled = guildGold >= cost && guildLevel >= level + 1;
                string button = guildLevel < level + 1 ? "길드 Lv." + (level + 1) + " 필요" : guildGold >= cost ? "업그레이드" : "골드 부족";
                if (GUI.Button(new Rect(x + 412, y + 154, 248, 44), button, buttonStyle)) PurchaseFacilityUpgrade(i);
                GUI.enabled = true;
            }
        }
        GUI.Label(new Rect(246, 970, 1420, 46), "기본 행동력 2~6회 + 시설 레벨당 1회. 행동력 증가는 다음 날부터 적용됩니다. 홍보는 방문 인원에만 영향을 줍니다.", labelStyle);
    }

    private string FacilityEffectText(int facility, int level)
    {
        switch (facility)
        {
            case 0: return "밤 방문 수용 인원  " + NightVisitorCapacity(level) + "명";
            case 1: return "길드 소속 용사 수용 인원  " + GuildMemberCapacity(level) + "명";
            case 2: return "방문 수요 확률  " + Mathf.RoundToInt(VisitorChance(level) * 100) + "%  (레벨당 +3%p)";
            case 3: return "훈련 경험치 +" + (level * 10) + "% · 1회 " + TrainingExperienceReward(level).ToString("0.#") + " EXP";
            default: return "하루 행동 포인트 추가  " + level + "회";
        }
    }

    private void PurchaseFacilityUpgrade(int index)
    {
        if (index < 0 || index >= facilityLevels.Length) return;
        int level = facilityLevels[index];
        if (level >= facilityMaxLevels[index]) { dayNotice = "이미 최대 레벨입니다."; return; }
        if (guildLevel < level + 1) { dayNotice = "길드 레벨이 부족합니다."; return; }
        int cost = facilityCosts[index][level];
        if (guildGold < cost) { dayNotice = "길드 금고에 골드가 부족합니다."; return; }
        guildGold -= cost;
        facilityLevels[index]++;
        if (index == 1)
            foreach (Guest guest in guests)
                if (guest.hasVisited && !guest.guildMember && AffinityStage(guest) >= 2) TryRecruitGuest(guest);
        dayNotice = facilityNames[index] + "이(가) Lv. " + facilityLevels[index] + "로 확장되었습니다.";
    }

    private void TryRecruitGuest(Guest guest)
    {
        if (guest.guildMember) return;
        if (GuildMemberCount() >= GuildMemberCapacity())
        {
            dayNotice = guest.name + "의 호감도는 충분하지만 숙소가 가득 찼습니다. 숙소를 확장해 주세요.";
            return;
        }
        guest.guildMember = true;
        recruitmentPopupGuest = guests.IndexOf(guest);
        dayNotice = guest.name + "이(가) 길드에 가입했습니다!";
    }
}
