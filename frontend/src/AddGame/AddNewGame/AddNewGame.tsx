import { useCallback, useEffect, useRef, useState } from 'react';
import { Error } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import TextInput from 'Components/Form/TextInput';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { icons, kinds } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import { AddNewGameSearchResultProps } from './AddNewGameSearchResult';
import AddNewGameSearchResultConnector from './AddNewGameSearchResultConnector';
import styles from './AddNewGame.css';

export type AddNewGameItem = Omit<
  AddNewGameSearchResultProps,
  | 'existingGameId'
  | 'isExistingGame'
  | 'isSmallScreen'
  | 'gameFile'
  | 'gameRuntimeFormat'
> & {
  internalId?: number;
};

interface AddNewGameProps {
  term?: string;
  isFetching: boolean;
  error?: Error;
  isAdding: boolean;
  addError?: Error;
  items: AddNewGameItem[];
  hasExistingGames: boolean;
  onGameLookupChange: (term: string) => void;
  onClearGameLookup: () => void;
}

function AddNewGame(props: AddNewGameProps) {
  const {
    term: termProp,
    isFetching: isFetchingProp,
    error,
    items,
    hasExistingGames,
    onGameLookupChange,
    onClearGameLookup,
  } = props;

  const [term, setTerm] = useState(termProp || '');
  const [isFetching, setIsFetching] = useState(false);

  const prevTermPropRef = useRef(termProp);
  const prevIsFetchingPropRef = useRef(isFetchingProp);

  // Mount effect - perform initial lookup if term exists
  useEffect(() => {
    if (term) {
      onGameLookupChange(term);
    }
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Update effect - sync with prop changes
  useEffect(() => {
    if (termProp && termProp !== prevTermPropRef.current) {
      setTerm(termProp);
      setIsFetching(true);
      onGameLookupChange(termProp);
    } else if (isFetchingProp !== prevIsFetchingPropRef.current) {
      setIsFetching(isFetchingProp);
    }

    prevTermPropRef.current = termProp;
    prevIsFetchingPropRef.current = isFetchingProp;
  }, [termProp, isFetchingProp, onGameLookupChange]);

  const onSearchInputChange = useCallback(
    ({ value }: { value: string }) => {
      const hasValue = !!value.trim();

      setTerm(value);
      setIsFetching(hasValue);

      if (hasValue) {
        onGameLookupChange(value);
      } else {
        onClearGameLookup();
      }
    },
    [onGameLookupChange, onClearGameLookup]
  );

  const onClearGameLookupPress = useCallback(() => {
    setTerm('');
    onClearGameLookup();
  }, [onClearGameLookup]);

  return (
    <PageContent title={translate('AddNewGame')}>
      <PageContentBody>
        <div className={styles.searchContainer}>
          <div className={styles.searchIconContainer}>
            <Icon name={icons.SEARCH} size={20} />
          </div>

          <TextInput
            className={styles.searchInput}
            name="gameLookup"
            value={term}
            placeholder="e.g. Elden Ring, steam:1245620, igdb:119133"
            autoFocus={true}
            onChange={onSearchInputChange}
          />

          <Button
            className={styles.clearLookupButton}
            onPress={onClearGameLookupPress}
          >
            <Icon name={icons.REMOVE} size={20} />
          </Button>
        </div>

        {isFetching && <LoadingIndicator />}

        {!isFetching && !!error ? (
          <div className={styles.message}>
            <div className={styles.helpText}>
              {translate('FailedLoadingSearchResults')}
            </div>

            <Alert kind={kinds.DANGER}>{getErrorMessage(error)}</Alert>

            <div>
              <Link to="https://wiki.servarr.com/gamarr/troubleshooting#invalid-response-received-from-igdb">
                {translate('WhySearchesCouldBeFailing')}
              </Link>
            </div>
          </div>
        ) : null}

        {!isFetching && !error && !!items.length && (
          <div className={styles.searchResults}>
            {items.map((item) => {
              return (
                <AddNewGameSearchResultConnector
                  key={item.titleSlug || item.igdbId}
                  {...item}
                />
              );
            })}
          </div>
        )}

        {!isFetching && !error && !items.length && !!term && (
          <div className={styles.message}>
            <div className={styles.noResults}>
              {translate('CouldNotFindResults', { term })}
            </div>
            <div>{translate('YouCanAlsoSearch')}</div>
            <div>
              <Link to="https://wiki.servarr.com/gamarr/faq#why-can-i-not-add-a-new-game-to-gamarr">
                {translate('CantFindGame')}
              </Link>
            </div>
          </div>
        )}

        {term ? null : (
          <div className={styles.message}>
            <div className={styles.helpText}>{translate('AddNewMessage')}</div>
            <div>{translate('AddNewIgdbIdMessage')}</div>
          </div>
        )}

        {!term && !hasExistingGames ? (
          <div className={styles.message}>
            <div className={styles.noGamesText}>
              {translate('HaveNotAddedGames')}
            </div>
            <div>
              <Button to="/add/import" kind={kinds.PRIMARY}>
                {translate('ImportExistingGames')}
              </Button>
            </div>
          </div>
        ) : null}

        <div />
      </PageContentBody>
    </PageContent>
  );
}

export default AddNewGame;
