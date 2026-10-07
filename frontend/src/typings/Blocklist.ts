import ModelBase from 'App/ModelBase';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';

interface Blocklist extends ModelBase {
  languages: Language[];
  quality: QualityModel;
  customFormats: CustomFormat[];
  title: string;
  date?: string;
  protocol: DownloadProtocol;
  sourceTitle: string;
  gameId?: number;
  indexer?: string;
  message?: string;
  size?: number;
  publishedDate?: string;
  indexerFlags?: string;
  torrentInfoHash?: string;
}

export default Blocklist;
