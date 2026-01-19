import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import getGameStatusDetails from 'Game/getGameStatusDetails';
import styles from './ListGameStatusCell.css';

function ListGameStatusCell(props) {
  const {
    className,
    status,
    component: Component,
    ...otherProps
  } = props;

  const statusDetails = getGameStatusDetails(status);

  return (
    <Component
      className={className}
      {...otherProps}
    >
      <Icon
        className={styles.statusIcon}
        name={statusDetails.icon}
        title={`${statusDetails.title}: ${statusDetails.message}`}
      />

    </Component>
  );
}

ListGameStatusCell.propTypes = {
  className: PropTypes.string.isRequired,
  status: PropTypes.string.isRequired,
  component: PropTypes.elementType
};

ListGameStatusCell.defaultProps = {
  className: styles.status,
  component: VirtualTableRowCell
};

export default ListGameStatusCell;
