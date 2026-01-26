import _ from 'lodash';
import {
  Children,
  cloneElement,
  Component,
  createRef,
  isValidElement,
  ReactNode,
  RefObject,
} from 'react';

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
  children?: ReactNode;
}

class Measure extends Component<MeasureProps> {
  // eslint-disable-next-line react/sort-comp
  private elementRef: RefObject<HTMLElement | null> = createRef();
  private resizeObserver: ResizeObserver | null = null;

  //
  // Lifecycle

  componentDidMount() {
    this.setupResizeObserver();
  }

  componentWillUnmount() {
    this.onMeasure.cancel();
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  //
  // Control

  setupResizeObserver = () => {
    const element = this.elementRef.current;
    if (!element) {
      return;
    }

    this.resizeObserver = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const { width, height } = entry.contentRect;
        this.onMeasure({ bounds: { width, height } });
      }
    });

    this.resizeObserver.observe(element);

    // Initial measurement
    const rect = element.getBoundingClientRect();
    this.onMeasure({ bounds: { width: rect.width, height: rect.height } });
  };

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
    const { children } = this.props;

    // Clone the child element and add our ref
    const child = Children.only(children);
    if (isValidElement(child)) {
      return cloneElement(child, {
        ref: this.elementRef,
      } as Record<string, unknown>);
    }

    return child;
  }
}

export default Measure;
