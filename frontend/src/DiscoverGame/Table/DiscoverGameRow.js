import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import ImportListList from 'Components/ImportListList';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import MetacriticRating from 'Components/MetacriticRating';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import Popover from 'Components/Tooltip/Popover';
import AddNewDiscoverGameModal from 'DiscoverGame/AddNewDiscoverGameModal';
import ExcludeGameModal from 'DiscoverGame/Exclusion/ExcludeGameModal';
import GameDetailsLinks from 'Game/Details/GameDetailsLinks';
import GamePopularityIndex from 'Game/GamePopularityIndex';
import { icons } from 'Helpers/Props';
import formatRuntime from 'Utilities/Date/formatRuntime';
import translate from 'Utilities/String/translate';
import ListGameStatusCell from './ListGameStatusCell';
import styles from './DiscoverGameRow.css';

class DiscoverGameRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNewAddGameModalOpen: false,
      isExcludeGameModalOpen: false
    };
  }

  //
  // Listeners

  onAddGamePress = () => {
    this.setState({ isNewAddGameModalOpen: true });
  };

  onAddGameModalClose = () => {
    this.setState({ isNewAddGameModalOpen: false });
  };

  onExcludeGamePress = () => {
    this.setState({ isExcludeGameModalOpen: true });
  };

  onExcludeGameModalClose = () => {
    this.setState({ isExcludeGameModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      status,
      igdbId,
      imdbId,
      youTubeTrailerId,
      title,
      originalLanguage,
      studio,
      inCinemas,
      physicalRelease,
      digitalRelease,
      runtime,
      year,
      overview,
      folder,
      images,
      genres,
      ratings,
      popularity,
      certification,
      gameRuntimeFormat,
      collection,
      columns,
      isExisting,
      isExcluded,
      isRecommendation,
      isTrending,
      isPopular,
      isSelected,
      lists,
      onSelectedChange
    } = this.props;

    const {
      isNewAddGameModalOpen,
      isExcludeGameModalOpen
    } = this.state;

    const linkProps = isExisting ? { to: `/game/${igdbId}` } : { onPress: this.onAddGamePress };

    return (
      <>
        <VirtualTableSelectCell
          inputClassName={styles.checkInput}
          id={igdbId}
          key={name}
          isSelected={isSelected}
          isDisabled={false}
          onSelectedChange={onSelectedChange}
        />

        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'status') {
              return (
                <ListGameStatusCell
                  key={name}
                  className={styles[name]}
                  status={status}
                  isExclusion={isExcluded}
                  isExisting={isExisting}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'sortTitle') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <Link
                    {...linkProps}
                  >
                    {title}
                  </Link>

                  {
                    isExisting ?
                      <Icon
                        className={styles.alreadyExistsIcon}
                        name={icons.CHECK_CIRCLE}
                        title={translate('AlreadyInYourLibrary')}
                      /> : null
                  }

                  {
                    isExcluded ?
                      <Icon
                        className={styles.exclusionIcon}
                        name={icons.DANGER}
                        title={translate('GameExcludedFromAutomaticAdd')}
                      /> : null
                  }
                </VirtualTableRowCell>
              );
            }

            if (name === 'year') {
              return (
                <VirtualTableRowCell key={name} className={styles[name]}>
                  {year}
                </VirtualTableRowCell>
              );
            }

            if (name === 'collection') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {collection ? collection.title : null }
                </VirtualTableRowCell>
              );
            }

            if (name === 'originalLanguage') {
              return (
                <VirtualTableRowCell key={name} className={styles[name]}>
                  {originalLanguage.name}
                </VirtualTableRowCell>
              );
            }

            if (name === 'studio') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {studio}
                </VirtualTableRowCell>
              );
            }

            if (name === 'inCinemas') {
              return (
                <RelativeDateCell
                  key={name}
                  className={styles[name]}
                  date={inCinemas}
                  timeForToday={false}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'physicalRelease') {
              return (
                <RelativeDateCell
                  key={name}
                  className={styles[name]}
                  date={physicalRelease}
                  timeForToday={false}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'digitalRelease') {
              return (
                <RelativeDateCell
                  key={name}
                  className={styles[name]}
                  date={digitalRelease}
                  timeForToday={false}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'runtime') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {formatRuntime(runtime, gameRuntimeFormat)}
                </VirtualTableRowCell>
              );
            }

            if (name === 'genres') {
              const joinedGenres = genres.join(', ');

              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <span title={joinedGenres}>
                    {joinedGenres}
                  </span>
                </VirtualTableRowCell>
              );
            }

            if (name === 'igdbRating') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {ratings.igdb ? <IgdbRating ratings={ratings} /> : null}
                </VirtualTableRowCell>
              );
            }

            if (name === 'metacriticRating') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {ratings.metacritic ? <MetacriticRating ratings={ratings} /> : null}
                </VirtualTableRowCell>
              );
            }

            if (name === 'popularity') {
              return (
                <VirtualTableRowCell key={name} className={styles[name]}>
                  <GamePopularityIndex popularity={popularity} />
                </VirtualTableRowCell>
              );
            }

            if (name === 'certification') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {certification}
                </VirtualTableRowCell>
              );
            }

            if (name === 'lists') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <ImportListList
                    lists={lists}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'isRecommendation') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {
                    isRecommendation ?
                      <Icon
                        className={styles.statusIcon}
                        name={icons.RECOMMENDED}
                        size={12}
                        title={translate('GameIsRecommend')}
                      /> :
                      null
                  }
                </VirtualTableRowCell>
              );
            }

            if (name === 'isTrending') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {
                    isTrending ?
                      <Icon
                        className={styles.statusIcon}
                        name={icons.TRENDING}
                        size={12}
                        title={translate('GameIsTrending')}
                      /> :
                      null
                  }
                </VirtualTableRowCell>
              );
            }

            if (name === 'isPopular') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {
                    isPopular ?
                      <Icon
                        className={styles.statusIcon}
                        name={icons.POPULAR}
                        size={12}
                        title={translate('GameIsPopular')}
                      /> :
                      null
                  }
                </VirtualTableRowCell>
              );
            }

            if (name === 'actions') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <span className={styles.externalLinks}>
                    <Popover
                      anchor={
                        <Icon
                          name={icons.EXTERNAL_LINK}
                          size={12}
                        />
                      }
                      title={translate('Links')}
                      body={
                        <GameDetailsLinks
                          igdbId={igdbId}
                          imdbId={imdbId}
                          youTubeTrailerId={youTubeTrailerId}
                        />
                      }
                    />
                  </span>

                  <IconButton
                    name={icons.REMOVE}
                    title={isExcluded ? translate('GameAlreadyExcluded') : translate('ExcludeGame')}
                    onPress={this.onExcludeGamePress}
                    isDisabled={isExcluded}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }

        <AddNewDiscoverGameModal
          isOpen={isNewAddGameModalOpen && !isExisting}
          igdbId={igdbId}
          title={title}
          year={year}
          overview={overview}
          folder={folder}
          images={images}
          onModalClose={this.onAddGameModalClose}
        />

        <ExcludeGameModal
          isOpen={isExcludeGameModalOpen}
          igdbId={igdbId}
          title={title}
          year={year}
          onModalClose={this.onExcludeGameModalClose}
        />
      </>
    );
  }
}

DiscoverGameRow.propTypes = {
  igdbId: PropTypes.number.isRequired,
  imdbId: PropTypes.string,
  youTubeTrailerId: PropTypes.string,
  status: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  originalLanguage: PropTypes.object.isRequired,
  year: PropTypes.number.isRequired,
  overview: PropTypes.string.isRequired,
  folder: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  studio: PropTypes.string,
  inCinemas: PropTypes.string,
  physicalRelease: PropTypes.string,
  digitalRelease: PropTypes.string,
  runtime: PropTypes.number,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  ratings: PropTypes.object.isRequired,
  popularity: PropTypes.number.isRequired,
  certification: PropTypes.string,
  collection: PropTypes.object,
  gameRuntimeFormat: PropTypes.string.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isExisting: PropTypes.bool.isRequired,
  isExcluded: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  isRecommendation: PropTypes.bool.isRequired,
  isPopular: PropTypes.bool.isRequired,
  isTrending: PropTypes.bool.isRequired,
  lists: PropTypes.arrayOf(PropTypes.number).isRequired,
  onSelectedChange: PropTypes.func.isRequired
};

DiscoverGameRow.defaultProps = {
  genres: [],
  lists: []
};

export default DiscoverGameRow;
