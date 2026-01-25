import VirtualTableRowCell, {
  VirtualTableRowCellProps,
} from 'Components/Table/Cells/VirtualTableRowCell';

interface DiscoverGameActionsCellProps
  extends Omit<VirtualTableRowCellProps, 'id'> {
  id: number;
}

function DiscoverGameActionsCell({
  id,
  ...otherProps
}: DiscoverGameActionsCellProps) {
  return (
    <VirtualTableRowCell {...otherProps}>
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

export default DiscoverGameActionsCell;
