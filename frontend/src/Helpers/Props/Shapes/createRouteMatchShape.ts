import PropTypes, { Validator } from 'prop-types';

function createRouteMatchShape(
  props: Record<string, Validator<unknown>>
): Validator<{ params: object }> {
  return PropTypes.shape({
    params: PropTypes.shape({
      ...props,
    }).isRequired,
  }) as Validator<{ params: object }>;
}

export default createRouteMatchShape;
