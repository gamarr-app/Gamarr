import PropTypes from 'prop-types';
import React, { Component } from 'react';
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

class ImportGameSelectGame extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._gameLookupTimeout = null;
    this._scheduleUpdate = null;
    this._buttonId = getUniqueElememtId();
    this._contentId = getUniqueElememtId();

    this.state = {
      term: props.id,
      isOpen: false
    };
  }

  componentDidUpdate() {
    if (this._scheduleUpdate) {
      this._scheduleUpdate();
    }
  }

  //
  // Control

  _addListener() {
    window.addEventListener('click', this.onWindowClick);
  }

  _removeListener() {
    window.removeEventListener('click', this.onWindowClick);
  }

  //
  // Listeners

  onWindowClick = (event) => {
    const button = document.getElementById(this._buttonId);
    const content = document.getElementById(this._contentId);

    if (!button || !content) {
      return;
    }

    if (
      !button.contains(event.target) &&
      !content.contains(event.target) &&
      this.state.isOpen
    ) {
      this.setState({ isOpen: false });
      this._removeListener();
    }
  };

  onPress = () => {
    if (this.state.isOpen) {
      this._removeListener();
    } else {
      this._addListener();
    }

    this.setState({ isOpen: !this.state.isOpen });
  };

  onSearchInputChange = ({ value }) => {
    if (this._gameLookupTimeout) {
      clearTimeout(this._gameLookupTimeout);
    }

    this.setState({ term: value }, () => {
      this._gameLookupTimeout = setTimeout(() => {
        this.props.onSearchInputChange(value);
      }, 200);
    });
  };

  onRefreshPress = () => {
    this.props.onSearchInputChange(this.state.term);
  };

  onGameSelect = (igdbId) => {
    this.setState({ isOpen: false });

    this.props.onGameSelect(igdbId);
  };

  //
  // Render

  render() {
    const {
      selectedGame,
      isExistingGame,
      isFetching,
      isPopulated,
      error,
      items,
      isQueued,
      isLookingUpGame
    } = this.props;

    const errorMessage = error &&
      error.responseJSON &&
      error.responseJSON.message;

    return (
      <Manager>
        <Reference>
          {({ ref }) => (
            <div
              ref={ref}
              id={this._buttonId}
            >
              <Link
                // ref={ref}
                className={styles.button}
                component="div"
                onPress={this.onPress}
              >
                {
                  isLookingUpGame && isQueued && !isPopulated ?
                    <LoadingIndicator
                      className={styles.loading}
                      size={20}
                    /> :
                    null
                }

                {
                  isPopulated && selectedGame && isExistingGame ?
                    <Icon
                      className={styles.warningIcon}
                      name={icons.WARNING}
                      kind={kinds.WARNING}
                    /> :
                    null
                }

                {
                  isPopulated && selectedGame ?
                    <ImportGameTitle
                      title={selectedGame.title}
                      year={selectedGame.year}
                      studio={selectedGame.studio}
                      isExistingGame={isExistingGame}
                    /> :
                    null
                }

                {
                  isPopulated && !selectedGame ?
                    <div className={styles.noMatches}>
                      <Icon
                        className={styles.warningIcon}
                        name={icons.WARNING}
                        kind={kinds.WARNING}
                      />

                      {translate('NoMatchFound')}
                    </div> :
                    null
                }

                {
                  !isFetching && !!error ?
                    <div>
                      <Icon
                        className={styles.warningIcon}
                        title={errorMessage}
                        name={icons.WARNING}
                        kind={kinds.WARNING}
                      />

                      {translate('SearchFailedPleaseTryAgainLater')}
                    </div> :
                    null
                }

                <div className={styles.dropdownArrowContainer}>
                  <Icon
                    name={icons.CARET_DOWN}
                  />
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
                boundariesElement: 'viewport'
              }
            }}
          >
            {({ ref, style, scheduleUpdate }) => {
              this._scheduleUpdate = scheduleUpdate;

              return (
                <div
                  ref={ref}
                  id={this._contentId}
                  className={styles.contentContainer}
                  style={style}
                >
                  {
                    this.state.isOpen ?
                      <div className={styles.content}>
                        <div className={styles.searchContainer}>
                          <div className={styles.searchIconContainer}>
                            <Icon name={icons.SEARCH} />
                          </div>

                          <TextInput
                            className={styles.searchInput}
                            name={`${name}_textInput`}
                            value={this.state.term}
                            onChange={this.onSearchInputChange}
                          />

                          <FormInputButton
                            kind={kinds.DEFAULT}
                            spinnerIcon={icons.REFRESH}
                            canSpin={true}
                            isSpinning={isFetching}
                            onPress={this.onRefreshPress}
                          >
                            <Icon name={icons.REFRESH} />
                          </FormInputButton>
                        </div>

                        <div className={styles.results}>
                          {
                            items.map((item) => {
                              return (
                                <ImportGameSearchResultConnector
                                  key={item.igdbId}
                                  igdbId={item.igdbId}
                                  title={item.title}
                                  year={item.year}
                                  studio={item.studio}
                                  onPress={this.onGameSelect}
                                />
                              );
                            })
                          }
                        </div>
                      </div> :
                      null
                  }
                </div>
              );
            }}
          </Popper>
        </Portal>
      </Manager>
    );
  }
}

ImportGameSelectGame.propTypes = {
  id: PropTypes.string.isRequired,
  selectedGame: PropTypes.object,
  isExistingGame: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isQueued: PropTypes.bool.isRequired,
  isLookingUpGame: PropTypes.bool.isRequired,
  onSearchInputChange: PropTypes.func.isRequired,
  onGameSelect: PropTypes.func.isRequired
};

ImportGameSelectGame.defaultProps = {
  isFetching: true,
  isPopulated: false,
  items: [],
  isQueued: true
};

export default ImportGameSelectGame;
