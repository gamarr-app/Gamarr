import { useCallback } from 'react';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import Menu from 'Components/Menu/Menu';
import MenuContent from 'Components/Menu/MenuContent';
import { sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddSpecificationPresetMenuItem from './AddSpecificationPresetMenuItem';
import styles from './AddSpecificationItem.css';

interface Preset {
  name: string;
  [key: string]: unknown;
}

interface SpecificationSelectPayload {
  implementation: string;
  name?: string;
}

interface AddSpecificationItemProps {
  implementation: string;
  implementationName: string;
  infoLink: string;
  presets?: Preset[];
  onSpecificationSelect: (payload: SpecificationSelectPayload) => void;
}

function AddSpecificationItem({
  implementation,
  implementationName,
  infoLink,
  presets,
  onSpecificationSelect,
}: AddSpecificationItemProps) {
  const handleSpecificationSelect = useCallback(() => {
    onSpecificationSelect({ implementation });
  }, [implementation, onSpecificationSelect]);

  const hasPresets = !!presets && !!presets.length;

  return (
    <div className={styles.specification}>
      <Link className={styles.underlay} onPress={handleSpecificationSelect} />

      <div className={styles.overlay}>
        <div className={styles.name}>{implementationName}</div>

        <div className={styles.actions}>
          {hasPresets && (
            <span>
              <Button size={sizes.SMALL} onPress={handleSpecificationSelect}>
                {translate('Custom')}
              </Button>

              <Menu className={styles.presetsMenu}>
                <Button className={styles.presetsMenuButton} size={sizes.SMALL}>
                  {translate('Presets')}
                </Button>

                <MenuContent>
                  {presets.map((preset, index) => {
                    return (
                      <AddSpecificationPresetMenuItem
                        key={index}
                        name={preset.name}
                        implementation={implementation}
                        onPress={onSpecificationSelect}
                      />
                    );
                  })}
                </MenuContent>
              </Menu>
            </span>
          )}

          <Button to={infoLink} size={sizes.SMALL}>
            {translate('MoreInfo')}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default AddSpecificationItem;
