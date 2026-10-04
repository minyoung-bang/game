# Guild Hall Unity Prototype

Unity 6 prototype for the guild interior.

## Open and play

1. Open this folder from Unity Hub using Unity `6000.3.2f1` or a compatible Unity 6 release.
2. On first editor load, the project creates `Assets/Scenes/GuildHall.unity` automatically. If needed, use **Guild Hall → Create or Reset Prototype Scene**.
3. Open `Assets/Scenes/GuildHall.unity` and press Play.
4. Hover over a guest to make their sprite gently rise and scale up, then click to open the guest interaction window.
5. Use the left portrait/dialogue area and the right-side Food, Weapons, Armor, or Requests categories.
6. Select an item and confirm the sale, or open a request for details and assign it.

The top-right buttons are available on both the guild hall and daytime map (close a guest interaction first):

- **설정** — toggle fullscreen, adjust text size, and set game volume. Interface settings persist between runs. The prototype does not yet include music or sound effects.
- **길드 업그레이드** — spend guild gold on four facilities, each from level 0 to 5: guild expansion raises nightly visitor capacity, lodging raises the recruitment cap, publicity raises the chance of level 3+ visitors, and training-hall expansion grants more training experience. Costs increase with each level. These prototype values are held in memory for the current play session; save/load is not implemented. The current guest artwork/seat set limits visible guests to six.
- **인벤토리** — inspect six gathered materials and the sale stock and prices of food, weapons, and armor.
- **길드원** — inspect recruited heroes, including their level, affinity, gold, experience, and calculated job stats. A visiting hero joins at affinity stage 2 (50 points) if lodging capacity is available; otherwise recruitment waits until capacity is upgraded.
- **길드정보** — inspect the vault, day, guild level progress, member count, completed sales, and successful simple requests. Successful requests grant guild experience and guild levels unlock products, materials, quest posts, and battle requests according to the balance draft.
- Click the bartender/owner behind the bar during evening to open a guild workbench with Food Crafting, Weapon Crafting, Armor Crafting, and Battle Preparation categories. Product lists use a scrollable four-column grid. Weapons and armor are grouped by price tier, with Fighter, Thief, Archer, and Mage equipment in that order. Each equipment category has three products per class (12 total); food has nine. Choose a product to inspect each ingredient's required and owned counts, then craft it into evening shop stock. Crafting is only available from this night guildmaster window.
- Planned advancement paths: Fighter (Knight, Berserker, Magic Knight), Thief (Thief, Assassin, Poisoner), Archer (Divine Archer, Bowmaster, Magic Archer), and Mage (Elementalist, Priest, Warlock). Player-selected advancement and class-specific equipment restrictions are not implemented yet; Thief weapons and armor are present in crafting, shop stock, and inventory.

Click the selected button again, the × button, or press Escape to close a menu. The previous **자리 다시 배치** button was removed; hero seating is randomized when evening begins.

## 낮 준비와 저녁 영업

- 첫 실행은 DAY 01 저녁입니다. 오른쪽 아래 **하루 종료**를 누르면 다음 날 낮으로 이동합니다.
- **재료 수집**: 숲·농장·광산에서 각각 6종, 총 18종을 수집합니다. 길드 레벨에 따라 채집처가 해금되고 수집 한 번에 행동 1회를 사용합니다. 하루 행동 횟수는 길드 레벨×2이며, 수확량은 재료별 밸런스 초안 범위에서 무작위입니다. 전투 희귀 재료 6종은 전투 보상용으로 등록돼 있지만 전투 실행/드롭은 아직 연결하지 않았습니다.
- **음식·장비 제작**: 재료와 제작 수수료를 사용해 음식 9종, 무기 12종, 방어구 12종을 제작합니다. 장비는 전사·도적·궁수·마법사 순으로 배치되어 있고 길드 레벨 해금 및 직업 제한을 반영합니다. 도적 장비의 가격·레벨·제작법은 밸런스 표에 없는 프로토타입 확장값입니다. 제작된 상품은 저녁 판매 재고에 추가됩니다.
- **마을 의뢰 게시판**: 길드 레벨에 따라 해금되는 일반 의뢰 중 하루 게시 수만큼 무작위로 표시됩니다. 각 의뢰 기한은 1/2/3일로 30/40/30% 확률로 정합니다. 수락한 의뢰는 당일 ‘수락 완료’로 표시되고 다음 날부터 게시판에서 숨겨집니다. 기한 내 실패하면 다른 용사에게 다시 맡길 수 있고, 만료되면 게시판과 용사 요청 목록에서 사라집니다.
- **간단 의뢰**: 길드 소속과 무관하게 용사에게 요청할 수 있고 자동 처리됩니다. 성공률은 기본 75%, 권장 레벨과 용사 레벨 차이당 6%p, 장비 능력치 합에 따른 최대 +10%p를 반영하고 20~95%로 제한합니다. 성공 시 용사 골드/경험치와 길드 경험치/15% 수수료, 호감도 +8을 지급하고 완료 처리합니다. 실패 시 호감도 −5이며 남은 기한 안에 다른 용사에게 재요청할 수 있습니다. 용사별 하루 한 건 제한입니다.
- **전투 의뢰 준비**: 한 번에 전투 의뢰 하나만 수락할 수 있습니다. 수락한 의뢰는 길드원 편성이 끝날 때까지 게시판에 진행 중으로 표시되며, 다른 전투 의뢰 수락은 잠깁니다. 전투 의뢰는 용사의 의뢰 요청 목록에 나오지 않습니다. 밤에 바 안쪽 길드장 위에 마우스를 올리면 떠오름/확대 효과와 안내가 표시됩니다. 클릭하면 수락한 전투 의뢰와 길드원 편성 화면이 열립니다. 길드원만 최대 4명까지 선택할 수 있으며, 다시 클릭하면 선택이 해제됩니다. 1~4명을 선택한 후 ‘준비 완료’로 의뢰별 편성을 저장합니다. 창을 닫거나 날짜가 바뀌어도 실행 중에는 편성이 유지됩니다. 실제 전투 출전·완료 처리는 아직 구현하지 않았습니다.
- **저녁 영업 시작**: 당일 저녁으로 전환하고 용사 좌석을 다시 배치합니다. 판매 시 상품 재고가 1개 줄어들며 품절된 상품은 판매할 수 없습니다. 처음에는 각 상품 2개가 있습니다.
- 재료, 상품 재고, 골드와 용사 상태는 날짜가 바뀌어도 유지됩니다. 현재는 실행 중 메모리에만 유지되므로 Play를 종료하면 초기화됩니다. 저장/불러오기는 아직 구현하지 않았습니다.
- **훈련장**: 낮 지도에서 길드 뒤편 훈련장을 선택합니다. 길드 소속 용사만 훈련하거나 스킬을 관리할 수 있습니다. 용사별로 하루 한 번 훈련해 경험치를 얻고 레벨을 올릴 수 있으며, 훈련장 확장으로 1회 훈련 경험치가 증가합니다. 기본 직업 스킬 2개는 처음부터 습득·장착되어 있습니다. 레벨 3 이상이면 플레이어가 전직 계열을 선택하고, 해당 계열의 스킬은 레벨 3·6·9·12·15·18·20에 순서대로 해금됩니다. 고레벨로 영입된 용사도 지난 해금 단계 스킬을 모두 선택할 수 있습니다. 습득 스킬은 최대 4개 장착할 수 있으며, 색상은 습득 상태, 금색 테두리는 장착 상태를 뜻합니다. 스킬 트리의 가로 스크롤과 마우스 오버 설명을 지원합니다. 스킬 효과와 계열 변경은 실제 전투 개발 단계에서 연결할 예정입니다.
- 길드 레벨 표의 레벨별 필요 경험치, 일일 게시 의뢰 수, 재료·상품 해금, 행동 횟수 및 전투 의뢰 정보가 적용되어 있습니다. 영웅 경험치 필요량 공식과 호감도 점수/단계 기준도 반영했습니다. 밸런스 표에 없는 도적 전용 스탯 공식, 선호 음식 캐릭터 배정, 초기 방문자 골드/레벨의 절차 생성은 아직 적용하지 않았습니다.
- 전투 의뢰 8종의 권장 레벨, 편성 인원, 전투 횟수, 보상, 경험치, 준비금 및 길드 해금 레벨을 준비/게시판 데이터에 반영했습니다. 실제 턴제 전투, 승패 정산, 전투 보상 재료 드롭은 아직 구현하지 않았습니다.

### 용사 성장·행동력 추가 반영

- 훈련은 길드원별 하루 1회이며 행동 포인트 1회를 소비합니다. 채집과 같은 일일 행동력 풀을 씁니다. 기본 행동 포인트는 길드 레벨×2이고, 새 **행동력 확장** 시설은 레벨당 +2회를 제공합니다. 확장 비용은 Lv.1~5 순서로 25/50/90/150/240 G이며, 이 비용/증가량은 밸런스 초안에 없는 임시 제안값입니다.
- 영입 기준은 호감도 3단계가 아니라 **2단계, 50점**입니다. 기존 판매 +3점, 간단 의뢰 성공 +8점, 실패 −5점 규칙은 유지합니다.
- 전사·궁수·마법사의 능력치는 밸런스 표의 레벨 성장식으로 계산합니다. 도적은 별도 공식이 없어 임시로 HP `34+6×(레벨−1)`, 공격 `9+2×(레벨−1)`, 방어 `2+floor(0.6×(레벨−1))`, 속도 `7+floor((레벨−1)/3)`을 사용합니다.
- 능력치는 용사의 현재 직업과 레벨로 재계산되어 인물 창과 길드원 목록에 표시됩니다. 향후 직업 변경은 `ChangeGuestBaseClass` 경로를 사용하도록 설계했으며, 직업을 바꾸면 능력치와 기본 스킬이 해당 직업 기준으로 갱신됩니다. 직업 변경 UI는 아직 없습니다. 능력치는 현재 표시/데이터 단계이며 전투 계산에는 아직 연결하지 않았습니다.
- 초안의 길드 레벨별 방문 인원·용사 레벨 생성, 신규 용사 골드 범위, 호감도별 재방문 확률은 여전히 미구현입니다. 현재 방문자는 고정된 6명의 캐릭터 데이터에서 고르며, 이는 절차적 용사 생성과 구분됩니다.

### 확인 순서

하루 종료 → DAY 02 낮 표시 → 각 재료 공급 소진 및 수집 버튼 비활성화 확인 → 재료 부족/충분 상태에서 제작 확인 → 저녁 영업 시작 → 제작한 상품 판매 및 재고 감소 확인 → 간단 의뢰 요청 후 목록에서 사라지는지 확인 → 하루 종료 후 공급 보충과 재고 유지 확인.

의뢰 추가 확인: 마을에서 간단/전투 의뢰 각각 수락 → 수락 완료 표시 → 길드로 복귀 → 방문 용사에게 간단 의뢰만 표시되는지 확인 → 바 안쪽 주인공 호버/클릭 → 소속 용사 선택·해제·준비 완료 → 닫았다 다시 열어 편성 확인 → 다음 날 게시판에서 수락한 두 의뢰가 숨겨지고 미수락 의뢰는 남아 있는지 확인. 길드원이 5명 이상인 상태에서 4명 선택 시 다섯 번째 선택이 제한되는지 확인.
