import React from 'react';
import { ParseModel } from 'App/State/ParseAppState';
import FieldSet from 'Components/FieldSet';
import GameFormats from 'Game/GameFormats';
import GameTitleLink from 'Game/GameTitleLink';
import translate from 'Utilities/String/translate';
import ParseResultItem from './ParseResultItem';
import styles from './ParseResult.css';

interface ParseResultProps {
  item: ParseModel;
}

function ParseResult(props: ParseResultProps) {
  const { item } = props;
  const { customFormats, customFormatScore, languages, parsedGameInfo, game } =
    item;

  const {
    releaseTitle,
    gameTitle,
    gameTitles,
    year,
    edition,
    releaseGroup,
    releaseHash,
    quality,
    igdbId,
    steamAppId,
  } = parsedGameInfo;

  const finalLanguages = languages ?? parsedGameInfo.languages;

  return (
    <div>
      <FieldSet legend={translate('Release')}>
        <ParseResultItem
          title={translate('ReleaseTitle')}
          data={releaseTitle}
        />

        <ParseResultItem title={translate('GameTitle')} data={gameTitle} />

        <ParseResultItem
          title={translate('Year')}
          data={year > 0 ? year : '-'}
        />

        <ParseResultItem
          title={translate('Edition')}
          data={edition ? edition : '-'}
        />

        <ParseResultItem
          title={translate('AllTitles')}
          data={gameTitles?.length > 0 ? gameTitles.join(', ') : '-'}
        />

        <ParseResultItem
          title={translate('ReleaseGroup')}
          data={releaseGroup ?? '-'}
        />

        <ParseResultItem
          title={translate('ReleaseHash')}
          data={releaseHash ? releaseHash : '-'}
        />

        {steamAppId ? (
          <ParseResultItem title={translate('SteamAppId')} data={steamAppId} />
        ) : null}

        {igdbId ? (
          <ParseResultItem title={translate('IGDBId')} data={igdbId} />
        ) : null}
      </FieldSet>

      <FieldSet legend={translate('Quality')}>
        <div className={styles.container}>
          <div className={styles.column}>
            <ParseResultItem
              title={translate('Quality')}
              data={quality.quality.name}
            />
            <ParseResultItem
              title={translate('Proper')}
              data={
                quality.revision.version > 1 && !quality.revision.isRepack
                  ? 'True'
                  : '-'
              }
            />

            <ParseResultItem
              title={translate('Repack')}
              data={quality.revision.isRepack ? translate('True') : '-'}
            />
          </div>

          <div className={styles.column}>
            <ParseResultItem
              title={translate('Version')}
              data={
                quality.revision.version > 1 ? quality.revision.version : '-'
              }
            />

            <ParseResultItem
              title={translate('Real')}
              data={quality.revision.real ? translate('True') : '-'}
            />
          </div>
        </div>
      </FieldSet>

      <FieldSet legend={translate('Languages')}>
        <ParseResultItem
          title={translate('Languages')}
          data={finalLanguages.map((l) => l.name).join(', ')}
        />
      </FieldSet>

      <FieldSet legend={translate('Details')}>
        <ParseResultItem
          title={translate('MatchedToGame')}
          data={
            game ? (
              <GameTitleLink
                titleSlug={game.titleSlug}
                title={game.title}
                year={game.year}
              />
            ) : (
              '-'
            )
          }
        />

        {game && game.originalLanguage ? (
          <ParseResultItem
            title={translate('OriginalLanguage')}
            data={game.originalLanguage.name}
          />
        ) : null}

        <ParseResultItem
          title={translate('CustomFormats')}
          data={
            customFormats?.length ? (
              <GameFormats formats={customFormats} />
            ) : (
              '-'
            )
          }
        />

        <ParseResultItem
          title={translate('CustomFormatScore')}
          data={customFormatScore}
        />
      </FieldSet>
    </div>
  );
}

export default ParseResult;
