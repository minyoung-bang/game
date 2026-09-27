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
- **길드원** — inspect recruited heroes, including their level, affinity, gold, and completed-request experience. A visiting hero joins at affinity 3 if lodging capacity is available; otherwise recruitment waits until capacity is upgraded.
- **길드정보** — inspect the vault, day, guild level progress, member count, completed sales, and successful simple requests. Successful requests grant guild experience; level-based content unlocks are planned but not yet implemented.
- Click the bartender/owner behind the bar during evening to open a guild workbench with Food Crafting, Weapon Crafting, Armor Crafting, and Battle Preparation categories. Each crafting category shows only its three relevant materials across the top and a scrollable three-column product grid. There are nine products in low, medium, and high price tiers for each category. Choose a product to inspect each ingredient's required and owned counts, then craft it into evening shop stock. Crafting is only available from this night guildmaster window.

Click the selected button again, the × button, or press Escape to close a menu. The previous **자리 다시 배치** button was removed; hero seating is randomized when evening begins.

## 낮 준비와 저녁 영업

- 첫 실행은 DAY 01 저녁입니다. 오른쪽 아래 **하루 종료**를 누르면 다음 날 낮으로 이동합니다.
- **재료 수집**: 곡물, 산딸기, 고기, 목재, 철광석, 가죽을 수집합니다. 행동 횟수 제한은 없고, 재료별 하루 공급량(현재 임시 설정 4~7개)만 제한됩니다. 한 번에 최대 2개를 얻습니다.
- **음식·장비 제작**: 재료를 소비해 음식/무기/방어구 9종을 제작합니다. 제작된 상품은 저녁 판매 재고에 추가됩니다. 제작 비용과 공급량은 초기 밸런스 값입니다.
- **마을 의뢰 게시판**: 간단 의뢰와 전투 의뢰 탭이 있습니다. 각 간단 의뢰에는 1~3일 중 무작위 기한이 표시됩니다. 수락한 의뢰는 당일 ‘수락 완료’로 표시되고 다음 날부터 게시판에서 숨겨집니다. 기한이 남은 동안은 밤의 용사 요청 목록에 표시되며, 성공할 때까지 실패 후 다른 용사에게 다시 맡길 수 있습니다. 기한이 지나면 만료되어 게시판과 요청 목록에서 사라집니다. 초기 의뢰는 미수락 상태입니다.
- **간단 의뢰**: 길드 소속과 무관하게 용사에게 요청할 수 있고 자동 처리됩니다. 성공 시 의뢰가 완료되고 보상과 경험치가 지급됩니다. 실패 시 호감도가 감소하며 남은 기한 안에 다시 요청할 수 있습니다.
- **전투 의뢰 준비**: 한 번에 전투 의뢰 하나만 수락할 수 있습니다. 수락한 의뢰는 길드원 편성이 끝날 때까지 게시판에 진행 중으로 표시되며, 다른 전투 의뢰 수락은 잠깁니다. 전투 의뢰는 용사의 의뢰 요청 목록에 나오지 않습니다. 밤에 바 안쪽 길드장 위에 마우스를 올리면 떠오름/확대 효과와 안내가 표시됩니다. 클릭하면 수락한 전투 의뢰와 길드원 편성 화면이 열립니다. 길드원만 최대 4명까지 선택할 수 있으며, 다시 클릭하면 선택이 해제됩니다. 1~4명을 선택한 후 ‘준비 완료’로 의뢰별 편성을 저장합니다. 창을 닫거나 날짜가 바뀌어도 실행 중에는 편성이 유지됩니다. 실제 전투 출전·완료 처리는 아직 구현하지 않았습니다.
- **저녁 영업 시작**: 당일 저녁으로 전환하고 용사 좌석을 다시 배치합니다. 판매 시 상품 재고가 1개 줄어들며 품절된 상품은 판매할 수 없습니다. 처음에는 각 상품 2개가 있습니다.
- 재료, 상품 재고, 골드와 용사 상태는 날짜가 바뀌어도 유지됩니다. 현재는 실행 중 메모리에만 유지되므로 Play를 종료하면 초기화됩니다. 저장/불러오기는 아직 구현하지 않았습니다.
- **훈련장**: 낮 지도에서 길드 뒤편 훈련장을 선택합니다. 길드 소속 용사만 훈련하거나 스킬을 관리할 수 있습니다. 용사별로 하루 한 번 훈련해 경험치를 얻고 레벨을 올릴 수 있으며, 훈련장 확장으로 1회 훈련 경험치가 증가합니다. 스킬 선택은 용사별로 저장되지만 실제 전투 효과는 전투 시스템 개발 때 연결합니다.
- 낮 화면은 영지 지도와 지역별 활동 화면으로 구현되어 있습니다. 직접 조작하는 전투와 길드 레벨별 콘텐츠 해금은 아직 구현하지 않았습니다.

### 확인 순서

하루 종료 → DAY 02 낮 표시 → 각 재료 공급 소진 및 수집 버튼 비활성화 확인 → 재료 부족/충분 상태에서 제작 확인 → 저녁 영업 시작 → 제작한 상품 판매 및 재고 감소 확인 → 간단 의뢰 요청 후 목록에서 사라지는지 확인 → 하루 종료 후 공급 보충과 재고 유지 확인.

의뢰 추가 확인: 마을에서 간단/전투 의뢰 각각 수락 → 수락 완료 표시 → 길드로 복귀 → 방문 용사에게 간단 의뢰만 표시되는지 확인 → 바 안쪽 주인공 호버/클릭 → 소속 용사 선택·해제·준비 완료 → 닫았다 다시 열어 편성 확인 → 다음 날 게시판에서 수락한 두 의뢰가 숨겨지고 미수락 의뢰는 남아 있는지 확인. 길드원이 5명 이상인 상태에서 4명 선택 시 다섯 번째 선택이 제한되는지 확인.
