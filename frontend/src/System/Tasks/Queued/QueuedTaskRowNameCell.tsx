import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import { CommandBody } from 'Commands/Command';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import createMultiGamesSelector from 'Store/Selectors/createMultiGamesSelector';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import styles from './QueuedTaskRowNameCell.css';

function formatTitles(titles: string[]) {
  if (!titles) {
    return null;
  }

  if (titles.length > 11) {
    return (
      <span title={titles.join(', ')}>
        {titles.slice(0, 10).join(', ')}, {titles.length - 10} more
      </span>
    );
  }

  return <span>{titles.join(', ')}</span>;
}

export interface QueuedTaskRowNameCellProps {
  commandName: string;
  body: CommandBody;
  clientUserAgent?: string;
}

export default function QueuedTaskRowNameCell(
  props: QueuedTaskRowNameCellProps
) {
  const { commandName, body, clientUserAgent } = props;
  const gameIds = useMemo(() => {
    const ids = [...(body.gameIds ?? [])];
    if (body.gameId) {
      ids.push(body.gameId);
    }
    return ids;
  }, [body.gameIds, body.gameId]);

  const games = useSelector(
    useMemo(() => createMultiGamesSelector(gameIds), [gameIds])
  );
  const sortedGames = games.sort(sortByProp('sortTitle'));

  return (
    <TableRowCell>
      <span className={styles.commandName}>
        {commandName}
        {sortedGames.length ? (
          <span> - {formatTitles(sortedGames.map((m) => m.title))}</span>
        ) : null}
      </span>

      {clientUserAgent ? (
        <span
          className={styles.userAgent}
          title={translate('TaskUserAgentTooltip')}
        >
          {translate('From')}: {clientUserAgent}
        </span>
      ) : null}
    </TableRowCell>
  );
}
