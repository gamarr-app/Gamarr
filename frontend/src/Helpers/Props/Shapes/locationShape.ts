import PropTypes from 'prop-types';

export interface LocationShapeType {
  pathname: string;
  search: string;
  state?: object;
  action?: string;
  key?: string;
}

const locationShape = PropTypes.shape({
  pathname: PropTypes.string.isRequired,
  search: PropTypes.string.isRequired,
  state: PropTypes.object,
  action: PropTypes.string,
  key: PropTypes.string,
});

export default locationShape;
