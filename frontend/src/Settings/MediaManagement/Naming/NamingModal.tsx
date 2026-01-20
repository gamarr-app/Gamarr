import React, { useCallback, useState } from 'react';
import FieldSet from 'Components/FieldSet';
import SelectInput from 'Components/Form/SelectInput';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import InlineMarkdown from 'Components/Markdown/InlineMarkdown';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { sizes } from 'Helpers/Props';
import NamingConfig from 'typings/Settings/NamingConfig';
import translate from 'Utilities/String/translate';
import NamingOption from './NamingOption';
import TokenCase from './TokenCase';
import TokenSeparator from './TokenSeparator';
import styles from './NamingModal.css';

const separatorOptions: { key: TokenSeparator; value: string }[] = [
  {
    key: ' ',
    get value() {
      return `${translate('Space')} ( )`;
    },
  },
  {
    key: '.',
    get value() {
      return `${translate('Period')} (.)`;
    },
  },
  {
    key: '_',
    get value() {
      return `${translate('Underscore')} (_)`;
    },
  },
  {
    key: '-',
    get value() {
      return `${translate('Dash')} (-)`;
    },
  },
];

const caseOptions: { key: TokenCase; value: string }[] = [
  {
    key: 'title',
    get value() {
      return translate('DefaultCase');
    },
  },
  {
    key: 'lower',
    get value() {
      return translate('Lowercase');
    },
  },
  {
    key: 'upper',
    get value() {
      return translate('Uppercase');
    },
  },
];

const fileNameTokens = [
  {
    token:
      '{Game Title} ({Release Year}) - {Edition Tags }{[Custom Formats]}{[Quality Full]}{-Release Group}',
    example:
      'The Game - Title (2010) - Ultimate Extended Edition [Surround Sound x264][Bluray-1080p Proper]-EVOLVE',
  },
  {
    token:
      '{Game CleanTitle} {Release Year} - {Edition Tags }{[Custom Formats]}{[Quality Full]}{-Release Group}',
    example:
      'The Game Title 2010 - Ultimate Extended Edition [Surround Sound x264][Bluray-1080p Proper]-EVOLVE',
  },
  {
    token:
      '{Game.CleanTitle}{.Release.Year}{.Edition.Tags}{.Custom.Formats}{.Quality.Full}{-Release Group}',
    example:
      'The.Game.Title.2010.Ultimate.Extended.Edition.Surround.Sound.x264.Bluray-1080p.Proper-EVOLVE',
  },
];

const gameTokens = [
  { token: '{Game Title}', example: "Game's Title", footNotes: '1' },
  { token: '{Game Title:DE}', example: 'Spieltitel', footNotes: '1' },
  { token: '{Game CleanTitle}', example: 'Games Title', footNotes: '1' },
  {
    token: '{Game CleanTitle:DE}',
    example: 'Spieltitel',
    footNotes: '1',
  },
  { token: '{Game TitleThe}', example: "Game's Title, The", footNotes: '1' },
  {
    token: '{Game CleanTitleThe}',
    example: 'Games Title, The',
    footNotes: '1',
  },
  { token: '{Game OriginalTitle}', example: 'ゲームタイトル', footNotes: '1' },
  {
    token: '{Game CleanOriginalTitle}',
    example: 'ゲームタイトル',
    footNotes: '1',
  },
  { token: '{Game TitleFirstCharacter}', example: 'M' },
  { token: '{Game TitleFirstCharacter:DE}', example: 'T' },
  {
    token: '{Game Collection}',
    example: 'The Game Collection',
    footNotes: '1',
  },
  {
    token: '{Game CollectionThe}',
    example: "Game's Collection, The",
    footNotes: '1',
  },
  {
    token: '{Game CleanCollectionThe}',
    example: 'Games Collection, The',
    footNotes: '1',
  },
  { token: '{Game Certification}', example: 'M' },
  { token: '{Release Year}', example: '2009' },
];

const gameIdTokens = [
  { token: '{SteamAppId}', example: '1245620' },
  { token: '{IgdbId}', example: '119133' },
];

const qualityTokens = [
  { token: '{Quality Full}', example: 'HDTV-720p Proper' },
  { token: '{Quality Title}', example: 'HDTV-720p' },
];

const mediaInfoTokens = [
  { token: '{MediaInfo Simple}', example: 'x264 DTS' },
  { token: '{MediaInfo Full}', example: 'x264 DTS [EN+DE]', footNotes: '1' },

  { token: '{MediaInfo AudioCodec}', example: 'DTS' },
  { token: '{MediaInfo AudioChannels}', example: '5.1' },
  {
    token: '{MediaInfo AudioLanguages}',
    example: '[EN+DE]',
    footNotes: '1,2',
  },
  {
    token: '{MediaInfo AudioLanguagesAll}',
    example: '[EN]',
    footNotes: '1',
  },
  { token: '{MediaInfo SubtitleLanguages}', example: '[DE]', footNotes: '1' },

  { token: '{MediaInfo VideoCodec}', example: 'x264' },
  { token: '{MediaInfo VideoBitDepth}', example: '10' },
  { token: '{MediaInfo VideoDynamicRange}', example: 'HDR' },
  { token: '{MediaInfo VideoDynamicRangeType}', example: 'DV HDR10' },
  { token: '{MediaInfo 3D}', example: '3D' },
];

const releaseGroupTokens = [
  { token: '{Release Group}', example: 'Rls Grp', footNotes: '1' },
];

const editionTokens = [
  { token: '{Edition Tags}', example: 'IMAX', footNotes: '1' },
];

const customFormatTokens = [
  { token: '{Custom Formats}', example: 'Surround Sound x264' },
  { token: '{Custom Format:FormatName}', example: 'AMZN' },
];

const originalTokens = [
  { token: '{Original Title}', example: 'Game.Title.HDTV.x264-EVOLVE' },
  { token: '{Original Filename}', example: 'game title hdtv.x264-Evolve' },
];

interface NamingModalProps {
  isOpen: boolean;
  name: keyof Pick<NamingConfig, 'standardGameFormat' | 'gameFolderFormat'>;
  value: string;
  game?: boolean;
  additional?: boolean;
  onInputChange: ({ name, value }: { name: string; value: string }) => void;
  onModalClose: () => void;
}

function NamingModal(props: NamingModalProps) {
  const {
    isOpen,
    name,
    value,
    game = false,
    additional = false,
    onInputChange,
    onModalClose,
  } = props;

  const [tokenSeparator, setTokenSeparator] = useState<TokenSeparator>(' ');
  const [tokenCase, setTokenCase] = useState<TokenCase>('title');
  const [selectionStart, setSelectionStart] = useState<number | null>(null);
  const [selectionEnd, setSelectionEnd] = useState<number | null>(null);

  const handleTokenSeparatorChange = useCallback(
    ({ value }: { value: TokenSeparator }) => {
      setTokenSeparator(value);
    },
    [setTokenSeparator]
  );

  const handleTokenCaseChange = useCallback(
    ({ value }: { value: TokenCase }) => {
      setTokenCase(value);
    },
    [setTokenCase]
  );

  const handleInputSelectionChange = useCallback(
    (selectionStart: number | null, selectionEnd: number | null) => {
      setSelectionStart(selectionStart);
      setSelectionEnd(selectionEnd);
    },
    [setSelectionStart, setSelectionEnd]
  );

  const handleOptionPress = useCallback(
    ({
      isFullFilename,
      tokenValue,
    }: {
      isFullFilename: boolean;
      tokenValue: string;
    }) => {
      if (isFullFilename) {
        onInputChange({ name, value: tokenValue });
      } else if (selectionStart == null || selectionEnd == null) {
        onInputChange({
          name,
          value: `${value}${tokenValue}`,
        });
      } else {
        const start = value.substring(0, selectionStart);
        const end = value.substring(selectionEnd);
        const newValue = `${start}${tokenValue}${end}`;

        onInputChange({ name, value: newValue });

        setSelectionStart(newValue.length - 1);
        setSelectionEnd(newValue.length - 1);
      }
    },
    [name, value, selectionEnd, selectionStart, onInputChange]
  );

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {game ? translate('FileNameTokens') : translate('FolderNameTokens')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.namingSelectContainer}>
            <SelectInput
              className={styles.namingSelect}
              name="separator"
              value={tokenSeparator}
              values={separatorOptions}
              onChange={handleTokenSeparatorChange}
            />

            <SelectInput
              className={styles.namingSelect}
              name="case"
              value={tokenCase}
              values={caseOptions}
              onChange={handleTokenCaseChange}
            />
          </div>

          {game ? (
            <FieldSet legend={translate('FileNames')}>
              <div className={styles.groups}>
                {fileNameTokens.map(({ token, example }) => (
                  <NamingOption
                    key={token}
                    token={token}
                    example={example}
                    isFullFilename={true}
                    tokenSeparator={tokenSeparator}
                    tokenCase={tokenCase}
                    size={sizes.LARGE}
                    onPress={handleOptionPress}
                  />
                ))}
              </div>
            </FieldSet>
          ) : null}

          <FieldSet legend={translate('Game')}>
            <div className={styles.groups}>
              {gameTokens.map(({ token, example, footNotes }) => {
                return (
                  <NamingOption
                    key={token}
                    token={token}
                    example={example}
                    footNotes={footNotes}
                    tokenSeparator={tokenSeparator}
                    tokenCase={tokenCase}
                    onPress={handleOptionPress}
                  />
                );
              })}
            </div>

            <div className={styles.footNote}>
              <sup className={styles.identifier}>1</sup>
              <InlineMarkdown data={translate('GameFootNote')} />
            </div>
          </FieldSet>

          <FieldSet legend={translate('GameID')}>
            <div className={styles.groups}>
              {gameIdTokens.map(({ token, example }) => {
                return (
                  <NamingOption
                    key={token}
                    token={token}
                    example={example}
                    tokenSeparator={tokenSeparator}
                    tokenCase={tokenCase}
                    onPress={handleOptionPress}
                  />
                );
              })}
            </div>
          </FieldSet>

          {additional ? (
            <div>
              <FieldSet legend={translate('Quality')}>
                <div className={styles.groups}>
                  {qualityTokens.map(({ token, example }) => {
                    return (
                      <NamingOption
                        key={token}
                        token={token}
                        example={example}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={handleOptionPress}
                      />
                    );
                  })}
                </div>
              </FieldSet>

              <FieldSet legend={translate('MediaInfo')}>
                <div className={styles.groups}>
                  {mediaInfoTokens.map(({ token, example, footNotes }) => {
                    return (
                      <NamingOption
                        key={token}
                        token={token}
                        example={example}
                        footNotes={footNotes}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={handleOptionPress}
                      />
                    );
                  })}
                </div>

                <div className={styles.footNote}>
                  <sup className={styles.identifier}>1</sup>
                  <InlineMarkdown data={translate('MediaInfoFootNote')} />
                </div>

                <div className={styles.footNote}>
                  <sup className={styles.identifier}>2</sup>
                  <InlineMarkdown data={translate('MediaInfoFootNote2')} />
                </div>
              </FieldSet>

              <FieldSet legend={translate('ReleaseGroup')}>
                <div className={styles.groups}>
                  {releaseGroupTokens.map(({ token, example, footNotes }) => {
                    return (
                      <NamingOption
                        key={token}
                        token={token}
                        example={example}
                        footNotes={footNotes}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={handleOptionPress}
                      />
                    );
                  })}
                </div>

                <div className={styles.footNote}>
                  <sup className={styles.identifier}>1</sup>
                  <InlineMarkdown data={translate('ReleaseGroupFootNote')} />
                </div>
              </FieldSet>

              <FieldSet legend={translate('Edition')}>
                <div className={styles.groups}>
                  {editionTokens.map(({ token, example, footNotes }) => {
                    return (
                      <NamingOption
                        key={token}
                        token={token}
                        example={example}
                        footNotes={footNotes}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={handleOptionPress}
                      />
                    );
                  })}
                </div>

                <div className={styles.footNote}>
                  <sup className={styles.identifier}>1</sup>
                  <InlineMarkdown data={translate('EditionFootNote')} />
                </div>
              </FieldSet>

              <FieldSet legend={translate('CustomFormats')}>
                <div className={styles.groups}>
                  {customFormatTokens.map(({ token, example }) => {
                    return (
                      <NamingOption
                        key={token}
                        token={token}
                        example={example}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={handleOptionPress}
                      />
                    );
                  })}
                </div>
              </FieldSet>

              <FieldSet legend={translate('Original')}>
                <div className={styles.groups}>
                  {originalTokens.map(({ token, example }) => {
                    return (
                      <NamingOption
                        key={token}
                        token={token}
                        example={example}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        size={sizes.LARGE}
                        onPress={handleOptionPress}
                      />
                    );
                  })}
                </div>
              </FieldSet>
            </div>
          ) : null}
        </ModalBody>

        <ModalFooter>
          <TextInput
            name={name}
            value={value}
            onChange={onInputChange}
            onSelectionChange={handleInputSelectionChange}
          />

          <Button onPress={onModalClose}>{translate('Close')}</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default NamingModal;
