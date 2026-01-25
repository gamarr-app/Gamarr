import _ from 'lodash';
import { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { Error as AppError } from 'App/State/AppSectionState';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import Measure from 'Components/Measure';
import PageSectionContent from 'Components/Page/PageSectionContent';
import Scroller from 'Components/Scroller/Scroller';
import { icons, scrollDirections } from 'Helpers/Props';
import {
  deleteDelayProfile,
  fetchDelayProfiles,
  reorderDelayProfile,
} from 'Store/Actions/settingsActions';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import translate from 'Utilities/String/translate';
import DelayProfile from './DelayProfile';
import DelayProfileDragPreview from './DelayProfileDragPreview';
import DelayProfileDragSource from './DelayProfileDragSource';
import EditDelayProfileModal from './EditDelayProfileModal';
import styles from './DelayProfiles.css';

interface DelayProfileItem {
  id: number;
  order: number;
  enableUsenet: boolean;
  enableTorrent: boolean;
  preferredProtocol: string;
  usenetDelay: number;
  torrentDelay: number;
  tags: number[];
}

function createDelayProfilesSelector() {
  return createSelector(
    (state: {
      settings: {
        delayProfiles: {
          items: DelayProfileItem[];
          isFetching: boolean;
          isPopulated: boolean;
          error: AppError | undefined;
        };
      };
    }) => state.settings.delayProfiles,
    createTagsSelector(),
    (delayProfiles, tagList) => {
      const defaultProfile = _.find(delayProfiles.items, { id: 1 });
      const items = _.sortBy(_.reject(delayProfiles.items, { id: 1 }), [
        'order',
      ]);

      return {
        defaultProfile,
        isFetching: delayProfiles.isFetching,
        isPopulated: delayProfiles.isPopulated,
        error: delayProfiles.error,
        items,
        tagList,
      };
    }
  );
}

function DelayProfiles() {
  const dispatch = useDispatch();
  const { defaultProfile, isFetching, isPopulated, error, items, tagList } =
    useSelector(createDelayProfilesSelector());

  const [isAddDelayProfileModalOpen, setIsAddDelayProfileModalOpen] =
    useState(false);
  const [width, setWidth] = useState(0);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [dropIndex, setDropIndex] = useState<number | null>(null);

  useEffect(() => {
    dispatch(fetchDelayProfiles());
  }, [dispatch]);

  const handleAddDelayProfilePress = useCallback(() => {
    setIsAddDelayProfileModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsAddDelayProfileModalOpen(false);
  }, []);

  const handleMeasure = useCallback(({ width: w }: { width: number }) => {
    setWidth(w);
  }, []);

  const handleConfirmDeleteDelayProfile = useCallback(
    (id: number) => {
      dispatch(deleteDelayProfile({ id }));
    },
    [dispatch]
  );

  const handleDelayProfileDragMove = useCallback(
    (newDragIndex: number, newDropIndex: number) => {
      setDragIndex(newDragIndex);
      setDropIndex(newDropIndex);
    },
    []
  );

  const handleDelayProfileDragEnd = useCallback(
    ({ id }: { id: number }, didDrop: boolean) => {
      if (didDrop && dropIndex !== null) {
        dispatch(reorderDelayProfile({ id, moveIndex: dropIndex - 1 }));
      }

      setDragIndex(null);
      setDropIndex(null);
    },
    [dispatch, dropIndex]
  );

  const isDragging = dropIndex !== null;
  const isDraggingUp =
    isDragging && dragIndex !== null && dropIndex < dragIndex;
  const isDraggingDown =
    isDragging && dragIndex !== null && dropIndex > dragIndex;

  return (
    <Measure onMeasure={handleMeasure}>
      <FieldSet legend={translate('DelayProfiles')}>
        <PageSectionContent
          errorMessage={translate('DelayProfilesLoadError')}
          isFetching={isFetching}
          isPopulated={isPopulated}
          error={error}
        >
          <Scroller
            className={styles.horizontalScroll}
            scrollDirection={scrollDirections.HORIZONTAL}
            autoFocus={false}
          >
            <div>
              <div className={styles.delayProfilesHeader}>
                <div className={styles.column}>
                  {translate('PreferredProtocol')}
                </div>
                <div className={styles.column}>{translate('UsenetDelay')}</div>
                <div className={styles.column}>{translate('TorrentDelay')}</div>
                <div className={styles.tags}>{translate('Tags')}</div>
              </div>

              <div className={styles.delayProfiles}>
                {items.map((item, index) => {
                  return (
                    <DelayProfileDragSource
                      key={item.id}
                      tagList={tagList}
                      {...item}
                      index={index}
                      isDragging={isDragging}
                      isDraggingUp={isDraggingUp}
                      isDraggingDown={isDraggingDown}
                      onConfirmDeleteDelayProfile={
                        handleConfirmDeleteDelayProfile
                      }
                      onDelayProfileDragMove={handleDelayProfileDragMove}
                      onDelayProfileDragEnd={handleDelayProfileDragEnd}
                    />
                  );
                })}

                <DelayProfileDragPreview width={width} />
              </div>

              {defaultProfile ? (
                <div>
                  <DelayProfile
                    tagList={tagList}
                    isDragging={false}
                    onConfirmDeleteDelayProfile={
                      handleConfirmDeleteDelayProfile
                    }
                    {...defaultProfile}
                  />
                </div>
              ) : null}
            </div>
          </Scroller>

          <div className={styles.addDelayProfile}>
            <Link
              className={styles.addButton}
              onPress={handleAddDelayProfilePress}
            >
              <Icon name={icons.ADD} />
            </Link>
          </div>

          <EditDelayProfileModal
            isOpen={isAddDelayProfileModalOpen}
            onModalClose={handleModalClose}
          />
        </PageSectionContent>
      </FieldSet>
    </Measure>
  );
}

export default DelayProfiles;
