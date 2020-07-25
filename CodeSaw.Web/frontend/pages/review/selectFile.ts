import { FileId } from '@api/reviewer';

export type SelectFileForViewHandler = (fileId: FileId) => void;
export type OnShowFileHandlerAvailable = (handler: () => void) => void;
