import classNames from 'classnames';
import {
  FocusEvent,
  FormEvent,
  KeyboardEvent,
  KeyboardEventHandler,
  MutableRefObject,
  ReactNode,
  Ref,
  SyntheticEvent,
  useCallback,
  useEffect,
  useState,
} from 'react';
import Autosuggest, {
  AutosuggestPropsBase,
  BlurEvent,
  ChangeEvent,
  RenderInputComponentProps,
  RenderSuggestionsContainerParams,
} from 'react-autosuggest';
import { usePopper } from 'react-popper';
import Portal from 'Components/Portal';
import usePrevious from 'Helpers/Hooks/usePrevious';
import { InputChanged } from 'typings/inputs';
import styles from './AutoSuggestInput.css';

interface AutoSuggestInputProps<T> extends Omit<
  AutosuggestPropsBase<T>,
  'renderInputComponent' | 'inputProps'
> {
  ref?: MutableRefObject<Autosuggest<T> | null>;
  className?: string;
  inputContainerClassName?: string;
  name: string;
  value?: string;
  placeholder?: string;
  suggestions: T[];
  hasError?: boolean;
  hasWarning?: boolean;
  enforceMaxHeight?: boolean;
  minHeight?: number;
  maxHeight?: number;
  renderInputComponent?: (
    inputProps: RenderInputComponentProps,
    ref: Ref<HTMLDivElement>
  ) => ReactNode;
  onInputChange: (
    event: FormEvent<HTMLElement>,
    params: ChangeEvent
  ) => unknown;
  onInputKeyDown?: KeyboardEventHandler<HTMLElement>;
  onInputFocus?: (event: SyntheticEvent) => unknown;
  onInputBlur: (
    event: FocusEvent<HTMLElement>,
    params?: BlurEvent<T>
  ) => unknown;
  onChange?: (change: InputChanged<T>) => unknown;
}

function AutoSuggestInput<T = unknown>(props: AutoSuggestInputProps<T>) {
  const {
    ref,
    className = styles.input,
    inputContainerClassName = styles.inputContainer,
    name,
    value = '',
    placeholder,
    suggestions,
    enforceMaxHeight = true,
    hasError,
    hasWarning,
    minHeight = 50,
    maxHeight = 200,
    getSuggestionValue,
    renderSuggestion,
    renderInputComponent,
    onInputChange,
    onInputKeyDown,
    onInputFocus,
    onInputBlur,
    onSuggestionsFetchRequested,
    onSuggestionsClearRequested,
    onSuggestionSelected,
    onChange,
    ...otherProps
  } = props;

  const [referenceElement, setReferenceElement] = useState<HTMLElement | null>(
    null
  );
  const [popperElement, setPopperElement] = useState<HTMLElement | null>(null);
  const previousSuggestions = usePrevious(suggestions);

  const {
    styles: popperStyles,
    attributes,
    update,
  } = usePopper(referenceElement, popperElement, {
    placement: 'bottom-start',
    modifiers: [
      {
        name: 'flip',
        options: {
          padding: minHeight,
        },
      },
      {
        name: 'computeMaxHeight',
        enabled: true,
        phase: 'beforeWrite',
        requires: ['computeStyles'],
        fn: ({ state }) => {
          const reference = state.rects.reference;

          if (enforceMaxHeight) {
            state.styles.popper.maxHeight = `${maxHeight}px`;
          } else {
            const windowHeight = window.innerHeight;
            const bottom = reference.y + reference.height;
            const top = reference.y;

            if (/^bottom/.test(state.placement)) {
              state.styles.popper.maxHeight = `${windowHeight - bottom}px`;
            } else {
              state.styles.popper.maxHeight = `${top}px`;
            }
          }

          state.styles.popper.width = `${reference.width}px`;
        },
      },
    ],
  });

  const createRenderInputComponent = useCallback(
    (inputProps: RenderInputComponentProps) => {
      if (renderInputComponent) {
        return renderInputComponent(
          inputProps,
          setReferenceElement as unknown as Ref<HTMLDivElement>
        );
      }

      return (
        <div ref={setReferenceElement as unknown as Ref<HTMLDivElement>}>
          <input {...inputProps} />
        </div>
      );
    },
    [renderInputComponent]
  );

  const renderSuggestionsContainer = useCallback(
    ({ containerProps, children }: RenderSuggestionsContainerParams) => {
      return (
        <Portal>
          <div
            ref={setPopperElement}
            style={popperStyles.popper}
            {...attributes.popper}
            className={children ? styles.suggestionsContainerOpen : undefined}
          >
            <div
              {...containerProps}
              style={{
                maxHeight: popperStyles.popper?.maxHeight,
              }}
            >
              {children}
            </div>
          </div>
        </Portal>
      );
    },
    [popperStyles, attributes]
  );

  const handleInputKeyDown = useCallback(
    (event: KeyboardEvent<HTMLElement>) => {
      if (
        event.key === 'Tab' &&
        suggestions.length &&
        suggestions[0] !== value
      ) {
        event.preventDefault();

        if (value) {
          onSuggestionSelected?.(event, {
            suggestion: suggestions[0],
            suggestionValue: value,
            suggestionIndex: 0,
            sectionIndex: null,
            method: 'enter',
          });
        }
      }
    },
    [value, suggestions, onSuggestionSelected]
  );

  const inputProps = {
    className: classNames(
      className,
      hasError && styles.hasError,
      hasWarning && styles.hasWarning
    ),
    name,
    value,
    placeholder,
    autoComplete: 'off',
    spellCheck: false,
    onChange: onInputChange,
    onKeyDown: onInputKeyDown || handleInputKeyDown,
    onFocus: onInputFocus,
    onBlur: onInputBlur,
  };

  const theme = {
    container: inputContainerClassName,
    containerOpen: styles.suggestionsContainerOpen,
    suggestionsContainer: styles.suggestionsContainer,
    suggestionsList: styles.suggestionsList,
    suggestion: styles.suggestion,
    suggestionHighlighted: styles.suggestionHighlighted,
  };

  useEffect(() => {
    if (update && suggestions !== previousSuggestions) {
      update();
    }
  }, [suggestions, previousSuggestions, update]);

  return (
    <Autosuggest
      {...otherProps}
      ref={ref}
      id={name}
      inputProps={inputProps}
      theme={theme}
      suggestions={suggestions}
      getSuggestionValue={getSuggestionValue}
      renderInputComponent={createRenderInputComponent}
      renderSuggestionsContainer={renderSuggestionsContainer}
      renderSuggestion={renderSuggestion}
      onSuggestionSelected={onSuggestionSelected}
      onSuggestionsFetchRequested={onSuggestionsFetchRequested}
      onSuggestionsClearRequested={onSuggestionsClearRequested}
    />
  );
}

export default AutoSuggestInput;
