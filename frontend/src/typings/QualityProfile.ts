import Quality from 'Quality/Quality';
import { QualityProfileFormatItem } from './CustomFormat';

export interface QualityProfileQualityItem {
  id?: number;
  quality?: Quality;
  items: QualityProfileQualityItem[];
  allowed: boolean;
  name?: string;
}

interface QualityProfile {
  name: string;
  upgradeAllowed: boolean;
  cutoff: number;
  items: QualityProfileQualityItem[];
  minFormatScore: number;
  cutoffFormatScore: number;
  minUpgradeFormatScore: number;
  formatItems: QualityProfileFormatItem[];
  preferredPlatforms: number[];
  id: number;
}

export default QualityProfile;
