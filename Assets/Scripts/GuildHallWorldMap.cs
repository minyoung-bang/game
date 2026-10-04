using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    private bool worldMapOpen;
    private int selectedWorldRegion = -1;
    private int selectedWorldMember = -1;
    private string worldMapNotice = "";
    private Vector2 worldMemberScroll;
    private Vector2 worldRegionScroll;
    private readonly HashSet<string> unlockedWorldRegions = new HashSet<string>();
    private static readonly string[] WorldRegionNames =
    { "깊은 숲", "안개 늪", "고대 폐허", "바람의 협곡", "폐탑", "얼어붙은 고개", "마왕군 변경 요새" };
    private static readonly string[] WorldRegionFeatures =
    {
        "길을 잃기 쉬운 숲. 길목마다 마물의 터전이 있습니다.",
        "시야가 짧고 발이 묶이기 쉬운 습지입니다.",
        "오래된 유적의 마력과 수호 장치가 움직입니다.",
        "절벽과 강풍이 이어진 길입니다.",
        "통제되지 않는 마법 장치에서 마력이 새어 나옵니다.",
        "눈보라 속의 좁은 고갯길입니다.",
        "마왕군이 국경 진입로를 감시하는 거점입니다."
    };
    private static readonly string[] WorldRegionMonsters =
    {
        "숲 슬라임 · 고블린 · 거대 거미", "독두꺼비 · 늪 괴물", "망령 · 해골 병사 · 유적 수호자",
        "하피 · 암석 도마뱀", "마도 인형 · 폭주한 마력체", "서리 정령 · 눈 골렘",
        "마왕군 정찰병 · 마왕군 지휘관"
    };
    private static readonly Rect[] WorldRegionRects =
    {
        new Rect(60, 70, 276, 128), new Rect(435, 56, 276, 128), new Rect(848, 88, 276, 128),
        new Rect(72, 342, 276, 128), new Rect(855, 350, 276, 128),
        new Rect(422, 592, 276, 128), new Rect(848, 588, 300, 128)
    };
    private static readonly Color[] WorldRegionColors =
    {
        new Color(.18f,.38f,.25f), new Color(.22f,.38f,.35f), new Color(.39f,.37f,.35f),
        new Color(.44f,.41f,.31f), new Color(.32f,.29f,.42f), new Color(.53f,.67f,.73f),
        new Color(.40f,.23f,.23f)
    };

    private BattleRequest AcceptedRegionRequest(int regionIndex)
    {
        if (regionIndex < 0 || regionIndex >= WorldRegionNames.Length) return null;
        foreach (BattleRequest request in battleRequests)
            if (request.acceptedDay >= 0 && request.region == WorldRegionNames[regionIndex]) return request;
        return null;
    }

    private bool WorldRegionVisible(int regionIndex)
        => unlockedWorldRegions.Contains(WorldRegionNames[regionIndex]) || AcceptedRegionRequest(regionIndex) != null;

    private void DrawWorldMap()
    {
        PixelRect(new Rect(0, 0, 1920, 1080), new Color(.014f, .022f, .036f, .96f));
        GUI.Label(new Rect(66, 48, 1300, 62), "길드 지도 · 출전 준비", headingStyle);
        GUI.Label(new Rect(66, 104, 1400, 38), "낮에 받은 전투 의뢰의 지역에 ! 표시가 나타납니다.", labelStyle);
        if (GUI.Button(new Rect(1788, 52, 70, 62), "×", buttonStyle) ||
            (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
        {
            worldMapOpen = false;
            Event.current.Use();
            return;
        }

        GUI.Box(new Rect(42, 164, 1050, 850), "", panelStyle);
        GUI.Box(new Rect(1114, 164, 764, 850), "", panelStyle);
        GUI.Label(new Rect(78, 180, 960, 48), "지역 지도", headingStyle);
        GUI.Label(new Rect(1150, 180, 650, 48), "길드원 목록 · 최대 4명", headingStyle);
        DrawWorldMapCanvas();

        BattleRequest request = AcceptedRegionRequest(selectedWorldRegion);
        if (request != null)
        {
            if (request.party.RemoveAll(i => i < 0 || i >= guests.Count || !guests[i].guildMember) > 0)
                request.prepared = false;
        }
        DrawWorldMapDetails(request);
        DrawWorldMemberList(request);
        DrawWorldMemberDetails();

        if (request != null)
        {
            GUI.enabled = request.party.Count >= 1 && request.party.Count <= 4;
            if (GUI.Button(new Rect(1502, 946, 335, 56), request.prepared ? "편성 다시 저장" : "편성 저장", buttonStyle))
            {
                request.prepared = true;
                worldMapOpen = false;
                ShowToast(request.party.Count + "명 편성 저장 완료 · 하루 종료 시 출전합니다.");
                GUI.enabled = true;
                return;
            }
            GUI.enabled = true;
        }
        GUI.Label(new Rect(1150, 912, 682, 34), worldMapNotice, labelStyle);
    }

    private void DrawWorldMapCanvas()
    {
        Rect canvas = new Rect(78, 236, 978, 574);
        worldRegionScroll = GUI.BeginScrollView(canvas, worldRegionScroll,
            new Rect(0, 0, 1230, 790), true, true);
        PixelRect(new Rect(0, 0, 1230, 790), new Color(.09f, .17f, .16f));
        Vector2 guildCenter = new Vector2(610, 410);
        for (int i = 0; i < WorldRegionNames.Length; i++)
            if (WorldRegionVisible(i)) DrawWorldRoad(guildCenter, WorldRegionRects[i].center);
        GUI.Box(new Rect(505, 354, 210, 112), "", cardStyle);
        PixelRect(new Rect(565, 370, 72, 54), new Color(.74f, .60f, .36f));
        PixelRect(new Rect(581, 353, 40, 20), new Color(.58f, .25f, .18f));
        GUI.Label(new Rect(517, 423, 184, 36), "우리 길드", headingStyle);
        for (int i = 0; i < WorldRegionNames.Length; i++)
            if (WorldRegionVisible(i)) DrawWorldRegion(i, WorldRegionRects[i], WorldRegionColors[i]);
        GUI.EndScrollView();
    }

    private void DrawWorldRoad(Vector2 start, Vector2 end)
    {
        Color road = new Color(.52f, .43f, .29f);
        float cornerX = (start.x + end.x) * .5f;
        PixelRect(new Rect(Mathf.Min(start.x, cornerX), start.y - 5, Mathf.Abs(cornerX - start.x) + 10, 10), road);
        PixelRect(new Rect(cornerX - 5, Mathf.Min(start.y, end.y), 10, Mathf.Abs(end.y - start.y) + 10), road);
        PixelRect(new Rect(Mathf.Min(cornerX, end.x), end.y - 5, Mathf.Abs(end.x - cornerX) + 10, 10), road);
    }

    private void DrawWorldRegion(int index, Rect rect, Color landmarkColor)
    {
        BattleRequest request = AcceptedRegionRequest(index);
        bool selected = selectedWorldRegion == index;
        GUI.Box(rect, "", selected ? selectedTabStyle : cardStyle);
        PixelRect(new Rect(rect.x + 18, rect.y + 22, 82, 72), landmarkColor);
        if (index == 0 || index == 1)
        {
            PixelRect(new Rect(rect.x + 30, rect.y + 8, 20, 102), landmarkColor);
            PixelRect(new Rect(rect.x + 59, rect.y + 2, 22, 105), landmarkColor);
        }
        else if (index == 2 || index == 3 || index == 6)
        {
            PixelRect(new Rect(rect.x + 22, rect.y + 8, 16, 100), landmarkColor);
            PixelRect(new Rect(rect.x + 76, rect.y + 8, 16, 100), landmarkColor);
        }
        else PixelRect(new Rect(rect.x + 46, rect.y + 3, 26, 95), landmarkColor);
        GUI.Label(new Rect(rect.x + 107, rect.y + 25, rect.width - 120, 45), WorldRegionNames[index], headingStyle);
        GUI.Label(new Rect(rect.x + 107, rect.y + 76, rect.width - 120, 40), request != null ? "의뢰 진행 중" : "아직 의뢰 없음", labelStyle);
        if (request != null)
        {
            GUI.Box(new Rect(rect.xMax - 43, rect.y - 12, 46, 46), "!", selectedTabStyle);
        }
        if (GUI.Button(rect, GUIContent.none, hoverStyle))
        {
            selectedWorldRegion = selected ? -1 : index;
            selectedWorldMember = -1;
            worldMapNotice = selectedWorldRegion < 0 ? "지역 선택을 해제했습니다." :
                request == null ? "이 지역에 수락한 전투 의뢰가 없습니다." :
                WorldRegionNames[index] + " · 출전할 길드원을 선택하세요.";
        }
    }

    private void DrawWorldMapDetails(BattleRequest request)
    {
        GUI.Box(new Rect(78, 825, 978, 160), "", cardStyle);
        if (selectedWorldRegion < 0)
        {
            GUI.Label(new Rect(100, 838, 925, 50), "지역을 선택하세요", headingStyle);
            GUI.Label(new Rect(100, 894, 925, 70), "지역을 선택하지 않은 상태에서는 오른쪽 길드원을 눌러 정보를 볼 수 있습니다.", labelStyle);
            return;
        }
        GUI.Label(new Rect(100, 836, 925, 43), WorldRegionNames[selectedWorldRegion], headingStyle);
        GUI.Label(new Rect(100, 882, 925, 90), request == null ?
            WorldRegionFeatures[selectedWorldRegion] + "\n등장 마물: " + WorldRegionMonsters[selectedWorldRegion] +
            " · 진행 중인 의뢰가 없습니다." :
            request.title + " · 권장 Lv." + request.recommendedLevel + " · " + request.waves + "전투\n" +
            request.description + "  보상 " + request.reward + " G / 준비금 " + request.expense + " G", labelStyle);
    }

    private void DrawWorldMemberList(BattleRequest request)
    {
        int count = GuildMemberCount();
        GUI.Label(new Rect(1150, 236, 680, 40), request == null ? "길드원을 누르면 상세 정보가 열립니다." :
            "전열 순서대로 " + request.party.Count + "/4명" + (request.prepared ? " · 저장됨" : " · 저장 필요"), labelStyle);
        worldMemberScroll = GUI.BeginScrollView(new Rect(1148, 282, 694, 400), worldMemberScroll,
            new Rect(0, 0, 668, Mathf.Max(400, count * 92)), false, true);
        int row = 0;
        for (int i = 0; i < guests.Count; i++)
        {
            Guest member = guests[i];
            if (!member.guildMember) continue;
            float y = row++ * 92;
            bool chosen = request != null && request.party.Contains(i);
            GUI.Box(new Rect(0, y, 660, 82), "", chosen ? selectedTabStyle : cardStyle);
            DrawGuestPortrait(new Rect(7, y + 5, 68, 72), i);
            GUI.Label(new Rect(84, y + 5, 286, 36), member.name + " · Lv." + member.level, headingStyle);
            GUI.Label(new Rect(84, y + 42, 286, 34), member.baseClass + (chosen ? " · " + (request.party.IndexOf(i) + 1) + "번" : ""), labelStyle);
            int guestIndex = i;
            GUI.enabled = request == null || chosen || request.party.Count < 4;
            if (GUI.Button(new Rect(380, y + 12, 164, 56), request == null ? "정보 보기" : chosen ? "편성 해제" : "편성", buttonStyle))
            {
                selectedWorldMember = guestIndex;
                if (request != null)
                {
                    if (chosen) request.party.Remove(guestIndex);
                    else request.party.Add(guestIndex);
                    request.prepared = false;
                    worldMapNotice = "편성 " + request.party.Count + "/4명 · 순서는 선택한 순서대로 정해집니다.";
                }
            }
            GUI.enabled = true;
            if (request != null && GUI.Button(new Rect(552, y + 12, 96, 56), "정보", tabStyle))
                selectedWorldMember = guestIndex;
        }
        GUI.EndScrollView();
        if (count == 0)
            GUI.Label(new Rect(1160, 335, 650, 120), "길드원이 없습니다. 밤에 용사의 호감도를 올려 길드에 영입하세요.", labelStyle);
    }

    private void DrawWorldMemberDetails()
    {
        GUI.Box(new Rect(1148, 699, 694, 200), "", cardStyle);
        if (selectedWorldMember < 0 || selectedWorldMember >= guests.Count || !guests[selectedWorldMember].guildMember)
        {
            GUI.Label(new Rect(1170, 739, 640, 100), "길드원을 선택하면 능력치와 장비가 여기에 표시됩니다.", labelStyle);
            return;
        }
        Guest member = guests[selectedWorldMember];
        GUI.Label(new Rect(1170, 708, 640, 40), member.name + " · " + member.baseClass + " · Lv." + member.level, headingStyle);
        GUI.Label(new Rect(1170, 752, 640, 36), HeroStatsLabel(member), labelStyle);
        GUI.Label(new Rect(1170, 789, 640, 36), "호감도 " + AffinityStage(member) + "/5 · 경험치 " + member.trainingExperience + "/" + HeroExperienceRequired(member.level), labelStyle);
        GUI.Label(new Rect(1170, 828, 640, 62), "무기: " + (member.equippedWeapon == null ? "없음" : member.equippedWeapon.name) +
            " · 방어구: " + (member.equippedArmor == null ? "없음" : member.equippedArmor.name), labelStyle);
    }
}
