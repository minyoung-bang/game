using System.Collections.Generic;
using UnityEngine;

public sealed partial class GuildHallRuntime
{
    private int visitorNights, initialNewcomers, nightsWithoutNewcomer, returnPrioritySequence;
    private static readonly float[] ReturnWeights = { .25f, .40f, .55f, .75f, .85f, .95f };

    // Stochastic rounding preserves mean demand independently of chair capacity.
    private int DrawVisitorCount()
    {
        float demand = (5 + Mathf.Clamp(guildLevel, 1, 10)) * VisitorChance();
        int count = Mathf.FloorToInt(demand);
        if (Random.value < demand - count) count++;
        return Mathf.Min(count, NightVisitorCapacity());
    }

    private int[] SelectEveningRoster()
    {
        int capacity = NightVisitorCapacity();
        int count = DrawVisitorCount();
        var roster = new List<int>();
        bool newcomerScheduled = initialNewcomers < 3 || nightsWithoutNewcomer >= 2;
        bool newcomerDue = newcomerScheduled && guests.Exists(g => !g.hasVisited);

        // Preferred-food recipients have a guaranteed return place. Oldest reservations are seated first.
        var priorityGuests = new List<int>();
        for (int i=0;i<guests.Count;i++)
            if (guests[i].hasVisited && guests[i].preferredReturnOrder >= 0) priorityGuests.Add(i);
        priorityGuests.Sort((a,b) => guests[a].preferredReturnOrder.CompareTo(guests[b].preferredReturnOrder));

        int requestedCount = Mathf.Max(count, priorityGuests.Count + (newcomerDue ? 1 : 0));
        count = Mathf.Min(requestedCount, capacity);
        for (int i=0;i<priorityGuests.Count && roster.Count<count;i++)
        {
            int guestIndex = priorityGuests[i];
            roster.Add(guestIndex);
            guests[guestIndex].preferredReturnOrder = -1;
        }

        bool newcomerArrived = false;
        if (newcomerDue && roster.Count<count)
        {
            int newcomer = CreateNewVisitor();
            if (newcomer >= 0)
            {
                roster.Add(newcomer);
                newcomerArrived = true;
            }
        }
        var candidates = new List<int>();
        for (int i=0;i<guests.Count;i++)
            if (guests[i].hasVisited && guests[i].preferredReturnOrder < 0 && !roster.Contains(i)) candidates.Add(i);
        while (roster.Count < count && candidates.Count > 0)
        {
            float total = 0;
            foreach (int i in candidates) total += VisitWeight(guests[i]);
            float roll = Random.value * total;
            int pick = candidates[candidates.Count - 1];
            foreach (int i in candidates) { roll -= VisitWeight(guests[i]); if (roll <= 0) { pick=i; break; } }
            roster.Add(pick);
            candidates.Remove(pick);
        }

        if (roster.Count == 0) return roster.ToArray();

        // Count completed visit nights, including nights containing only ordinary returnees.
        // Before the weighted fill above, an empty roster does not mean an empty evening.
        if (newcomerArrived)
        {
            if (initialNewcomers < 3) initialNewcomers++;
            nightsWithoutNewcomer = 0;
        }
        else if (!newcomerDue) nightsWithoutNewcomer = Mathf.Min(2, nightsWithoutNewcomer + 1);
        // When guaranteed returns fill all seats, keep the newcomer due until a seat is free.

        visitorNights++;
        foreach(int i in roster)
        {
            guests[i].hasVisited = true;
            guests[i].lastVisitNight = visitorNights;
            if (!guests[i].guildMember && AffinityStage(guests[i]) >= 2) TryRecruitGuest(guests[i]);
        }
        return roster.ToArray();
    }

    private void SeatOpeningRoster()
    {
        // The opening night is reserved for the two tutorial recruits.
        int[] roster = { 1, 5 };
        visitorNights = 1;
        foreach (int index in roster)
        {
            guests[index].hasVisited = true;
            guests[index].lastVisitNight = visitorNights;
        }
        var artwork = new int[guests.Count];
        for (int i = 0; i < guests.Count; i++) artwork[i] = guests[i].portraitIndex;
        presentation.ConfigureGuests(artwork);
        presentation.Reseat(Random.Range(1, int.MaxValue), roster, NightVisitorCapacity());
    }

    private float VisitWeight(Guest guest)
        => ReturnWeights[AffinityStage(guest)] * (guest.lastVisitNight == visitorNights ? .5f : 1f);

    private int CreateNewVisitor()
    {
        var unvisited = new List<int>();
        for (int i=0;i<guests.Count;i++) if (!guests[i].hasVisited) unvisited.Add(i);
        if (unvisited.Count == 0) return -1;
        int index = unvisited[Random.Range(0, unvisited.Count)];
        Guest guest = guests[index];
        // Identity, class, and starting level belong to this authored character and never reroll.
        guest.gold = Random.Range(20+8*(guest.level-1),36+12*(guest.level-1)+1);
        guest.affinity = AffinityThresholdForStage(0);
        guest.foodPreference = Random.Range(0,3);
        guest.equippedSkills.Clear();
        foreach(string skill in BasicSkillsFor(guest.baseClass)) guest.equippedSkills.Add(BasicSkillId(guest,skill));
        RefreshHeroStats(guest);
        return index;
    }

    private void SeatEveningRoster()
    {
        int[] roster = SelectEveningRoster();
        var artwork = new int[guests.Count];
        for(int i=0;i<guests.Count;i++) artwork[i]=guests[i].portraitIndex;
        presentation.ConfigureGuests(artwork);
        presentation.Reseat(Random.Range(1,int.MaxValue),roster,NightVisitorCapacity());
    }

    private int FoodPreferenceGroup(ShopItem item)
    {
        int index = System.Array.IndexOf(inventory[0], item);
        return index == 0 || index == 1 || index == 7 ? 0 : index == 4 ? 2 : 1;
    }
}
