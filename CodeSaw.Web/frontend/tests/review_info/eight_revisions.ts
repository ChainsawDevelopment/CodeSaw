import { ReviewInfo, ReviewInfoState, ReviewMergeStatus, } from "@api/reviewer";
import { RevisionId } from "@api/revisionId";
import { ReviewBuilder } from "./builders";

export const FileId = {
    'file4.txt': '1cb24b9a-cebc-4d56-b872-ab9300e16ce9'
}

const bolierplate = {
    reviewId: {
        projectId: 28,
        reviewId: 88
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
}


export const info: ReviewInfo = {
    ...bolierplate,
    headRevision: RevisionId.makeSelected(2),
    headCommit: 'REV_2_HEAD',
    baseCommit: 'REV_2_BASE',
    pastRevisions: [
        {
            number: 1,
            head: 'REV_1_HEAD',
            base: 'REV_1_BASE',
        },
        {
            number: 2,
            head: 'REV_2_HEAD',
            base: 'REV_2_BASE',
        }
    ],
    filesToReview: [
        {
            fileId: FileId["file4.txt"],
            changeType: 'modified',
            diffFile: {
                oldPath: 'file4/old.txt',
                newPath: 'file4/new.txt'
            },
            reviewFile: {
                oldPath: 'file4/old.txt',
                newPath: 'file4/new.txt'
            },
            previous: RevisionId.Base,
            current: RevisionId.makeSelected(2)
        }
    ],

    hasProvisionalRevision: false,

};

