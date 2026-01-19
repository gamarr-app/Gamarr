import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';

class DiscoverGameActionsCell extends Component {

  //
  // Render

  render() {
    const {
      id,
      ...otherProps
    } = this.props;

    return (
      <VirtualTableRowCell
        {...otherProps}
      >
        {/* <SpinnerIconButton
          name={icons.REFRESH}
          title="Refresh Game"
          isSpinning={isRefreshingGame}
          onPress={onRefreshGamePress}
        />

        <IconButton
          name={icons.EDIT}
          title="Edit Game"
          onPress={this.onEditGamePress}
        /> */}

        {/* <EditGameModalConnector
          isOpen={isEditGameModalOpen}
          gameId={id}
          onModalClose={this.onEditGameModalClose}
          onDeleteGamePress={this.onDeleteGamePress}
        />

        <DeleteGameModal
          isOpen={isDeleteGameModalOpen}
          gameId={id}
          onModalClose={this.onDeleteGameModalClose}
        /> */}
      </VirtualTableRowCell>
    );
  }
}

DiscoverGameActionsCell.propTypes = {
  id: PropTypes.number.isRequired
};

export default DiscoverGameActionsCell;
