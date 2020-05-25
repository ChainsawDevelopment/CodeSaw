import { ReviewInfo, FileId, ReviewDiscussion, FileDiscussion } from "@api/reviewer";
import { UnpublishedReview, FileReviewStatusChange } from "@src/pages/review/state";
import { RevisionId, LocalRevisionId, RevisionSelected } from "@api/revisionId";

export interface FileIdMap {
    [fileId: string]: string;
}

const remapProvisionalRevision = (info: ReviewInfo, unpublished: UnpublishedReview): UnpublishedReview => {
    const matchingPastRevision = info.pastRevisions.find(r => r.base == unpublished.baseCommit && r.head == unpublished.headCommit);

    if (matchingPastRevision == null) {
        return {
            ...unpublished
        };
    }

    const mapRevision = (r: LocalRevisionId): LocalRevisionId => {
        if (RevisionId.isProvisional(r)) {
            return RevisionId.makeSelected(matchingPastRevision.number);
        } else {
            return r;
        }
    }

    return {
        ...unpublished,
        unpublishedFileDiscussions: unpublished.unpublishedFileDiscussions.map(d => ({
            ...d,
            revision: mapRevision(d.revision)
        })),
        unpublishedReviewDiscussions: unpublished.unpublishedReviewDiscussions.map(d => ({
            ...d,
            revision: mapRevision(d.revision)
        })),
        unpublishedReviewedFiles: unpublished.unpublishedReviewedFiles.map(d => ({
            ...d,
            revision: mapRevision(d.revision)
        })),
        unpublishedUnreviewedFiles: unpublished.unpublishedUnreviewedFiles.map(d => ({
            ...d,
            revision: mapRevision(d.revision)
        })),
    }
}

const sanitizeReviewedFiles = (info: ReviewInfo, unpublished: UnpublishedReview): UnpublishedReview => {
    const upstreamReviewedFiles = info.filesToReview.filter(f => RevisionId.equal(f.previous, f.current)).map(f => f.fileId);
    return {
        ...unpublished,
        unpublishedReviewedFiles: unpublished.unpublishedReviewedFiles.filter(f => upstreamReviewedFiles.indexOf(f.fileId) == -1)
    };
}

const sanitizeUnreviewedFiles = (info: ReviewInfo, unpublished: UnpublishedReview): UnpublishedReview => {
    const upstreamReviewedFiles = info.filesToReview.filter(f => RevisionId.equal(f.previous, f.current)).map(f => f.fileId);
    return {
        ...unpublished,
        unpublishedUnreviewedFiles: unpublished.unpublishedUnreviewedFiles.filter(f => upstreamReviewedFiles.indexOf(f.fileId) != -1)
    };
}

const removeMissingReviewedFiles = (info: ReviewInfo, unpublished: UnpublishedReview): UnpublishedReview => {
    const validFiles = info.filesToReview.map(f => f.fileId);
    return {
        ...unpublished,
        unpublishedReviewedFiles: unpublished.unpublishedReviewedFiles.filter(f => validFiles.indexOf(f.fileId) != -1)
    };
}

const remapFileIds = (info: ReviewInfo, unpublished: UnpublishedReview, fileIds: FileIdMap): UnpublishedReview => {
    const matchingPastRevision = info.pastRevisions.find(r => r.base == unpublished.baseCommit && r.head == unpublished.headCommit);

    const remap = (id: FileId): FileId => {
        if (matchingPastRevision == null) {
            return id;
        }
        const match = info.fileMatrix.find(e => {
            const r = e.revisions[matchingPastRevision.number - 1];
            return r.file.newPath == fileIds[id];
        });

        if (match != null) {
            return match.fileId;
        } else {
            return id;
        }
    };

    return {
        ...unpublished,
        unpublishedReviewedFiles: unpublished.unpublishedReviewedFiles.map(f => ({
            ...f,
            fileId: remap(f.fileId)
        })),
        unpublishedFileDiscussions: unpublished.unpublishedFileDiscussions.map(f => ({
            ...f,
            fileId: remap(f.fileId)
        }))
    };
}

const handleHeadDiverged = (info: ReviewInfo, unpublished: UnpublishedReview): UnpublishedReview => {
    const provisionalHeadMatches = RevisionId.isProvisional(info.headRevision) && info.baseCommit == unpublished.baseCommit && info.headCommit == unpublished.headCommit;

    return {
        ...unpublished,
        unpublishedReviewedFiles: unpublished.unpublishedReviewedFiles.filter(f => !RevisionId.isProvisional(f.revision) || provisionalHeadMatches)
    };
}

const convertLostFileDiscussions = (info: ReviewInfo, unpublished: UnpublishedReview): UnpublishedReview => {
    const additionalReviewDiscussions: ReviewDiscussion[] = [];
    const fileDiscussions: FileDiscussion[] = [];

    const validFiles = info.filesToReview.map(f => f.fileId);

    for (const discussion of unpublished.unpublishedFileDiscussions) {
        if (validFiles.indexOf(discussion.fileId) == -1) {
            additionalReviewDiscussions.push({
                canResolve: discussion.canResolve,
                comment: discussion.comment,
                id: discussion.id, // HACK,
                revision: discussion.revision,
                state: discussion.state
            });
            continue;
        }
        fileDiscussions.push(discussion);
    }

    return {
        ...unpublished,
        unpublishedReviewDiscussions: [...unpublished.unpublishedReviewDiscussions, ...additionalReviewDiscussions],
        unpublishedFileDiscussions: fileDiscussions
    };
}

const unreviewFileThatChangesInHead = (info: ReviewInfo, unpublished: UnpublishedReview): UnpublishedReview => {
    const result: FileReviewStatusChange[] = [];

    for (const file of unpublished.unpublishedReviewedFiles) {
        if(RevisionId.isProvisional(file.revision)) {
            result.push(file);
            continue;
        }

        const revisionsWithChanges = info.fileMatrix.find(f=>f.fileId == file.fileId).revisions.filter(f => !f.isUnchanged).map(f=>f.revision);
        if(revisionsWithChanges.length == 0) {
            throw new Error("not implemented");
            continue;
        }
        const lastChangeIn = revisionsWithChanges[revisionsWithChanges.length - 1];
        const reviewedChangeIn = file.revision;

        if(RevisionId.equal(lastChangeIn, reviewedChangeIn)) {
            result.push(file);
        }
    }

    return {
        ...unpublished,
        unpublishedReviewedFiles: result
    };
}

export const upgradeReview = (info: ReviewInfo, unpublished: UnpublishedReview, fileIds: FileIdMap): UnpublishedReview => {
    if (info.fileMatrix == null) {
        throw new Error("no fileMatrix");
    }

    let result = unpublished;
    result = remapProvisionalRevision(info, result);
    result = remapFileIds(info, result, fileIds);
    result = sanitizeReviewedFiles(info, result);
    result = sanitizeUnreviewedFiles(info, result);
    result = removeMissingReviewedFiles(info, result);
    result = convertLostFileDiscussions(info, result);
    result = handleHeadDiverged(info, result);
    result = unreviewFileThatChangesInHead(info, result);

    return {
        ...result,
        headCommit: info.headCommit,
        baseCommit: info.baseCommit,
    };
}