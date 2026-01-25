import { useCallback, useEffect, useRef, useState } from 'react';
import LazyLoad from 'react-lazyload';
import { CoverType, Image } from './Game';

function findImages(images: Image[], coverType: CoverType) {
  return images.filter((image) => image.coverType === coverType);
}

function getUrl(image: Image, coverType: CoverType, size: number) {
  const imageUrl = image?.url ?? image?.remoteUrl;

  if (!imageUrl) {
    return null;
  }

  // For MediaCoverProxy URLs or external URLs that don't follow our naming convention,
  // return the URL as-is (the server will handle resizing or it's already sized)
  if (
    imageUrl.includes('/MediaCoverProxy/') ||
    !imageUrl.includes(`${coverType}.jpg`)
  ) {
    return imageUrl;
  }

  return imageUrl.replace(`${coverType}.jpg`, `${coverType}-${size}.jpg`);
}

export interface GameImageProps {
  className?: string;
  style?: object;
  images: Image[];
  coverType: CoverType;
  placeholder: string;
  size?: number;
  lazy?: boolean;
  overflow?: boolean;
  onError?: () => void;
  onLoad?: () => void;
}

const pixelRatio = Math.max(Math.round(window.devicePixelRatio), 1);

function GameImage({
  className,
  style,
  images,
  coverType,
  placeholder,
  size = 250,
  lazy = true,
  overflow = false,
  onError,
  onLoad,
}: GameImageProps) {
  const [url, setUrl] = useState<string | null>(null);
  const [hasError, setHasError] = useState(false);
  const [isLoaded, setIsLoaded] = useState(true);
  const [imageIndex, setImageIndex] = useState(0);
  const availableImages = useRef<Image[]>([]);
  const hasCheckedInitialImages = useRef(false);

  const handleLoad = useCallback(() => {
    setHasError(false);
    setIsLoaded(true);
    onLoad?.();
  }, [setHasError, setIsLoaded, onLoad]);

  const handleError = useCallback(() => {
    // Try next image of the same type
    const nextIndex = imageIndex + 1;
    if (nextIndex < availableImages.current.length) {
      setImageIndex(nextIndex);
      setUrl(
        getUrl(availableImages.current[nextIndex], coverType, pixelRatio * size)
      );
    } else {
      // All images failed, show placeholder
      setHasError(true);
      setIsLoaded(false);
      onError?.();
    }
  }, [imageIndex, coverType, size, onError]);

  useEffect(() => {
    const matchingImages = findImages(images, coverType);

    if (matchingImages.length > 0) {
      const currentUrl =
        availableImages.current[0]?.url ??
        availableImages.current[0]?.remoteUrl;
      const newUrl = matchingImages[0]?.url ?? matchingImages[0]?.remoteUrl;

      if (currentUrl !== newUrl) {
        availableImages.current = matchingImages;
        setImageIndex(0);
        setUrl(getUrl(matchingImages[0], coverType, pixelRatio * size));
        setHasError(false);
      }
    } else if (availableImages.current.length > 0) {
      availableImages.current = [];
      setImageIndex(0);
      setUrl(placeholder);
      setHasError(false);
      onError?.();
    }
  }, [images, coverType, placeholder, size, onError]);

  useEffect(() => {
    if (!hasCheckedInitialImages.current) {
      hasCheckedInitialImages.current = true;
      if (availableImages.current.length === 0) {
        onError?.();
      }
    }
  }, [onError]);

  if (hasError || !url) {
    return <img className={className} style={style} src={placeholder} />;
  }

  if (lazy) {
    return (
      <LazyLoad
        height={size}
        offset={100}
        overflow={overflow}
        placeholder={
          <img className={className} style={style} src={placeholder} />
        }
      >
        <img
          className={className}
          style={style}
          src={url}
          rel="noreferrer"
          onError={handleError}
          onLoad={handleLoad}
        />
      </LazyLoad>
    );
  }

  return (
    <img
      className={className}
      style={style}
      src={isLoaded ? url : placeholder}
      onError={handleError}
      onLoad={handleLoad}
    />
  );
}

export default GameImage;
