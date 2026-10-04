// Generated from 용사길드_밸런스초안_v0.5.xlsx. Combat resolution remains deferred.
using System.Collections.Generic;
using UnityEngine;
public sealed partial class GuildHallRuntime
{
 private static readonly Dictionary<string, HeroStats[]> BalanceStats = new Dictionary<string, HeroStats[]>
 {
 { "전사", new HeroStats[] { new HeroStats(42,8,4,3), new HeroStats(50,10,4,3), new HeroStats(58,12,5,3), new HeroStats(66,14,6,3), new HeroStats(74,16,7,3), new HeroStats(82,18,8,4), new HeroStats(90,20,9,4), new HeroStats(98,22,10,4), new HeroStats(106,24,11,4), new HeroStats(114,26,12,4), new HeroStats(122,28,13,5), new HeroStats(130,30,13,5), new HeroStats(138,32,14,5), new HeroStats(146,34,15,5), new HeroStats(154,36,16,5), new HeroStats(162,38,17,6), new HeroStats(170,40,18,6), new HeroStats(178,42,19,6), new HeroStats(186,44,20,6), new HeroStats(194,46,21,6) } },
 { "도적", new HeroStats[] { new HeroStats(34,9,2,7), new HeroStats(40,11,2,7), new HeroStats(46,13,3,7), new HeroStats(52,15,3,8), new HeroStats(58,17,4,8), new HeroStats(64,19,5,8), new HeroStats(70,21,5,9), new HeroStats(76,23,6,9), new HeroStats(82,25,6,9), new HeroStats(88,27,7,10), new HeroStats(94,29,8,10), new HeroStats(100,31,8,10), new HeroStats(106,33,9,11), new HeroStats(112,35,9,11), new HeroStats(118,37,10,11), new HeroStats(124,39,11,12), new HeroStats(130,41,11,12), new HeroStats(136,43,12,12), new HeroStats(142,45,12,13), new HeroStats(148,47,13,13) } },
 { "궁수", new HeroStats[] { new HeroStats(32,9,2,7), new HeroStats(38,11,2,7), new HeroStats(44,13,2,7), new HeroStats(50,15,2,8), new HeroStats(56,17,2,8), new HeroStats(62,19,3,8), new HeroStats(68,21,3,9), new HeroStats(74,23,3,9), new HeroStats(80,25,3,9), new HeroStats(86,27,3,10), new HeroStats(92,29,4,10), new HeroStats(98,31,4,10), new HeroStats(104,33,4,11), new HeroStats(110,35,4,11), new HeroStats(116,37,4,11), new HeroStats(122,39,5,12), new HeroStats(128,41,5,12), new HeroStats(134,43,5,12), new HeroStats(140,45,5,13), new HeroStats(146,47,5,13) } },
 { "마법사", new HeroStats[] { new HeroStats(28,10,1,5), new HeroStats(34,12,1,5), new HeroStats(39,14,1,5), new HeroStats(45,16,1,5), new HeroStats(50,18,2,6), new HeroStats(56,20,2,6), new HeroStats(61,22,2,6), new HeroStats(67,24,2,6), new HeroStats(72,26,3,7), new HeroStats(78,28,3,7), new HeroStats(83,30,3,7), new HeroStats(89,32,3,7), new HeroStats(94,34,4,8), new HeroStats(100,36,4,8), new HeroStats(105,38,4,8), new HeroStats(111,40,4,8), new HeroStats(116,42,5,9), new HeroStats(122,44,5,9), new HeroStats(127,46,5,9), new HeroStats(133,48,5,9) } },
 };
 private void ApplyBalanceV05()
 {
  var recipeByName = new Dictionary<string, int[]>();
  int oldIndex = 0;
  foreach (var category in inventory) foreach (var item in category) recipeByName[item.name] = recipes[oldIndex++];
  ApplyItemRow(0, "꿀 바른 빵", 8, 1, 0, "전 직업", 8, 0, 0);
  recipeByName["꿀 바른 빵"] = Recipe(0,2);
  ApplyItemRow(0, "산딸기 수프", 10, 2, 0, "전 직업", 12, 0, 0);
  recipeByName["산딸기 수프"] = Recipe(1,2,10,1);
  ApplyItemRow(0, "구운 고기", 14, 1, 0, "전 직업", 16, 0, 0);
  recipeByName["구운 고기"] = Recipe(2,2);
  ApplyItemRow(0, "허브 치킨", 24, 3, 1, "전 직업", 24, 0, 0);
  recipeByName["허브 치킨"] = Recipe(2,2,6,1,10,1);
  ApplyItemRow(0, "버섯 파이", 28, 4, 1, "전 직업", 28, 0, 0);
  recipeByName["버섯 파이"] = Recipe(0,2,7,2,12,1);
  ApplyItemRow(0, "훈제 연어", 32, 4, 1, "전 직업", 32, 0, 0);
  recipeByName["훈제 연어"] = Recipe(2,2,11,2,14,1);
  ApplyItemRow(0, "왕실 만찬", 65, 8, 2, "전 직업", 40, 0, 0);
  recipeByName["왕실 만찬"] = Recipe(2,3,12,2,7,2,21,1);
  ApplyItemRow(0, "마력열매 타르트", 62, 9, 2, "전 직업", 48, 0, 0);
  recipeByName["마력열매 타르트"] = Recipe(0,2,1,3,8,1,23,1);
  ApplyItemRow(0, "용고기 스테이크", 80, 9, 2, "전 직업", 60, 0, 0);
  recipeByName["용고기 스테이크"] = Recipe(2,4,12,2,6,2,18,1);
  ApplyItemRow(1, "견습자의 검", 20, 1, 0, "전사", 0, 3, 0);
  recipeByName["견습자의 검"] = Recipe(3,1,4,2);
  ApplyItemRow(1, "사냥꾼의 활", 28, 1, 0, "궁수", 0, 3, 0);
  recipeByName["사냥꾼의 활"] = Recipe(3,2,5,1);
  ApplyItemRow(1, "참나무 지팡이", 24, 1, 0, "마법사", 0, 3, 0);
  recipeByName["참나무 지팡이"] = Recipe(3,2,1,1);
  ApplyItemRow(1, "강철 장검", 55, 3, 1, "전사", 0, 7, 0);
  recipeByName["강철 장검"] = Recipe(4,4,14,2,3,1);
  ApplyItemRow(1, "장인의 장궁", 68, 4, 1, "궁수", 0, 7, 0);
  recipeByName["장인의 장궁"] = Recipe(3,4,5,2,9,1);
  ApplyItemRow(1, "마도사의 지팡이", 62, 7, 1, "마법사", 0, 8, 0);
  recipeByName["마도사의 지팡이"] = Recipe(3,2,16,1,8,1);
  ApplyItemRow(1, "왕가의 검", 120, 8, 2, "전사", 0, 13, 0);
  recipeByName["왕가의 검"] = Recipe(4,4,15,2,14,2,18,1);
  ApplyItemRow(1, "용사냥 장궁", 145, 8, 2, "궁수", 0, 13, 0);
  recipeByName["용사냥 장궁"] = Recipe(3,4,15,2,5,3,9,2,20,1);
  ApplyItemRow(1, "대마법사의 지팡이", 135, 9, 2, "마법사", 0, 15, 0);
  recipeByName["대마법사의 지팡이"] = Recipe(3,3,16,2,8,2,15,1,19,1);
  ApplyItemRow(2, "가죽 조끼", 18, 1, 0, "전사·궁수", 0, 0, 2);
  recipeByName["가죽 조끼"] = Recipe(5,3);
  ApplyItemRow(2, "작은 방패", 22, 1, 0, "전사", 0, 0, 3);
  recipeByName["작은 방패"] = Recipe(3,2,17,2,4,1);
  ApplyItemRow(2, "여행자 망토", 16, 1, 0, "전 직업", 0, 0, 1);
  recipeByName["여행자 망토"] = Recipe(5,2,1,1);
  ApplyItemRow(2, "강화 가죽갑옷", 45, 3, 1, "전사·궁수", 0, 0, 4);
  recipeByName["강화 가죽갑옷"] = Recipe(5,4,4,2,13,1);
  ApplyItemRow(2, "수호자의 방패", 52, 4, 1, "전사", 0, 0, 5);
  recipeByName["수호자의 방패"] = Recipe(3,3,4,3,17,3,9,1);
  ApplyItemRow(2, "마법 망토", 48, 7, 1, "마법사", 0, 0, 3);
  recipeByName["마법 망토"] = Recipe(5,3,16,1,8,1);
  ApplyItemRow(2, "용비늘 갑옷", 110, 8, 2, "전사", 0, 0, 7);
  recipeByName["용비늘 갑옷"] = Recipe(4,4,15,3,5,3,14,2,22,1);
  ApplyItemRow(2, "왕실 수호 방패", 125, 8, 2, "전사", 0, 0, 8);
  recipeByName["왕실 수호 방패"] = Recipe(4,4,15,3,17,4,9,2,20,1);
  ApplyItemRow(2, "별빛 로브", 118, 9, 2, "마법사", 0, 0, 6);
  recipeByName["별빛 로브"] = Recipe(5,4,16,2,8,2,23,1);
  var allRecipes = new List<int[]>();
  for (int c=0;c<inventory.Length;c++) for(int i=0;i<inventory[c].Length;i++)
  { var item=inventory[c][i]; item.stock = c==0 && i<2 ? 2 : 0; allRecipes.Add(recipeByName[item.name]); }
  recipes = allRecipes.ToArray();
  // Apply workbook display-name proposals after recipes are keyed by the existing item names.
  RenameItem(0, "꿀 바른 빵", "곡물 빵");
  RenameItem(0, "허브 치킨", "허브 고기구이");
  RenameItem(0, "훈제 연어", "사과 훈제구이");
  RenameItem(0, "마력열매 타르트", "달빛 산딸기 타르트");
  RenameItem(0, "용고기 스테이크", "영웅 스테이크");
  RenameItem(2, "용비늘 갑옷", "은강철 갑옷");
  for(int i=0;i<guests.Count;i++)
  {
   Guest g=guests[i]; g.portraitIndex=i; g.hasVisited=false; g.foodPreference=Random.Range(0,3);
   g.guildMember=false;
   g.affinity=(g.name=="브란" || g.name=="노아") ? 47 : AffinityThresholdForStage(0);
   RefreshHeroStats(g);
  }
  // Guided opening: these two goods must be crafted before the visitors can buy them.
  inventory[0][0].stock=0;
  inventory[0][2].stock=0;
  inventory[1][0].stock=0;
  if (guests.Count > 5) guests[5].foodPreference=1; // Bread purchase grants the normal +3 only.
  materials[0]=2; // Grain for one loaf.
  materials[3]=1; // Wood for the starter sword.
  materials[4]=2; // Iron ore for the starter sword.
 }
 private void ApplyItemRow(int category,string name,int price,int unlock,int cost,string jobs,int hp,int attack,int defense)
 {
  ShopItem item=System.Array.Find(inventory[category], x=>x.name==name);
  if(item==null) { item=new ShopItem(name,price,7); var list=new List<ShopItem>(inventory[category]); list.Add(item); inventory[category]=list.ToArray(); }
  item.price=price; item.unlockGuildLevel=unlock; item.productionCost=cost; item.allowedJob=jobs; item.hpRecovery=hp; item.attackBonus=attack; item.defenseBonus=defense;
 }
 private void RenameItem(int category,string currentName,string proposedName)
 {
  ShopItem item=System.Array.Find(inventory[category],x=>x.name==currentName);
  if(item!=null) item.name=proposedName;
 }
 private static bool CanEquip(Guest guest,ShopItem item) => item==null || item.allowedJob=="전 직업" || System.Array.IndexOf(item.allowedJob.Split('·'),guest.baseClass)>=0;
}
