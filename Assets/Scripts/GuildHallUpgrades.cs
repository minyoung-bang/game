using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    // Upgrade costs are the gold price of moving from the current index to the next level.
    // Kept separate from effects so a balance pass can tune either without changing the UI.
    private readonly int[] facilityLevels = new int[4];
    private readonly string[] facilityNames = { "길드 확장", "숙소 확장", "길드 홍보", "훈련장 확장" };
    private readonly int[][] facilityCosts =
    {
        new[] { 35, 75, 135, 220, 340 },
        new[] { 30, 65, 115, 190, 290 },
        new[] { 35, 75, 130, 210, 320 },
        new[] { 30, 65, 110, 180, 275 }
    };

    private int GuildMemberCount()
    {
        int count = 0;
        foreach (Guest guest in guests) if (guest.guildMember) count++;
        return count;
    }

    private int NightVisitorCapacity(int level = -1) => 4 + (level < 0 ? facilityLevels[0] : level);
    private int GuildMemberCapacity(int level = -1) => 2 + (level < 0 ? facilityLevels[1] : level);
    private float HighLevelVisitorChance(int level = -1) => .25f + (level < 0 ? facilityLevels[2] : level) * .08f;
    private int TrainingExperienceReward(int level = -1) => 4 + (level < 0 ? facilityLevels[3] : level) * 2;

    private int[] SelectEveningRoster()
    {
        var roster = new List<int>();
        for (int i = 0; i < guests.Count && roster.Count < GuildHallPresentation.GuestCount; i++)
            if (guests[i].guildMember) roster.Add(i);

        var visitors = new List<int>();
        for (int i = 0; i < guests.Count; i++) if (!guests[i].guildMember) visitors.Add(i);
        int capacity = Mathf.Min(NightVisitorCapacity(), GuildHallPresentation.GuestCount);
        while (roster.Count < capacity && visitors.Count > 0)
        {
            var highLevel = new List<int>();
            var other = new List<int>();
            foreach (int index in visitors)
                if (guests[index].level >= 3) highLevel.Add(index); else other.Add(index);

            bool chooseHigh = highLevel.Count > 0 && (other.Count == 0 || Random.value < HighLevelVisitorChance());
            List<int> pool = chooseHigh ? highLevel : other.Count > 0 ? other : highLevel;
            int selected = pool[Random.Range(0, pool.Count)];
            roster.Add(selected);
            visitors.Remove(selected);
        }
        return roster.ToArray();
    }

    private void DrawGuildUpgradePage()
    {
        GUI.Label(new Rect(246, 218, 1420, 46), "골드를 투자해 길드 시설을 키웁니다. 각 시설은 0레벨에서 시작하며 최대 5레벨입니다.", labelStyle);
        GUI.Label(new Rect(246, 266, 1420, 42), "길드 금고  " + guildGold + " G     현재 소속 용사 " + GuildMemberCount() + "/" + GuildMemberCapacity() + "명", headingStyle);

        for (int i = 0; i < facilityNames.Length; i++)
        {
            float x = i % 2 == 0 ? 246 : 976;
            float y = i < 2 ? 326 : 622;
            Rect card = new Rect(x, y, 696, 270);
            GUI.Box(card, "", panelStyle);
            int level = facilityLevels[i];
            GUI.Label(new Rect(x + 28, y + 18, 430, 48), facilityNames[i] + "  ·  Lv. " + level + "/5", headingStyle);
            GUI.Label(new Rect(x + 28, y + 74, 638, 88), FacilityEffectText(i, level), labelStyle);
            string nextEffect = level >= 5 ? "최고 레벨입니다." : "다음 레벨 효과: " + FacilityEffectText(i, level + 1);
            GUI.Label(new Rect(x + 28, y + 158, 638, 52), nextEffect, labelStyle);

            if (level >= 5)
            {
                GUI.enabled = false;
                GUI.Button(new Rect(x + 412, y + 206, 248, 52), "최대 레벨", buttonStyle);
                GUI.enabled = true;
            }
            else
            {
                int cost = facilityCosts[i][level];
                GUI.Label(new Rect(x + 28, y + 210, 330, 44), "업그레이드 비용  " + cost + " G", labelStyle);
                GUI.enabled = guildGold >= cost;
                string button = guildGold >= cost ? "업그레이드" : "골드 부족";
                if (GUI.Button(new Rect(x + 412, y + 206, 248, 52), button, buttonStyle)) PurchaseFacilityUpgrade(i);
                GUI.enabled = true;
            }
        }
        GUI.Label(new Rect(246, 906, 1400, 36), "현재 훈련장 레벨은 하루 훈련 경험치에 반영됩니다. 방문 인원은 보유 캐릭터 리소스(최대 6명) 범위에서 표시됩니다.", labelStyle);
    }

    private string FacilityEffectText(int facility, int level)
    {
        switch (facility)
        {
            case 0: return "밤 방문 수용 인원  " + NightVisitorCapacity(level) + "명";
            case 1: return "길드 소속 용사 수용 인원  " + GuildMemberCapacity(level) + "명";
            case 2: return "고레벨 용사 방문 확률  " + Mathf.RoundToInt(HighLevelVisitorChance(level) * 100) + "%  (레벨당 +8%p)";
            default: return "용사 1회 훈련 경험치  " + TrainingExperienceReward(level) + " EXP";
        }
    }

    private void PurchaseFacilityUpgrade(int index)
    {
        if (index < 0 || index >= facilityLevels.Length) return;
        int level = facilityLevels[index];
        if (level >= 5) { dayNotice = "이미 최대 레벨입니다."; return; }
        int cost = facilityCosts[index][level];
        if (guildGold < cost) { dayNotice = "길드 금고에 골드가 부족합니다."; return; }
        guildGold -= cost;
        facilityLevels[index]++;
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
        dayNotice = guest.name + "이(가) 길드에 가입했습니다!";
    }
}
