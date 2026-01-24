import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextInput from 'Components/Form/TextInput';
import styles from './QualityDefinition.css';

class QualityDefinition extends Component {

  //
  // Render

  render() {
    const {
      id,
      quality,
      title,
      onTitleChange
    } = this.props;

    return (
      <div className={styles.qualityDefinition}>
        <div className={styles.quality}>
          {quality.name}
        </div>

        <div className={styles.title}>
          <TextInput
            name={`${id}.${title}`}
            value={title}
            onChange={onTitleChange}
          />
        </div>
      </div>
    );
  }
}

QualityDefinition.propTypes = {
  id: PropTypes.number.isRequired,
  quality: PropTypes.object.isRequired,
  title: PropTypes.string.isRequired,
  onTitleChange: PropTypes.func.isRequired
};

export default QualityDefinition;
