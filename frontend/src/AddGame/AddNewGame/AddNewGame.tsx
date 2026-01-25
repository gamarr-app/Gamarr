import React, { Component } from 'react';
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
import AddNewGameSearchResultConnector from './AddNewGameSearchResultConnector';
import styles from './AddNewGame.css';

interface AddNewGameItem {
  titleSlug: string;
  igdbId: number;
  [key: string]: unknown;
}

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

interface AddNewGameState {
  term: string;
  isFetching: boolean;
}

class AddNewGame extends Component<AddNewGameProps, AddNewGameState> {
  //
  // Lifecycle

  constructor(props: AddNewGameProps) {
    super(props);

    this.state = {
      term: props.term || '',
      isFetching: false,
    };
  }

  componentDidMount() {
    const term = this.state.term;

    if (term) {
      this.props.onGameLookupChange(term);
    }
  }

  componentDidUpdate(prevProps: AddNewGameProps) {
    const { term, isFetching } = this.props;

    if (term && term !== prevProps.term) {
      this.setState({
        term,
        isFetching: true,
      });
      this.props.onGameLookupChange(term);
    } else if (isFetching !== prevProps.isFetching) {
      this.setState({
        isFetching,
      });
    }
  }

  //
  // Listeners

  onSearchInputChange = ({ value }: { value: string }) => {
    const hasValue = !!value.trim();

    this.setState({ term: value, isFetching: hasValue }, () => {
      if (hasValue) {
        this.props.onGameLookupChange(value);
      } else {
        this.props.onClearGameLookup();
      }
    });
  };

  onClearGameLookupPress = () => {
    this.setState({ term: '' });
    this.props.onClearGameLookup();
  };

  //
  // Render

  render() {
    const { error, items, hasExistingGames } = this.props;

    const term = this.state.term;
    const isFetching = this.state.isFetching;

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
              onChange={this.onSearchInputChange}
            />

            <Button
              className={styles.clearLookupButton}
              onPress={this.onClearGameLookupPress}
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
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  <AddNewGameSearchResultConnector
                    key={item.titleSlug || item.igdbId}
                    {...(item as any)}
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
              <div className={styles.helpText}>
                {translate('AddNewMessage')}
              </div>
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
}

export default AddNewGame;
