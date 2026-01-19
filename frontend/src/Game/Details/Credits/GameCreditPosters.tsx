import React, { useCallback, useMemo } from 'react';
import { Navigation } from 'swiper';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Swiper as SwiperClass } from 'swiper/types';
import dimensions from 'Styles/Variables/dimensions';
import GameCredit from 'typings/GameCredit';
import GameCreditPoster from './GameCreditPoster';
import styles from './GameCreditPosters.css';

// Import Swiper styles
import 'swiper/css';
import 'swiper/css/navigation';

// Poster container dimensions
const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);

interface GameCreditPostersProps {
  items: GameCredit[];
  itemComponent: React.ElementType;
  isSmallScreen: boolean;
}

function GameCreditPosters(props: GameCreditPostersProps) {
  const { items, itemComponent, isSmallScreen } = props;

  const posterWidth = 162;
  const posterHeight = 238;

  const rowHeight = useMemo(() => {
    const titleHeight = 19;
    const characterHeight = 19;

    const heights = [
      posterHeight,
      titleHeight,
      characterHeight,
      isSmallScreen ? columnPaddingSmallScreen : columnPadding,
    ];

    return heights.reduce((acc, height) => acc + height, 0);
  }, [posterHeight, isSmallScreen]);

  const handleSwiperInit = useCallback((swiper: SwiperClass) => {
    swiper.navigation.init();
    swiper.navigation.update();
  }, []);

  return (
    <div className={styles.sliderContainer}>
      <Swiper
        slidesPerView="auto"
        spaceBetween={10}
        slidesPerGroup={isSmallScreen ? 1 : 3}
        navigation={true}
        loop={false}
        loopFillGroupWithBlank={true}
        className="mySwiper"
        modules={[Navigation]}
        onInit={handleSwiperInit}
      >
        {items.map((credit) => (
          <SwiperSlide
            key={credit.id}
            style={{ width: posterWidth, height: rowHeight }}
          >
            <GameCreditPoster
              key={credit.id}
              component={itemComponent}
              posterWidth={posterWidth}
              posterHeight={posterHeight}
              igdbId={credit.personIgdbId}
              personName={credit.personName}
              images={credit.images}
              job={credit.job}
              character={credit.character}
            />
          </SwiperSlide>
        ))}
      </Swiper>
    </div>
  );
}

export default GameCreditPosters;
