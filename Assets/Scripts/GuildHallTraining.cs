using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    private int trainingTab;
    private readonly HashSet<Guest> trainedToday = new HashSet<Guest>();
    private Vector2 trainingScrollPosition;

    private static string[] SkillsFor(Guest guest)
    {
        if (guest.role.Contains("마도사")) return new[] { "화염구", "마력 방벽", "번개 사슬" };
        if (guest.role.Contains("검사") || guest.role.Contains("기사")) return new[] { "강타", "방패 자세", "연속 베기" };
        return new[] { "정밀 사격", "회피 사격", "급소 조준" };
    }

    private void DrawTrainingHall()
    {
        if (GUI.Button(new Rect(68, 344, 420, 56), "용사 훈련", trainingTab == 0 ? selectedTabStyle : tabStyle)) trainingTab = 0;
        if (GUI.Button(new Rect(504, 344, 420, 56), "스킬 관리", trainingTab == 1 ? selectedTabStyle : tabStyle)) trainingTab = 1;
        GUI.Label(new Rect(68, 408, 1768, 54), trainingTab == 0
            ? "길드 소속 용사만 이용할 수 있습니다. 훈련은 용사별 하루 1회이며, 경험치가 쌓이면 레벨이 오릅니다."
            : "길드 소속 용사의 전투 기술을 선택합니다. 현재는 선택 내용을 저장하며 실제 전투 효과는 전투 시스템 개발 때 연결됩니다.", labelStyle);

        Rect view = new Rect(68, 476, 1768, 420);
        int memberCount = GuildMemberCount();
        trainingScrollPosition = GUI.BeginScrollView(view, trainingScrollPosition, new Rect(0, 0, 1728, Mathf.Max(420, memberCount * 132)), false, true);
        int row = 0;
        for (int i = 0; i < guests.Count; i++)
        {
            Guest guest = guests[i];
            if (!guest.guildMember) continue;
            float y = row++ * 132;
            Rect card = new Rect(0, y, 1696, 116);
            GUI.Box(card, "", panelStyle);
            GUI.Label(new Rect(24, y + 12, 500, 42), guest.name + " · " + guest.role, headingStyle);

            if (trainingTab == 0)
            {
                int threshold = TrainingExperienceToNextLevel(guest.level);
                GUI.Label(new Rect(24, y + 58, 830, 42), "레벨 " + guest.level + "   다음 레벨까지 " + guest.trainingExperience + "/" + threshold + " EXP", labelStyle);
                bool trained = trainedToday.Contains(guest);
                GUI.enabled = !trained && guest.level < 20;
                string button = guest.level >= 20 ? "최고 레벨" : trained ? "오늘 훈련 완료" : "훈련하기  ·  +" + TrainingExperienceReward() + " EXP";
                if (GUI.Button(new Rect(1248, y + 26, 416, 64), button, buttonStyle)) TrainGuest(guest);
                GUI.enabled = true;
            }
            else
            {
                string[] skills = SkillsFor(guest);
                guest.skillIndex = Mathf.Clamp(guest.skillIndex, 0, skills.Length - 1);
                GUI.Label(new Rect(24, y + 58, 1000, 42), "장착 기술  ·  " + skills[guest.skillIndex], labelStyle);
                if (GUI.Button(new Rect(1248, y + 26, 416, 64), "기술 변경  →", buttonStyle))
                    guest.skillIndex = (guest.skillIndex + 1) % skills.Length;
            }
        }
        GUI.EndScrollView();
        if (memberCount == 0) GUI.Label(new Rect(92, 526, 1540, 80), "길드 소속 용사가 없습니다. 밤에 방문 용사를 영입한 뒤 이용해 주세요.", headingStyle);
        GUI.Label(new Rect(68, 912, 1680, 44), trainingTab == 0
            ? "훈련 효율: 시설 레벨 " + facilityLevels[3] + " · 1회 훈련 " + TrainingExperienceReward() + " EXP"
            : "기술 선택은 용사별로 저장됩니다.", labelStyle);
    }

    private int TrainingExperienceToNextLevel(int level) => Mathf.Max(1, level * 3 + 5);

    private void TrainGuest(Guest guest)
    {
        if (guest == null || !guest.guildMember || guest.level >= 20 || trainedToday.Contains(guest)) return;
        trainedToday.Add(guest);
        guest.trainingExperience += TrainingExperienceReward();
        int levelsGained = 0;
        while (guest.level < 20 && guest.trainingExperience >= TrainingExperienceToNextLevel(guest.level))
        {
            guest.trainingExperience -= TrainingExperienceToNextLevel(guest.level);
            guest.level++;
            levelsGained++;
        }
        dayNotice = levelsGained > 0
            ? guest.name + "이(가) 훈련으로 레벨 " + guest.level + "이(가) 되었습니다!"
            : guest.name + "이(가) 훈련 경험치 " + TrainingExperienceReward() + "을(를) 얻었습니다.";
    }
}
