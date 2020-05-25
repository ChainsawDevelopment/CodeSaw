import { ReviewInfo, ReviewDiscussion, FileId, FileDiscussion } from "@api/reviewer";
import { UnpublishedReview, emptyUnpublishedReview } from "@src/pages/review/state";
import {expect} from 'chai';
import { RevisionId, LocalRevisionId } from "@api/revisionId";
import * as B from './review_info/builders';
import { upgradeReview } from "@src/pages/review/upgradeReview";

const FileIds = {
    file4: '1cb24b9a-cebc-4d56-b872-ab9300e16ce9',
    prov_file4: 'PROV_file4',
    prov_file3: 'PROV_file3',
}

const expectNoChanges = (baseReviewInfo: ReviewInfo): Mocha.Func => {
    return () => {
        const reviewInfo = B.applyBuilders(baseReviewInfo, B.defineFileMatrix());
        const unpublished = {...emptyUnpublishedReview, ...forHead(reviewInfo)};
        const fileIdMap = {};
        const upgraded = upgradeReview(reviewInfo, unpublished, fileIdMap);
        expect(upgraded).to.deep.equal(unpublished);
    }
}

const forHead = (reviewInfo: ReviewInfo) => ({
    headCommit: reviewInfo.headCommit,
    baseCommit: reviewInfo.baseCommit
});

const reviewDiscussion = (revision: LocalRevisionId): ReviewDiscussion => ({
    revision: revision,
    canResolve: true,
    comment: null,
    id: 'REVIEW-1',
    state: "NeedsResolution"
});

const fileDiscussion = (revision: LocalRevisionId, fileId: FileId): FileDiscussion => ({
    revision: revision,
    fileId: fileId,
    lineNumber: 10,
    canResolve: true,
    comment: null,
    id: 'REVIEW-1',
    state: "NeedsResolution"
});

const expectUnreviewedFileMarkedAsReviewed = (baseReviewInfo: ReviewInfo, reviewedAt: number): Mocha.Func => {
    return () => {
        const reviewInfo = B.applyBuilders(baseReviewInfo,
            B.addFileToReview(FileIds.file4, 'file4', RevisionId.Base, RevisionId.makeSelected(2)),
            B.defineFileMatrix(),
            B.markFileChanged(FileIds.file4, [ RevisionId.makeSelected(reviewedAt) ])
        );
        const unpublished: UnpublishedReview = {
            ...emptyUnpublishedReview,
            ...forHead(reviewInfo),
            unpublishedReviewedFiles: [
                {revision: RevisionId.makeSelected(reviewedAt), fileId: FileIds.file4}
            ]
        };
        const fileIdMap = {
            [FileIds.file4]: 'file4/new.txt'
        };

        const upgraded = upgradeReview(reviewInfo, unpublished, fileIdMap);

        expect(upgraded).to.deep.equal({
            ...emptyUnpublishedReview,
            ...forHead(reviewInfo),
            unpublishedReviewedFiles: [
                {revision: RevisionId.makeSelected(reviewedAt), fileId: FileIds.file4}
            ]
        });
    };
}

const expectReviewedFileMarkedAsUnreviewed = (baseReviewInfo: ReviewInfo, reviewedAt: number): Mocha.Func => {
    return () => {
        const reviewInfo = B.applyBuilders(baseReviewInfo,
            B.addFileToReview(FileIds.file4, 'file4', RevisionId.makeSelected(reviewedAt), RevisionId.makeSelected(reviewedAt)),
            B.defineFileMatrix()
        );
        const unpublished: UnpublishedReview = {
            ...emptyUnpublishedReview,
            ...forHead(reviewInfo),
            unpublishedUnreviewedFiles: [
                {revision: RevisionId.makeSelected(reviewedAt), fileId: FileIds.file4}
            ]
        };
        const fileIdMap = {
            [FileIds.file4]: 'file4/new.txt'
        };

        const upgraded = upgradeReview(reviewInfo, unpublished, fileIdMap);

        expect(upgraded).to.deep.equal({
            ...emptyUnpublishedReview,
            ...forHead(reviewInfo),
            unpublishedUnreviewedFiles: [
                {revision: RevisionId.makeSelected(reviewedAt), fileId: FileIds.file4}
            ]
        });
    };
}

const expectReviewedFileMarkedAsReviewed = (baseReviewInfo: ReviewInfo, reviewedAt: number): Mocha.Func => {
    return () => {
        const reviewInfo = B.applyBuilders(baseReviewInfo,
            B.addFileToReview(FileIds.file4, 'file4', RevisionId.makeSelected(reviewedAt), RevisionId.makeSelected(reviewedAt)),
            B.defineFileMatrix()
        );
        const unpublished: UnpublishedReview = {
            ...emptyUnpublishedReview,
            ...forHead(reviewInfo),
            unpublishedReviewedFiles: [
                {revision: RevisionId.makeSelected(reviewedAt), fileId: FileIds.file4}
            ]
        };
        const fileIdMap = {
            [FileIds.file4]: 'file4/new.txt'
        };

        const upgraded = upgradeReview(reviewInfo, unpublished, fileIdMap);

        expect(upgraded).to.deep.equal({...emptyUnpublishedReview, ...forHead(baseReviewInfo)});
    }
}

const expectUnreviewedFileMarkedAsUnreviewed = (baseReviewInfo: ReviewInfo, reviewedAt: number): Mocha.Func => {
    return () => {
        const reviewInfo = B.applyBuilders(baseReviewInfo,
            B.addFileToReview(FileIds.file4, 'file4', RevisionId.Base, RevisionId.makeSelected(reviewedAt)),
            B.defineFileMatrix()
        );
        const unpublished: UnpublishedReview = {
            ...emptyUnpublishedReview,
            ...forHead(reviewInfo),
            unpublishedUnreviewedFiles: [
                {revision: RevisionId.makeSelected(reviewedAt), fileId: FileIds.file4}
            ]
        };
        const fileIdMap = {
            [FileIds.file4]: 'file4/new.txt'
        };

        const upgraded = upgradeReview(reviewInfo, unpublished, fileIdMap);

        expect(upgraded).to.deep.equal({...emptyUnpublishedReview, ...forHead(baseReviewInfo)});
    }
}

const expectReviewDiscussionForSavedRevision = (baseReviewInfo: ReviewInfo, revision: number): Mocha.Func => {
    return () => {
        const reviewInfo = B.applyBuilders(baseReviewInfo, B.defineFileMatrix());
        const unpublished: UnpublishedReview = {...emptyUnpublishedReview, ...forHead(reviewInfo),
            unpublishedReviewDiscussions: [reviewDiscussion(RevisionId.makeSelected(revision))]
        };
        const fileIdMap = {};
        expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
    };
}

const expectFileDiscussionForSavedRevision = (baseReviewInfo: ReviewInfo, revision: number, fileId: FileId, fileName: string): Mocha.Func => {
    return () => {
        const reviewInfo = B.applyBuilders(baseReviewInfo,
            B.addFileToReview(fileId, 'file4', RevisionId.makeSelected(revision), RevisionId.makeSelected(revision)),
            B.defineFileMatrix()
        );
        const unpublished: UnpublishedReview = {...emptyUnpublishedReview, ...forHead(reviewInfo),
            unpublishedFileDiscussions: [fileDiscussion(RevisionId.makeSelected(revision), fileId)]
        };
        const fileIdMap = {
            [fileId]: fileName
        };
        expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
    };
}

describe('Upgrade review', () => {
    describe('Two revisions, no provisional', () => {
        const base = B.buildReviewInfo(
            B.addRevision(1),
            B.addRevision(2),
        );

        it('empty unpublished', expectNoChanges(base));
        it('unreviewed file marked as reviewed at R2', expectUnreviewedFileMarkedAsReviewed(base, 2));
        it('unreviewed file marked as reviewed at R1', expectUnreviewedFileMarkedAsReviewed(base, 1));
        it('reviewed file marked as unreviewed at R2', expectReviewedFileMarkedAsUnreviewed(base, 2));
        it('reviewed file marked as unreviewed at R1', expectReviewedFileMarkedAsUnreviewed(base, 1));
        it('reviewed file marked as reviewed at R2', expectReviewedFileMarkedAsReviewed(base, 2));
        it('reviewed file marked as reviewed at R1', expectReviewedFileMarkedAsReviewed(base, 1));
        it('unreviewed file marked as unreviewed at R2', expectUnreviewedFileMarkedAsUnreviewed(base, 2));
        it('unreviewed file marked as unreviewed at R1', expectUnreviewedFileMarkedAsUnreviewed(base, 1));

        it('review discussion at R2', expectReviewDiscussionForSavedRevision(base, 2));
        it('review discussion at R1', expectReviewDiscussionForSavedRevision(base, 1));
        it('file discussion at R2', expectFileDiscussionForSavedRevision(base, 2, FileIds.file4, 'file4/new.txt'));
        it('file discussion at R1',  expectFileDiscussionForSavedRevision(base, 1, FileIds.file4, 'file4/new.txt'));
    });

    describe('Two revisions, provisional', () => {
        const base = B.buildReviewInfo(
            B.addRevision(1),
            B.addRevision(2),
            B.addProvisional('P_HEAD_1', 'P_BASE_1'),
        );

        it('no unpublished changes', expectNoChanges(base));
        it('unreviewed file marked as reviewed at R2', expectUnreviewedFileMarkedAsReviewed(base, 2));
        it('unreviewed file marked as reviewed at R1', expectUnreviewedFileMarkedAsReviewed(base, 1));
        it('reviewed file marked as unreviewed at R2', expectReviewedFileMarkedAsUnreviewed(base, 2));
        it('reviewed file marked as unreviewed at R1', expectReviewedFileMarkedAsUnreviewed(base, 1));
        it('reviewed file marked as reviewed at R2', expectReviewedFileMarkedAsReviewed(base, 2));
        it('reviewed file marked as reviewed at R1', expectReviewedFileMarkedAsReviewed(base, 1));
        it('unreviewed file marked as unreviewed at R2', expectUnreviewedFileMarkedAsUnreviewed(base, 2));
        it('unreviewed file marked as unreviewed at R1', expectUnreviewedFileMarkedAsUnreviewed(base, 1));

        it('review discussion at R2', expectReviewDiscussionForSavedRevision(base, 2));
        it('review discussion at R1', expectReviewDiscussionForSavedRevision(base, 1));
        it('file discussion at R2', expectFileDiscussionForSavedRevision(base, 2, FileIds.file4, 'file4/new.txt'));
        it('file discussion at R1',  expectFileDiscussionForSavedRevision(base, 1, FileIds.file4, 'file4/new.txt'));

        it('provisional file marked as reviewed at P', () => {
            const reviewInfo = B.applyBuilders(base,
                B.addFileToReview(FileIds.prov_file3, 'file3', RevisionId.makeSelected(1), RevisionId.Provisional),
                B.defineFileMatrix()
            );
            const unpublished = { ...emptyUnpublishedReview,
                ...forHead(reviewInfo),
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.prov_file3}
                ]
            };
            const fileIdMap = {
                [FileIds.prov_file3]: 'prov_file3/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
        });

        it('known file marked as reviewed at P', () => {
            const reviewInfo = B.applyBuilders(base,
                B.addFileToReview(FileIds.file4, 'file4', RevisionId.makeSelected(1), RevisionId.Provisional),
                B.defineFileMatrix(),
                B.markFileChanged(FileIds.file4, [RevisionId.makeSelected(1), RevisionId.Provisional])
            );
            const unpublished = { ...emptyUnpublishedReview,
                ...forHead(reviewInfo),
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.file4}
                ]
            };
            const fileIdMap = {
                [FileIds.file4]: 'file4/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
        });

        it('provisional file marked as reviewed at P, file vanished', () => {
            const reviewInfo = B.applyBuilders(base, B.defineFileMatrix());
            const unpublished = { ...emptyUnpublishedReview,
                ...forHead(reviewInfo),
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.prov_file3}
                ]
            };
            const fileIdMap = {
                [FileIds.prov_file3]: 'prov_file3/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({...emptyUnpublishedReview, ...forHead(reviewInfo)});
        });

        it('known file marked as reviewed at P, file vanished', () => {
            const reviewInfo = B.applyBuilders(base, B.defineFileMatrix());
            const unpublished = { ...emptyUnpublishedReview,
                ...forHead(reviewInfo),
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.prov_file4}
                ]
            };
            const fileIdMap = {
                [FileIds.prov_file4]: 'file4_prov/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({...emptyUnpublishedReview, ...forHead(reviewInfo)});
        });

        it('review discussion at P', () => {
            const reviewInfo = B.applyBuilders(base, B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview, ...forHead(reviewInfo),
                unpublishedReviewDiscussions: [reviewDiscussion(RevisionId.Provisional)]
            };
            const fileIdMap = {};
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
        });

        it('file discussion at P', () => {
            const reviewInfo = B.applyBuilders(base, B.addFileToReview(FileIds.prov_file3, 'file_prov', RevisionId.Base, RevisionId.Provisional), B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview, ...forHead(reviewInfo),
                unpublishedFileDiscussions: [fileDiscussion(RevisionId.Provisional, FileIds.prov_file3)]
            };
            const fileIdMap = {
                [FileIds.prov_file3]: 'file_prov/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
        });

        it('file discussion at P, file vanished', () => {
            const reviewInfo = B.applyBuilders(base, B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview, ...forHead(reviewInfo),
                unpublishedFileDiscussions: [fileDiscussion(RevisionId.Provisional, FileIds.prov_file3)]
            };
            const fileIdMap = {
                [FileIds.prov_file3]: 'file3/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({...emptyUnpublishedReview, ...forHead(reviewInfo),
                unpublishedFileDiscussions: [],
                unpublishedReviewDiscussions: [reviewDiscussion(RevisionId.Provisional)]
            });
        });
    });

    describe('Three revisions, provisional is now saved', () => {
        const base = B.buildReviewInfo(
            B.addRevision(1),
            B.addRevision(2),
            B.addRevision(3),
        );

        it('no unpublished changes', expectNoChanges(base));
        it('unreviewed file marked as reviewed at R2', expectUnreviewedFileMarkedAsReviewed(base, 2));
        it('unreviewed file marked as reviewed at R1', expectUnreviewedFileMarkedAsReviewed(base, 1));
        it('reviewed file marked as unreviewed at R2', expectReviewedFileMarkedAsUnreviewed(base, 2));
        it('reviewed file marked as unreviewed at R1', expectReviewedFileMarkedAsUnreviewed(base, 1));
        it('reviewed file marked as reviewed at R2', expectReviewedFileMarkedAsReviewed(base, 2));
        it('reviewed file marked as reviewed at R1', expectReviewedFileMarkedAsReviewed(base, 1));
        it('unreviewed file marked as unreviewed at R2', expectUnreviewedFileMarkedAsUnreviewed(base, 2));
        it('unreviewed file marked as unreviewed at R1', expectUnreviewedFileMarkedAsUnreviewed(base, 1));

        it('review discussion at R2', expectReviewDiscussionForSavedRevision(base, 2));
        it('review discussion at R1', expectReviewDiscussionForSavedRevision(base, 1));
        it('file discussion at R2', expectFileDiscussionForSavedRevision(base, 2, FileIds.file4, 'file4/new.txt'));
        it('file discussion at R1',  expectFileDiscussionForSavedRevision(base, 1, FileIds.file4, 'file4/new.txt'));

        it('known file marked as reviewed at P (now R3)', () => {
            const reviewInfo = B.applyBuilders(base,
                B.addFileToReview(FileIds.file4, 'file4', RevisionId.makeSelected(1), RevisionId.makeSelected(3)),
                B.defineFileMatrix(),
                B.markFileChanged(FileIds.file4, [RevisionId.makeSelected(1), RevisionId.makeSelected(3)])
            );
            const unpublished = { ...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.file4}
                ]
            };
            const fileIdMap = {
                [FileIds.file4]: 'file4/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({ ...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedReviewedFiles: [
                    {revision: RevisionId.makeSelected(3), fileId: FileIds.file4}
                ]
            });
        });

        it('known file marked as reviewed at P (now R2)', () => {
            const reviewInfo = B.applyBuilders(base,
                B.addFileToReview(FileIds.file4, 'file4', RevisionId.makeSelected(1), RevisionId.makeSelected(2)),
                B.defineFileMatrix(),
                B.markFileChanged(FileIds.file4, [RevisionId.makeSelected(1), RevisionId.makeSelected(2)])
            );
            const unpublished = { ...emptyUnpublishedReview,
                headCommit: 'REV_2_HEAD',
                baseCommit: 'REV_2_BASE',
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.file4}
                ]
            };
            const fileIdMap = {
                [FileIds.file4]: 'file4/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({ ...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedReviewedFiles: [
                    {revision: RevisionId.makeSelected(2), fileId: FileIds.file4}
                ]
            });
        });

        it('provisional file (now known) marked as reviewed at P (now R3)', () => {
            const reviewInfo = B.applyBuilders(base,
                B.addFileToReview(FileIds.file4, 'file4', RevisionId.makeSelected(1), RevisionId.makeSelected(3)),
                B.defineFileMatrix(),
                B.markFileChanged(FileIds.file4, [RevisionId.makeSelected(1), RevisionId.makeSelected(3)])
            );
            const unpublished = { ...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.prov_file4}
                ]
            };
            const fileIdMap = {
                [FileIds.prov_file4]: 'file4/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({ ...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedReviewedFiles: [
                    {revision: RevisionId.makeSelected(3), fileId: FileIds.file4}
                ],
            });
        });

        it('review discussion at P (now R2)', () => {
            const reviewInfo = B.applyBuilders(base, B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview,
                headCommit: 'REV_2_HEAD',
                baseCommit: 'REV_2_BASE',
                unpublishedReviewDiscussions: [reviewDiscussion(RevisionId.Provisional)]
            };
            const fileIdMap = {};
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedReviewDiscussions: [reviewDiscussion(RevisionId.makeSelected(2))]
            });
        });

        it('known file discussion at P (now R2)', () => {
            const reviewInfo = B.applyBuilders(base, B.addFileToReview(FileIds.file4, 'file4', RevisionId.Base, RevisionId.Provisional), B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview,
                headCommit: 'REV_2_HEAD',
                baseCommit: 'REV_2_BASE',
                unpublishedFileDiscussions: [fileDiscussion(RevisionId.Provisional, FileIds.file4)]
            };
            const fileIdMap = {
                [FileIds.file4]: 'file4/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({
                ...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedFileDiscussions: [fileDiscussion(RevisionId.makeSelected(2), FileIds.file4)]
            });
        });

        it('provisional (now known) file discussion at P (now R3)', () => {
            const reviewInfo = B.applyBuilders(base, B.addFileToReview(FileIds.file4, 'file4', RevisionId.Base, RevisionId.Provisional), B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview,
                headCommit: 'REV_2_HEAD',
                baseCommit: 'REV_2_BASE',
                unpublishedFileDiscussions: [fileDiscussion(RevisionId.Provisional, FileIds.prov_file4)]
            };
            const fileIdMap = {
                [FileIds.prov_file4]: 'file4/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({
                ...emptyUnpublishedReview,
                headCommit: 'REV_3_HEAD',
                baseCommit: 'REV_3_BASE',
                unpublishedFileDiscussions: [fileDiscussion(RevisionId.makeSelected(2), FileIds.file4)]
            });
        });
    });

    describe('Two revisions, provisional, head diverges', () => {
        const base = B.buildReviewInfo(
            B.addRevision(1),
            B.addRevision(2),
            B.addProvisional('P_HEAD_1', 'P_BASE_1')
        );

        it('no unpublished changes', expectNoChanges(base));
        it('unreviewed file marked as reviewed at R2', expectUnreviewedFileMarkedAsReviewed(base, 2));
        it('unreviewed file marked as reviewed at R1', expectUnreviewedFileMarkedAsReviewed(base, 1));
        it('reviewed file marked as unreviewed at R2', expectReviewedFileMarkedAsUnreviewed(base, 2));
        it('reviewed file marked as unreviewed at R1', expectReviewedFileMarkedAsUnreviewed(base, 1));
        it('reviewed file marked as reviewed at R2', expectReviewedFileMarkedAsReviewed(base, 2));
        it('reviewed file marked as reviewed at R1', expectReviewedFileMarkedAsReviewed(base, 1));
        it('unreviewed file marked as unreviewed at R2', expectUnreviewedFileMarkedAsUnreviewed(base, 2));
        it('unreviewed file marked as unreviewed at R1', expectUnreviewedFileMarkedAsUnreviewed(base, 1));

        it('review discussion at R2', expectReviewDiscussionForSavedRevision(base, 2));
        it('review discussion at R1', expectReviewDiscussionForSavedRevision(base, 1));
        it('file discussion at R2', expectFileDiscussionForSavedRevision(base, 2, FileIds.file4, 'file4/new.txt'));
        it('file discussion at R1',  expectFileDiscussionForSavedRevision(base, 1, FileIds.file4, 'file4/new.txt'));

        it('known file marked as reviewed at P', () => {
            const reviewInfo = B.applyBuilders(base,
                B.addFileToReview(FileIds.file4, 'file4', RevisionId.makeSelected(1), RevisionId.Provisional),
                B.defineFileMatrix()
            );
            const unpublished = { ...emptyUnpublishedReview,
                headCommit: 'P_HEAD_2',
                baseCommit: 'P_BASE_1',
                unpublishedReviewedFiles: [
                    {revision: RevisionId.Provisional, fileId: FileIds.file4}
                ]
            };
            const fileIdMap = {
                [FileIds.file4]: 'file4/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({ ...emptyUnpublishedReview,
                headCommit: 'P_HEAD_1',
                baseCommit: 'P_BASE_1',
                unpublishedReviewedFiles: []
            });
        });

        it('review discussion at P', () => {
            const reviewInfo = B.applyBuilders(base, B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview, ...forHead(reviewInfo),
                unpublishedReviewDiscussions: [reviewDiscussion(RevisionId.Provisional)]
            };
            const fileIdMap = {};
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
        });

        it('file discussion at P', () => {
            const reviewInfo = B.applyBuilders(base, B.addFileToReview(FileIds.prov_file3, 'file_prov', RevisionId.Base, RevisionId.Provisional), B.defineFileMatrix());
            const unpublished: UnpublishedReview = {...emptyUnpublishedReview, ...forHead(reviewInfo),
                unpublishedFileDiscussions: [fileDiscussion(RevisionId.Provisional, FileIds.prov_file3)]
            };
            const fileIdMap = {
                [FileIds.prov_file3]: 'file_prov/new.txt'
            };
            expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal(unpublished);
        });
    });

    it('haha', () => {
        const reviewInfo = B.buildReviewInfo(B.addRevision(1), B.addProvisional('P_HEAD_1', 'P_BASE_1'),
            B.addFileToReview(FileIds.file4, 'file4', RevisionId.Base, RevisionId.Provisional),
            B.defineFileMatrix(),
            B.markFileChanged(FileIds.file4, [RevisionId.makeSelected(1), RevisionId.Provisional])
        );
        console.log(reviewInfo.fileMatrix[0].revisions);
        const unpublished: UnpublishedReview = {...emptyUnpublishedReview,
            headCommit: 'REV_1_HEAD',
            baseCommit: 'REV_1_BASE',
            unpublishedReviewedFiles: [
                { revision: RevisionId.makeSelected(1), fileId: FileIds.file4, }
            ]
        };
        const fileIdMap = {
            [FileIds.file4]: 'file4/new.txt'
        };
        expect(upgradeReview(reviewInfo, unpublished, fileIdMap)).to.deep.equal({...emptyUnpublishedReview,
            headCommit: 'P_HEAD_1',
            baseCommit: 'P_BASE_1',
            unpublishedReviewedFiles: []
        })
    });
});