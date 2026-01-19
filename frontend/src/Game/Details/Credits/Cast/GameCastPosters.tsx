import React from 'react';
import { useSelector } from 'react-redux';
import createGameCreditsSelector from 'Store/Selectors/createGameCreditsSelector';
import GameCreditPosters from '../GameCreditPosters';
import GameCastPoster from './GameCastPoster';

interface GameCastPostersProps {
  isSmallScreen: boolean;
}

function GameCastPosters({ isSmallScreen }: GameCastPostersProps) {
  const { items: castCredits } = useSelector(
    createGameCreditsSelector('cast')
  );

  return (
    <GameCreditPosters
      items={castCredits}
      itemComponent={GameCastPoster}
      isSmallScreen={isSmallScreen}
    />
  );
}

export default GameCastPosters;
