# 낮 영지 지도

내장 imagegen 도구로 생성하고 Assets/Resources/GuildDayMap.png에 저장했습니다. 기존 GuildHallBackground-EmptyGuests.png를 그래픽 참고로 사용했습니다.

## 생성 프롬프트

Create a production game background, 16:9 landscape 1536x864 or larger. Use attached tavern image ONLY as pixel-art STYLE reference: same crisp dark outlines, rich small pixel clusters, warm wood, navy shadows, charming detailed RPG game art, no smooth AI painting. New subject is DAYTIME fantasy overworld region selection map, high angle isometric natural green valley, full bleed. EXACT layout: dense enchanted FOREST landmark centered at normalized (0.19,0.29); FARM with wheat plots barn windmill centered (0.19,0.74); large inviting timber GUILD HALL blue roofs golden stag banners centered (0.50,0.49); rocky MINE entrance with cart crystals centered (0.83,0.29); cozy VILLAGE cluster of red-roof cottages market centered (0.80,0.74). Each landmark occupies about 20% of image width and 25% height, entirely visible and distinctly separated by grassy meadows. Winding footpaths connect landmarks across rolling grass, wildflowers, small stream and bridges. Distant fantasy hills across top edge. Beautiful readable playable map, not a poster. No text, no labels, no UI, no borders, no people. Empty small areas directly beneath each landmark for labels rendered in game. Keep all five landmark positions accurate.

## 조작

- 하루 종료: 다음 날 전체 화면 영지 지도.
- 왼쪽 위 숲: 산딸기·목재 수집.
- 왼쪽 아래 농장: 곡물·고기·가죽 수집.
- 가운데 길드: 낮 종료 후 같은 날 저녁 영업.
- 오른쪽 위 광산: 철광석 수집.
- 오른쪽 아래 마을: 의뢰 목록과 접수.
- 오른쪽 아래 제작 작업대 버튼: 기존 제작 기능.
- 각 활동 화면의 ‘영지 지도로’: 낮을 끝내지 않고 지도 복귀.

지도는 팝업이 아닌 별도 전체 화면 상태입니다. 지역 그림과 배경은 하나의 지도 아트이고 각 지역에 클릭 영역과 이름표를 배치했습니다.
