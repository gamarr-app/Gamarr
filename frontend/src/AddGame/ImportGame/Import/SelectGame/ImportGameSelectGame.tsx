import { useCallback, useEffect, useRef, useState } from 'react';
import { Manager, Popper, Reference } from 'react-popper';
import FormInputButton from 'Components/Form/FormInputButton';
import TextInput from 'Components/Form/TextInput';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Portal from 'Components/Portal';
import { icons, kinds } from 'Helpers/Props';
import getUniqueElememtId from 'Utilities/getUniqueElementId';
import translate from 'Utilities/String/translate';
import ImportGameSearchResultConnector from './ImportGameSearchResultConnector';
import ImportGameTitle from './ImportGameTitle';
import styles from './ImportGameSelectGame.css';

interface SelectedGame {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
  [key: string]: unknown;
}

interface GameItem {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
}

interface ImportError {
  responseJSON?: {
    message?: string;
  };
}

interface ImportGameSelectGameProps {
  id: string;
  selectedGame?: SelectedGame;
  isExistingGame: boolean;
  isFetching: boolean;
  isPopulated: boolean;
  error?: ImportError;
  items: GameItem[];
  isQueued: boolean;
  isLookingUpGame: boolean;
  onSearchInputChange: (term: string) => void;
  onGameSelect: (igdbId: number) => void;
}

function ImportGameSelectGame(props: ImportGameSelectGameProps) {
  const {
    id,
    selectedGame,
    isExistingGame,
    isFetching,
    isPopulated,
    error,
    items,
    isQueued,
    isLookingUpGame,
    onSearchInputChange,
    onGameSelect,
  } = props;

  const [term, setTerm] = useState(id);
  const [isOpen, setIsOpen] = useState(false);

  const gameLookupTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(
    null
  );
  const scheduleUpdateRef = useRef<(() => void) | null>(null);
  const buttonIdRef = useRef(getUniqueElememtId());
  const contentIdRef = useRef(getUniqueElememtId());

  const buttonId = buttonIdRef.current;
  const contentId = contentIdRef.current;

  // Update popper position when content changes
  useEffect(() => {
    if (scheduleUpdateRef.current) {
      scheduleUpdateRef.current();
    }
  });

  // Handle click outside to close
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleWindowClick = (event: MouseEvent) => {
      const button = document.getElementById(buttonId);
      const content = document.getElementById(contentId);

      if (!button || !content) {
        return;
      }

      if (
        !button.contains(event.target as Node) &&
        !content.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    window.addEventListener('click', handleWindowClick);

    return () => {
      window.removeEventListener('click', handleWindowClick);
    };
  }, [isOpen, buttonId, contentId]);

  const handlePress = useCallback(() => {
    setIsOpen((prev) => !prev);
  }, []);

  const handleSearchInputChange = useCallback(
    ({ value }: { value: string }) => {
      if (gameLookupTimeoutRef.current) {
        clearTimeout(gameLookupTimeoutRef.current);
      }

      setTerm(value);

      gameLookupTimeoutRef.current = setTimeout(() => {
        onSearchInputChange(value);
      }, 200);
    },
    [onSearchInputChange]
  );

  const handleRefreshPress = useCallback(() => {
    onSearchInputChange(term);
  }, [onSearchInputChange, term]);

  const handleGameSelect = useCallback(
    (igdbId: number) => {
      setIsOpen(false);
      onGameSelect(igdbId);
    },
    [onGameSelect]
  );

  const errorMessage =
    error && error.responseJSON && error.responseJSON.message;

  return (
    <Manager>
      <Reference>
        {({ ref }) => (
          <div ref={ref} id={buttonId}>
            <Link
              className={styles.button}
              component="div"
              onPress={handlePress}
            >
              {isLookingUpGame && isQueued && !isPopulated ? (
                <LoadingIndicator className={styles.loading} size={20} />
              ) : null}

              {isPopulated && selectedGame && isExistingGame ? (
                <Icon
                  className={styles.warningIcon}
                  name={icons.WARNING}
                  kind={kinds.WARNING}
                />
              ) : null}

              {isPopulated && selectedGame ? (
                <ImportGameTitle
                  title={selectedGame.title}
                  year={selectedGame.year}
                  studio={selectedGame.studio}
                  isExistingGame={isExistingGame}
                />
              ) : null}

              {isPopulated && !selectedGame ? (
                <div className={styles.noMatches}>
                  <Icon
                    className={styles.warningIcon}
                    name={icons.WARNING}
                    kind={kinds.WARNING}
                  />

                  {translate('NoMatchFound')}
                </div>
              ) : null}

              {!isFetching && !!error ? (
                <div>
                  <Icon
                    className={styles.warningIcon}
                    title={errorMessage}
                    name={icons.WARNING}
                    kind={kinds.WARNING}
                  />

                  {translate('SearchFailedPleaseTryAgainLater')}
                </div>
              ) : null}

              <div className={styles.dropdownArrowContainer}>
                <Icon name={icons.CARET_DOWN} />
              </div>
            </Link>
          </div>
        )}
      </Reference>

      <Portal>
        <Popper
          placement="bottom"
          modifiers={{
            preventOverflow: {
              boundariesElement: 'viewport',
            },
          }}
        >
          {({ ref, style, scheduleUpdate }) => {
            scheduleUpdateRef.current = scheduleUpdate;

            return (
              <div
                ref={ref}
                id={contentId}
                className={styles.contentContainer}
                style={style}
              >
                {isOpen ? (
                  <div className={styles.content}>
                    <div className={styles.searchContainer}>
                      <div className={styles.searchIconContainer}>
                        <Icon name={icons.SEARCH} />
                      </div>

                      <TextInput
                        className={styles.searchInput}
                        name={`${id}_textInput`}
                        value={term}
                        onChange={handleSearchInputChange}
                      />

                      <FormInputButton
                        kind={kinds.DEFAULT}
                        canSpin={true}
                        isSpinning={isFetching}
                        onPress={handleRefreshPress}
                      >
                        <Icon name={icons.REFRESH} />
                      </FormInputButton>
                    </div>

                    <div className={styles.results}>
                      {items.map((item) => {
                        return (
                          <ImportGameSearchResultConnector
                            key={item.igdbId}
                            igdbId={item.igdbId}
                            steamAppId={0}
                            title={item.title}
                            year={item.year}
                            studio={item.studio}
                            onPress={handleGameSelect}
                          />
                        );
                      })}
                    </div>
                  </div>
                ) : null}
              </div>
            );
          }}
        </Popper>
      </Portal>
    </Manager>
  );
}

export default ImportGameSelectGame;
