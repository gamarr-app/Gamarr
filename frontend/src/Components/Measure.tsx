import _ from 'lodash';
import { Component } from 'react';
import ReactMeasure from 'react-measure';

// Simple dimensions object matching what consumers expect
interface Dimensions {
  width?: number;
  height?: number;
}

interface MeasureProps {
  whitelist?: string[];
  blacklist?: string[];
  includeMargin?: boolean;
  onMeasure: (dimensions: Dimensions) => void;
  children?: React.ReactNode;
}

class Measure extends Component<MeasureProps> {
  //
  // Lifecycle

  componentWillUnmount() {
    this.onMeasure.cancel();
  }

  //
  // Listeners

  onMeasure = _.debounce(
    (contentRect: { bounds?: { width: number; height: number } }) => {
      const { bounds } = contentRect;
      if (bounds) {
        this.props.onMeasure({
          width: bounds.width,
          height: bounds.height,
        });
      }
    },
    250,
    { leading: true, trailing: false }
  );

  //
  // Render

  render() {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { onMeasure, whitelist, blacklist, includeMargin, ...restProps } =
      this.props;

    return (
      <ReactMeasure bounds={true} onResize={this.onMeasure} {...restProps} />
    );
  }
}

export default Measure;
