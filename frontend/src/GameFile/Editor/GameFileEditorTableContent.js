import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { sortDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import GameFileEditorRow from './GameFileEditorRow';
import styles from './GameFileEditorTableContent.css';

class GameFileEditorTableContent extends Component {

  //
  // Render

  render() {
    const {
      items,
      columns,
      sortKey,
      sortDirection,
      onSortPress,
      onTableOptionChange
    } = this.props;

    return (
      <div>
        {
          !items.length &&
            <div className={styles.blankpad}>
              {translate('NoGameFilesToManage')}
            </div>
        }

        {
          !!items.length &&
            <Table
              columns={columns}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
              onTableOptionChange={onTableOptionChange}
            >
              <TableBody>
                {
                  items.map((item) => {
                    return (
                      <GameFileEditorRow
                        key={item.id}
                        columns={columns}
                        {...item}
                        onDeletePress={this.props.onDeletePress}
                      />
                    );
                  })
                }
              </TableBody>
            </Table>
        }

      </div>
    );
  }
}

GameFileEditorTableContent.propTypes = {
  gameId: PropTypes.number,
  isDeleting: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string.isRequired,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  onTableOptionChange: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  onDeletePress: PropTypes.func.isRequired
};

export default GameFileEditorTableContent;
