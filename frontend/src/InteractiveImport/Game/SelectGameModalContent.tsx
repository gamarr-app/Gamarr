import { throttle } from 'lodash';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useSelector } from 'react-redux';
import { FixedSizeList as List, ListChildComponentProps } from 'react-window';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Column from 'Components/Table/Column';
import VirtualTableRowButton from 'Components/Table/VirtualTableRowButton';
import { scrollDirections } from 'Helpers/Props';
import Game from 'Game/Game';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import dimensions from 'Styles/Variables/dimensions';
import { InputChanged } from 'typings/inputs';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import SelectGameModalTableHeader from './SelectGameModalTableHeader';
import SelectGameRow from './SelectGameRow';
import styles from './SelectGameModalContent.css';

const columns = [
  {
    name: 'title',
    label: () => translate('Title'),
    isVisible: true,
  },
  {
    name: 'year',
    label: () => translate('Year'),
    isVisible: true,
  },
  {
    name: 'imdbId',
    label: () => translate('IMDbId'),
    isVisible: true,
  },
  {
    name: 'igdbId',
    label: () => translate('IGDBId'),
    isVisible: true,
  },
];

const bodyPadding = parseInt(dimensions.pageContentBodyPadding);

interface SelectGameModalContentProps {
  modalTitle: string;
  onGameSelect(game: Game): void;
  onModalClose(): void;
}

interface RowItemData {
  items: Game[];
  columns: Column[];
  onGameSelect(gameId: number): void;
}

function Row({ index, style, data }: ListChildComponentProps<RowItemData>) {
  const { items, onGameSelect } = data;
  const game = index >= items.length ? null : items[index];

  const handlePress = useCallback(() => {
    if (game?.id) {
      onGameSelect(game.id);
    }
  }, [game?.id, onGameSelect]);

  if (game == null) {
    return null;
  }

  return (
    <VirtualTableRowButton
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        ...style,
      }}
      onPress={handlePress}
    >
      <SelectGameRow
        key={game.id}
        title={game.title}
        igdbId={game.igdbId}
        imdbId={game.imdbId}
        year={game.year}
      />
    </VirtualTableRowButton>
  );
}

function SelectGameModalContent(props: SelectGameModalContentProps) {
  const { modalTitle, onGameSelect, onModalClose } = props;

  const listRef = useRef<List<RowItemData>>(null);
  const scrollerRef = useRef<HTMLDivElement>(null);
  const allGames: Game[] = useSelector(createAllGamesSelector());
  const [filter, setFilter] = useState('');
  const [size, setSize] = useState({ width: 0, height: 0 });
  const windowHeight = window.innerHeight;

  useEffect(() => {
    const current = scrollerRef?.current as HTMLElement;

    if (current) {
      const width = current.clientWidth;
      const height = current.clientHeight;
      const padding = bodyPadding - 5;

      setSize({
        width: width - padding * 2,
        height: height + padding,
      });
    }
  }, [windowHeight, scrollerRef]);

  useEffect(() => {
    const currentScrollerRef = scrollerRef.current as HTMLElement;
    const currentScrollListener = currentScrollerRef;

    const handleScroll = throttle(() => {
      const { offsetTop = 0 } = currentScrollerRef;
      const scrollTop = currentScrollerRef.scrollTop - offsetTop;

      listRef.current?.scrollTo(scrollTop);
    }, 10);

    currentScrollListener.addEventListener('scroll', handleScroll);

    return () => {
      handleScroll.cancel();

      if (currentScrollListener) {
        currentScrollListener.removeEventListener('scroll', handleScroll);
      }
    };
  }, [listRef, scrollerRef]);

  const onFilterChange = useCallback(
    ({ value }: InputChanged<string>) => {
      setFilter(value);
    },
    [setFilter]
  );

  const onGameSelectWrapper = useCallback(
    (gameId: number) => {
      const game = allGames.find((s) => s.id === gameId) as Game;

      onGameSelect(game);
    },
    [allGames, onGameSelect]
  );

  const sortedGames = useMemo(
    () => [...allGames].sort(sortByProp('sortTitle')),
    [allGames]
  );

  const items = useMemo(
    () =>
      sortedGames.filter(
        (item) =>
          item.title.toLowerCase().includes(filter.toLowerCase()) ||
          item.igdbId.toString().includes(filter) ||
          item.imdbId?.includes(filter)
      ),
    [sortedGames, filter]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('SelectGameModalTitle', { modalTitle })}
      </ModalHeader>

      <ModalBody
        className={styles.modalBody}
        scrollDirection={scrollDirections.NONE}
      >
        <TextInput
          className={styles.filterInput}
          placeholder={translate('FilterGamePlaceholder')}
          name="filter"
          value={filter}
          autoFocus={true}
          onChange={onFilterChange}
        />

        <Scroller
          ref={scrollerRef}
          className={styles.scroller}
          autoFocus={false}
        >
          <SelectGameModalTableHeader columns={columns} />
          <List<RowItemData>
            ref={listRef}
            style={{
              width: '100%',
              height: '100%',
              overflow: 'none',
            }}
            width={size.width}
            height={size.height}
            itemCount={items.length}
            itemSize={38}
            itemData={{
              items,
              columns,
              onGameSelect: onGameSelectWrapper,
            }}
          >
            {Row}
          </List>
        </Scroller>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default SelectGameModalContent;
