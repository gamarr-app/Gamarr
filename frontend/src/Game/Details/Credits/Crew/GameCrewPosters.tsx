import React from 'react';
import { useSelector } from 'react-redux';
import createGameCreditsSelector from 'Store/Selectors/createGameCreditsSelector';
import GameCreditPosters from '../GameCreditPosters';
import GameCrewPoster from './GameCrewPoster';

interface GameCrewPostersProps {
  isSmallScreen: boolean;
}

function GameCrewPosters({ isSmallScreen }: GameCrewPostersProps) {
  const { items: crewCredits } = useSelector(
    createGameCreditsSelector('crew')
  );

  return (
    <GameCreditPosters
      items={crewCredits}
      itemComponent={GameCrewPoster}
      isSmallScreen={isSmallScreen}
    />
  );
}

export default GameCrewPosters;
