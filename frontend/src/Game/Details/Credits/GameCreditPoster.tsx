import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  selectImportListSchema,
  setImportListFieldValue,
  setImportListValue,
} from 'Store/Actions/settingsActions';
import createGameCreditImportListSelector from 'Store/Selectors/createGameCreditImportListSelector';
import { GameCastPosterProps } from './Cast/GameCastPoster';
import { GameCrewPosterProps } from './Crew/GameCrewPoster';

type GameCreditPosterProps = {
  component: React.ElementType;
} & (
  | Omit<GameCrewPosterProps, 'onImportListSelect'>
  | Omit<GameCastPosterProps, 'onImportListSelect'>
);

function GameCreditPoster({
  component: ItemComponent,
  igdbId,
  personName,
  ...otherProps
}: GameCreditPosterProps) {
  const importList = useSelector(createGameCreditImportListSelector(igdbId));

  const dispatch = useDispatch();

  const handleImportListSelect = useCallback(() => {
    dispatch(
      selectImportListSchema({
        implementation: 'TMDbPersonImport',
        implementationName: 'TMDb Person',
        presetName: undefined,
      })
    );

    dispatch(
      // @ts-expect-error 'setImportListFieldValue' isn't typed yet
      setImportListFieldValue({ name: 'personId', value: igdbId.toString() })
    );

    dispatch(
      // @ts-expect-error 'setImportListValue' isn't typed yet
      setImportListValue({ name: 'name', value: `${personName} - ${igdbId}` })
    );
  }, [dispatch, igdbId, personName]);

  return (
    <ItemComponent
      {...otherProps}
      igdbId={igdbId}
      personName={personName}
      importList={importList}
      onImportListSelect={handleImportListSelect}
    />
  );
}

export default GameCreditPoster;
