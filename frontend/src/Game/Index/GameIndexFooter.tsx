import classNames from 'classnames';
import React from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { ColorImpairedConsumer } from 'App/ColorImpairedContext';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import Game, { GameStatus, Statistics } from 'Game/Game';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './GameIndexFooter.css';

interface FooterGameItem {
  monitored: boolean;
  status: GameStatus;
  hasFile: boolean;
  statistics?: Statistics;
}

function createUnoptimizedSelector() {
  return createSelector(
    (state: AppState) => state.games.items,
    (games: Game[]): FooterGameItem[] => {
      return games.map((m) => {
        const { monitored, status, hasFile, statistics } = m;

        return {
          monitored,
          status,
          hasFile,
          statistics,
        };
      });
    }
  );
}

function createGameSelector() {
  return createDeepEqualSelector(createUnoptimizedSelector(), (games) => games);
}

export default function GameIndexFooter() {
  const games = useSelector(createGameSelector());
  const count = games.length;
  let gameFiles = 0;
  let monitored = 0;
  let totalFileSize = 0;

  games.forEach((m) => {
    const { statistics = { sizeOnDisk: 0 } } = m;

    const { sizeOnDisk = 0 } = statistics;

    if (m.hasFile) {
      gameFiles += 1;
    }

    if (m.monitored) {
      monitored++;
    }

    totalFileSize += sizeOnDisk;
  });

  return (
    <ColorImpairedConsumer>
      {(enableColorImpairedMode) => {
        return (
          <div className={styles.footer}>
            <div>
              <div className={styles.legendItem}>
                <div className={styles.ended} />
                <div>{translate('DownloadedAndMonitored')}</div>
              </div>

              <div className={styles.legendItem}>
                <div className={styles.availNotMonitored} />
                <div>{translate('DownloadedButNotMonitored')}</div>
              </div>

              <div className={styles.legendItem}>
                <div
                  className={classNames(
                    styles.missingMonitored,
                    enableColorImpairedMode && 'colorImpaired'
                  )}
                />
                <div>{translate('MissingMonitoredAndConsideredAvailable')}</div>
              </div>

              <div className={styles.legendItem}>
                <div
                  className={classNames(
                    styles.missingUnmonitored,
                    enableColorImpairedMode && 'colorImpaired'
                  )}
                />
                <div>{translate('MissingNotMonitored')}</div>
              </div>

              <div className={styles.legendItem}>
                <div className={styles.queue} />
                <div>{translate('Queued')}</div>
              </div>

              <div className={styles.legendItem}>
                <div className={styles.continuing} />
                <div>{translate('Unreleased')}</div>
              </div>
            </div>

            <div className={styles.statistics}>
              <DescriptionList>
                <DescriptionListItem title={translate('Games')} data={count} />

                <DescriptionListItem
                  title={translate('GameFiles')}
                  data={gameFiles}
                />
              </DescriptionList>

              <DescriptionList>
                <DescriptionListItem
                  title={translate('Monitored')}
                  data={monitored}
                />

                <DescriptionListItem
                  title={translate('Unmonitored')}
                  data={count - monitored}
                />
              </DescriptionList>

              <DescriptionList>
                <DescriptionListItem
                  title={translate('TotalFileSize')}
                  data={formatBytes(totalFileSize)}
                />
              </DescriptionList>
            </div>
          </div>
        );
      }}
    </ColorImpairedConsumer>
  );
}
