import {
    ReviewInfo,
    ReviewInfoState,
    ReviewMergeStatus,
    FileId,
    FileMatrixEntry,
    FileMatrixRevision,
} from '@api/reviewer';
import { expect } from 'chai';
import { RevisionId, LocalRevisionId, RevisionSelected } from '@api/revisionId';

export type ReviewBuilder = (input: ReviewInfo) => ReviewInfo;

export const applyBuilders = (input: ReviewInfo, ...builders: ReviewBuilder[]): ReviewInfo => {
    let result = { ...input };

    for (const builder of builders) {
        result = builder(result);
    }

    return result;
};

export const buildReviewInfo = (...builders: ReviewBuilder[]): ReviewInfo => {
    return applyBuilders(emptyReviewInfo, ...builders);
};

export const emptyReviewInfo: ReviewInfo = {
    reviewId: {
        projectId: 28,
        reviewId: 88,
    },
    title: '',
    projectPath: '',
    description: '',
    webUrl: '',
    state: 'opened' as ReviewInfoState,
    mergeStatus: 'can_be_merged' as ReviewMergeStatus,
    sourceBranch: '',
    targetBranch: '',
    buildStatuses: [],
    author: null,
    isAuthor: false,
    reviewFinished: false,
    fileMatrix: null,
    fileDiscussions: [],
    reviewDiscussions: [],
    headRevision: null,
    headCommit: 'UNSET',
    baseCommit: 'UNSET',
    pastRevisions: [],
    filesToReview: [],
    hasProvisionalRevision: false,
    commits: [],
};

export const addRevision = (revision: number): ReviewBuilder => (input) => {
    expect(input.headRevision == null || RevisionId.isSelected(input.headRevision)).to.be.true;

    return {
        ...input,
        headRevision: RevisionId.makeSelected(revision),
        headCommit: `REV_${revision}_HEAD`,
        baseCommit: `REV_${revision}_BASE`,
        pastRevisions: [
            ...input.pastRevisions,
            {
                number: revision,
                base: `REV_${revision}_BASE`,
                head: `REV_${revision}_HEAD`,
            },
        ],
    };
};

export const addProvisional = (headCommit: string, baseCommit: string): ReviewBuilder => (input) => {
    expect(input.headRevision == null || RevisionId.isSelected(input.headRevision)).to.be.true;

    return {
        ...input,
        hasProvisionalRevision: true,
        headRevision: RevisionId.Provisional,
        headCommit: headCommit,
        baseCommit: baseCommit,
    };
};

export const addFileToReview = (
    fileId: FileId,
    fileName: string,
    previous: LocalRevisionId,
    current: LocalRevisionId,
) => (input: { filesToReview: any }): any => {
    return {
        ...input,
        filesToReview: [
            ...input.filesToReview,
            {
                fileId: fileId,
                changeType: 'modified',
                diffFile: {
                    oldPath: `${fileName}/old.txt`,
                    newPath: `${fileName}/new.txt`,
                },
                reviewFile: {
                    oldPath: `${fileName}/old.txt`,
                    newPath: `${fileName}/new.txt`,
                },
                previous: previous,
                current: current,
            },
        ],
    };
};

export const defineFileMatrix = (): ReviewBuilder => (input) => {
    const matrix: FileMatrixEntry[] = [];

    for (const file of input.filesToReview) {
        const entryRevisions: FileMatrixRevision[] = [];

        for (const revision of input.pastRevisions) {
            entryRevisions.push({
                file: file.reviewFile,
                isDeleted: false,
                isNew: false,
                isRenamed: false,
                isUnchanged: true,
                reviewers: [],
                revision: RevisionId.makeSelected(revision.number),
            });
        }

        if (input.hasProvisionalRevision) {
            entryRevisions.push({
                file: file.reviewFile,
                isDeleted: false,
                isNew: false,
                isRenamed: false,
                isUnchanged: true,
                reviewers: [],
                revision: RevisionId.Provisional,
            });
        }

        matrix.push({
            file: file.reviewFile,
            fileId: file.fileId,
            revisions: entryRevisions,
        });
    }

    return {
        ...input,
        fileMatrix: matrix,
    };
};

export const markFileChanged = (fileId: FileId, revisions: LocalRevisionId[]): ReviewBuilder => (input) => {
    return {
        ...input,
        fileMatrix: input.fileMatrix.map((f) => {
            if (f.fileId != fileId) {
                return f;
            }

            return {
                ...f,
                revisions: f.revisions.map((r) => ({
                    ...r,
                    isUnchanged: revisions.find((x) => RevisionId.equal(r.revision, x)) == null,
                })),
            };
        }),
    };
};
