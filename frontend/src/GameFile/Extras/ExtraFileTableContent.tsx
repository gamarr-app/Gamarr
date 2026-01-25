import React from 'react';
import IconButton from 'Components/Link/IconButton';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { ExtraFile } from 'GameFile/ExtraFile';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import ExtraFileRow from './ExtraFileRow';
import styles from './ExtraFileTableContent.css';

interface ExtraFileWithDetails extends ExtraFile {
  title?: string;
  languageTags?: string[];
}

interface ExtraFileTableContentProps {
  gameId?: number;
  items: ExtraFileWithDetails[];
}

const columns: Column[] = [
  {
    name: 'relativePath',
    label: () => translate('RelativePath'),
    isVisible: true,
  },
  {
    name: 'extension',
    label: () => translate('Extension'),
    isVisible: true,
  },
  {
    name: 'type',
    label: () => translate('Type'),
    isVisible: true,
  },
  {
    name: 'action',
    label: (<IconButton name={icons.ADVANCED_SETTINGS} />) as unknown as string,
    isVisible: true,
  },
];

function ExtraFileTableContent(props: ExtraFileTableContentProps) {
  const { items } = props;

  return (
    <div>
      {!items.length && (
        <div className={styles.blankpad}>
          {translate('NoExtraFilesToManage')}
        </div>
      )}

      {!!items.length && (
        <Table columns={columns}>
          <TableBody>
            {items.map((item) => {
              return <ExtraFileRow key={item.id} {...item} />;
            })}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

export default ExtraFileTableContent;
