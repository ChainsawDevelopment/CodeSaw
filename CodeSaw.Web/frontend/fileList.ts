import { FileToReview, FileId, FileDiscussion } from '@api/reviewer';

class FileList {
    constructor(
        private files: FileToReview[],
        private currentFile: FileId,
        private reviewedFiles: FileId[],
        private discussions: FileDiscussion[],
    ) {}

    private _findNextFile = (direction: 1 | -1, predicate: (f: FileToReview) => boolean): FileToReview => {
        const currentIndex = this.files.findIndex((p) => p.fileId == this.currentFile);

        if (currentIndex == -1) {
            return null;
        }

        let index = currentIndex;

        while (true) {
            index += direction;

            if (index == -1) {
                index = this.files.length - 1;
            } else if (index == this.files.length) {
                index = 0;
            }

            if (index == currentIndex) {
                return this.files[currentIndex];
            }

            const candidate = this.files[index];

            if (predicate(candidate)) {
                return candidate;
            }
        }
    };

    public nextUnreviewedFile = (direction: 1 | -1): FileToReview => {
        const predicate = (file: FileToReview) => {
            const filesReviewedByUser: FileId[] = this.reviewedFiles;

            const fileDiscussions = this.discussions
                .filter((f) => f.fileId == file.fileId)
                .filter((f) => f.state === 'NeedsResolution');

            if (fileDiscussions.length > 0) {
                return true;
            }

            if (filesReviewedByUser.indexOf(file.fileId) == -1) {
                return true;
            }

            return false;
        };

        return this._findNextFile(direction, predicate);
    };

    public nextFileWithUnresolvedComment = (direction: 1 | -1): FileToReview => {
        const predicate = (file: FileToReview) => {
            const fileDiscussions = this.discussions
                .filter((f) => f.fileId == file.fileId)
                .filter((f) => f.state === 'NeedsResolution' && f.comment.children.length == 0);

            if (fileDiscussions.length > 0) {
                return true;
            }

            return false;
        };

        return this._findNextFile(direction, predicate);
    };
}

export default FileList;
