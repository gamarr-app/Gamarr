import classNames from 'classnames';
import React, { Component, CSSProperties } from 'react';

const FPS = 20;
const STEP = 1;
const TIMEOUT = (1 / FPS) * 1000;

interface MarqueeProps {
  text: string;
  title: string;
  hoverToStop: boolean;
  loop: boolean;
  className: string;
}

interface MarqueeState {
  animatedWidth: number;
  overflowWidth: number;
  direction: number;
}

class Marquee extends Component<MarqueeProps, MarqueeState> {
  static defaultProps = {
    text: '',
    title: '',
    hoverToStop: true,
    loop: false,
    className: '',
  };

  private container: HTMLDivElement | null = null;
  private text: HTMLSpanElement | null = null;
  private marqueeTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(props: MarqueeProps) {
    super(props);

    this.state = {
      animatedWidth: 0,
      overflowWidth: 0,
      direction: 0,
    };
  }

  componentDidMount() {
    this.measureText();

    if (this.props.hoverToStop) {
      this.startAnimation();
    }
  }

  // eslint-disable-next-line react/no-deprecated
  componentWillReceiveProps(nextProps: MarqueeProps) {
    if (this.props.text.length !== nextProps.text.length) {
      if (this.marqueeTimer) {
        clearTimeout(this.marqueeTimer);
      }
      this.setState({ animatedWidth: 0, direction: 0 });
    }
  }

  componentDidUpdate() {
    this.measureText();

    if (this.props.hoverToStop) {
      this.startAnimation();
    }
  }

  componentWillUnmount() {
    if (this.marqueeTimer) {
      clearTimeout(this.marqueeTimer);
    }
  }

  onHandleMouseEnter = () => {
    if (this.props.hoverToStop) {
      if (this.marqueeTimer) {
        clearTimeout(this.marqueeTimer);
      }
    } else if (this.state.overflowWidth > 0) {
      this.startAnimation();
    }
  };

  onHandleMouseLeave = () => {
    if (this.props.hoverToStop && this.state.overflowWidth > 0) {
      this.startAnimation();
    } else {
      if (this.marqueeTimer) {
        clearTimeout(this.marqueeTimer);
      }
      this.setState({ animatedWidth: 0 });
    }
  };

  startAnimation = () => {
    if (this.marqueeTimer) {
      clearTimeout(this.marqueeTimer);
    }
    const isLeading = this.state.animatedWidth === 0;
    const timeout = isLeading ? 0 : TIMEOUT;

    const animate = () => {
      const { overflowWidth } = this.state;
      let animatedWidth = this.state.animatedWidth;
      let direction = this.state.direction;

      if (direction === 0) {
        animatedWidth = this.state.animatedWidth + STEP;
      } else {
        animatedWidth = this.state.animatedWidth - STEP;
      }

      const isRoundOver = animatedWidth < 0;
      const endOfText = animatedWidth > overflowWidth;

      if (endOfText) {
        direction = direction === 1 ? 0 : 1;
      }

      if (isRoundOver) {
        if (this.props.loop) {
          direction = direction === 0 ? 1 : 0;
        } else {
          return;
        }
      }

      this.setState({ animatedWidth, direction });
      this.marqueeTimer = setTimeout(animate, TIMEOUT);
    };

    this.marqueeTimer = setTimeout(animate, timeout);
  };

  measureText = () => {
    const container = this.container;
    const node = this.text;

    if (container && node) {
      const containerWidth = container.offsetWidth;
      const textWidth = node.offsetWidth;
      const overflowWidth = textWidth - containerWidth;

      if (overflowWidth !== this.state.overflowWidth) {
        this.setState({ overflowWidth });
      }
    }
  };

  render() {
    const style: CSSProperties = {
      position: 'relative',
      right: this.state.animatedWidth,
      whiteSpace: 'nowrap',
    };

    if (this.state.overflowWidth < 0) {
      return (
        <div
          ref={(el) => {
            this.container = el;
          }}
          className={classNames('ui-marquee', this.props.className)}
          style={{ overflow: 'hidden' }}
        >
          <span
            ref={(el) => {
              this.text = el;
            }}
            style={style}
            title={
              this.props.title && this.props.text !== this.props.title
                ? `Original Title: ${this.props.title}`
                : this.props.text
            }
          >
            {this.props.text}
          </span>
        </div>
      );
    }

    return (
      <div
        ref={(el) => {
          this.container = el;
        }}
        className={classNames('ui-marquee', this.props.className)}
        style={{ overflow: 'hidden' }}
        onMouseEnter={this.onHandleMouseEnter}
        onMouseLeave={this.onHandleMouseLeave}
      >
        <span
          ref={(el) => {
            this.text = el;
          }}
          style={style}
          title={
            this.props.title && this.props.text !== this.props.title
              ? `Original Title: ${this.props.title}`
              : this.props.text
          }
        >
          {this.props.text}
        </span>
      </div>
    );
  }
}

export default Marquee;
