function status(review) {
    const byStatus = _(review.matrix)
        .countBy(file => _(file.revisions).filter(r => !r.isUnchanged).last().reviewers.length > 0)
        .value();

    const summary = {
        reviewedCount: byStatus[true] || 0,
        unreviewedCount: byStatus[false] || 0,
        unresolved: review.unresolvedDiscussions,
    };

    const state = {
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