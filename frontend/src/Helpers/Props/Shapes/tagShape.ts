import PropTypes, { ValidationMap } from 'prop-types';

export interface TagShapeType {
  id: boolean | number | string;
  name: number | string;
}

const tagShape: ValidationMap<TagShapeType> = {
  id: PropTypes.oneOfType([PropTypes.bool, PropTypes.number, PropTypes.string])
    .isRequired,
  name: PropTypes.oneOfType([PropTypes.number, PropTypes.string]).isRequired,
};

export default tagShape;
