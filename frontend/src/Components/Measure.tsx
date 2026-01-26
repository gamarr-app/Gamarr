import { ReactNode, useEffect, useRef } from 'react';
import useMeasure from 'Helpers/Hooks/useMeasure';

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

function Measure({ onMeasure, children }: MeasureProps) {
  const [measureRef, bounds] = useMeasure();
  const lastBoundsRef = useRef({ width: 0, height: 0 });

  useEffect(() => {
    // Only call onMeasure if dimensions actually changed
    if (
      bounds.width !== lastBoundsRef.current.width ||
      bounds.height !== lastBoundsRef.current.height
    ) {
      lastBoundsRef.current = { width: bounds.width, height: bounds.height };
      onMeasure({
        width: bounds.width,
        height: bounds.height,
      });
    }
  }, [bounds.width, bounds.height, onMeasure]);

  return <div ref={measureRef}>{children}</div>;
}

export default Measure;
