function status(review) {
    const byStatus = _(review.matrix)
        .countBy(file => _(file.revisions).filter(r => !r.isUnchanged).last().reviewers.length > 0)
        .value();

    const summary = {
        reviewedCount: byStatus[true] || 0,
        unreviewedCount: byStatus[false] || 0,
        unresolved: review.unresolvedDiscussions,
    };

    let state = {
        ok: true,
        reasons: []
    };
    
    if(summary.reviewedCount > 0)
        state.reasons.push(`${summary.reviewedCount} files reviewed at latest revision`);

    if (summary.unreviewedCount > 0) {
        state.ok = false;
        state.reasons.push(`${summary.unreviewedCount} files not reviewed`);
    }

    if (summary.unresolved > 0) {
        state.ok = false;
        state.reasons.push(`${summary.unresolved} discussions unresolved`);
    }

    return state;
}

function thumb(review, user) {
    const byStatus = _(review.matrix)
        .countBy(file => _(file.revisions).filter(r => !r.isUnchanged).last().reviewers.indexOf(user) >= 0)
        .value();

    const nothingReviewed = (byStatus[true] || 0) === 0;

    if (nothingReviewed) {
        return 0;
    }

    const allReviewed = (byStatus[false] || 0) === 0;

    return allReviewed ? 1 : -1;
}